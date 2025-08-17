using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using System.Security.Claims;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class UserService : IUserService
    {
        private readonly apuestasDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IPasswordHasher<UserAccount> _passwordHasher;
        private readonly IMapper _mapper;
        private readonly ILogger<UserService> _logger;

        public UserService(
            apuestasDbContext context,
            IHttpContextAccessor httpContextAccessor,
            IPasswordHasher<UserAccount> passwordHasher,
            IMapper mapper,
            ILogger<UserService> logger)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _passwordHasher = passwordHasher;
            _mapper = mapper;
            _logger = logger;
        }

        public int GetCurrentUserId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return int.TryParse(claim?.Value, out var userId) ? userId : 0;
        }

        public string GetCurrentUserEmail()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        }

        public ClaimsPrincipal GetCurrentUser()
        {
            return _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
        }

        public async Task<UserAccount?> GetCurrentUserAsync()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return null;

            return await GetUserByIdAsync(userId);
        }

        public async Task<UserAccount?> GetUserByIdAsync(int userId)
        {
            return await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<UserAccount?> GetUserByEmailAsync(string email)
        {
            return await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<UserAccount?> GetUserByUsernameAsync(string username)
        {
            return await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<UserAccount?> GetUserByEmailOrUsernameAsync(string emailOrUsername)
        {
            return await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.UserName == emailOrUsername);
        }

        public async Task<bool> IsUserInRoleAsync(int userId, string roleName)
        {
            return await _context.UserAccounts
                .AnyAsync(u => u.UserId == userId && u.Role.RoleName == roleName);
        }

        public async Task<bool> IsCurrentUserInRoleAsync(string roleName)
        {
            var userId = GetCurrentUserId();
            return await IsUserInRoleAsync(userId, roleName);
        }

        public async Task<UserProfileViewModel?> GetUserProfileAsync(int userId)
        {
            var user = await _context.UserAccounts
                .Include(u => u.Role)
                .Include(u => u.Bets)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null) return null;

            var profile = _mapper.Map<UserProfileViewModel>(user);

            // NOTE: Esto nos calcula estadísticas
            profile.TotalBets = user.Bets.Count;
            profile.WonBets = user.Bets.Count(b => b.BetStatus == "W");
            profile.LostBets = user.Bets.Count(b => b.BetStatus == "L");
            profile.TotalWinnings = user.Bets.Where(b => b.BetStatus == "W").Sum(b => b.Payout);
            profile.TotalLosses = user.Bets.Where(b => b.BetStatus == "L").Sum(b => b.Stake);
            profile.WinRate = profile.TotalBets > 0 ? (decimal)profile.WonBets / profile.TotalBets * 100 : 0;

            return profile;
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileViewModel model)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                user.FirstName = model.FirstName;
                user.PrimerApellido = model.PrimerApellido;
                user.SegundoApellido = model.SegundoApellido;
                user.PhoneNumber = model.PhoneNumber;
                user.Country = model.Country;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile for userId: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
                if (verificationResult == PasswordVerificationResult.Failed) return false;

                user.PasswordHash = _passwordHasher.HashPassword(user, newPassword);
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for userId: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UpdateUserBalanceAsync(int userId, decimal amount, string transactionType)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                if (transactionType == "DEPOSIT" || transactionType == "PAYOUT")
                {
                    user.CreditBalance += amount;
                }
                else if (transactionType == "WITHDRAWAL" || transactionType == "BET")
                {
                    if (user.CreditBalance < amount) return false;
                    user.CreditBalance -= amount;
                }

                user.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating balance for userId: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> LockUserAccountAsync(int userId, DateTime until, string reason)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                user.LockedUntil = until;
                user.UpdatedAt = DateTime.Now;

                // Note: Registra en log
                await _context.ReportLogs.AddAsync(new ReportLog
                {
                    UserId = userId,
                    ReportType = "ACCOUNT_LOCK",
                    GeneratedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking account for userId: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UnlockUserAccountAsync(int userId)
        {
            try
            {
                var user = await GetUserByIdAsync(userId);
                if (user == null) return false;

                user.LockedUntil = null;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking account for userId: {UserId}", userId);
                return false;
            }
        }

        public async Task<List<UserAccount>> GetActiveUsersAsync()
        {
            return await _context.UserAccounts
                .Include(u => u.Role)
                .Where(u => u.IsActive == true && (u.LockedUntil == null || u.LockedUntil <= DateTime.Now))
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        public async Task<bool> RecordLoginAttemptAsync(string email, bool isSuccessful)
        {
            try
            {
                var user = await GetUserByEmailAsync(email);
                if (user == null) return false;

                await _context.LoginAttempts.AddAsync(new LoginAttempt
                {
                    UserId = user.UserId,
                    AttemptTime = DateTime.Now,
                    IsSuccessful = isSuccessful
                });

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording login attempt for email: {Email}", email);
                return false;
            }
        }

        public async Task<int> GetFailedLoginAttemptsAsync(string email, TimeSpan period)
        {
            var user = await GetUserByEmailAsync(email);
            if (user == null) return 0;

            var since = DateTime.Now.Subtract(period);
            return await _context.LoginAttempts
                .Where(la => la.UserId == user.UserId &&
                            !la.IsSuccessful &&
                            la.AttemptTime >= since)
                .CountAsync();
        }
    }
}