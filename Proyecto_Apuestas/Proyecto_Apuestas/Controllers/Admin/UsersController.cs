using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Controllers.Admin
{
    public class UsersController : AdminBaseController
    {
        private readonly apuestasDbContext _context;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public UsersController(
            apuestasDbContext context,
            IUserService userService,
            INotificationService notificationService,
            ILogger<UsersController> logger) : base(logger)
        {
            _context = context;
            _userService = userService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(UserManagementViewModel model)
        {
            var query = _context.UserAccounts
                .Include(u => u.Role)
                .Include(u => u.Bets)
                .AsQueryable();

            // NOTE: Esto nos aplica filtros
            if (!string.IsNullOrEmpty(model.SearchTerm))
            {
                query = query.Where(u => u.UserName.Contains(model.SearchTerm) ||
                                        u.Email.Contains(model.SearchTerm));
            }

            if (model.RoleId.HasValue)
            {
                query = query.Where(u => u.RoleId == model.RoleId.Value);
            }

            if (model.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == model.IsActive.Value);
            }

            //  NOTE: Esto nos aplica ordenamiento
            query = model.SortBy switch
            {
                "name" => query.OrderBy(u => u.UserName),
                "email" => query.OrderBy(u => u.Email),
                "balance" => query.OrderByDescending(u => u.CreditBalance),
                "lastbet" => query.OrderByDescending(u => u.LastBet),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var totalUsers = await query.CountAsync();
            var pageSize = model.PageSize > 0 ? model.PageSize : 20;
            var currentPage = model.CurrentPage > 0 ? model.CurrentPage : 1;

            var users = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserAdminViewModel
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Email = u.Email,
                    RoleName = u.Role.RoleName,
                    CreditBalance = u.CreditBalance,
                    IsActive = u.IsActive ?? false,
                    LockedUntil = u.LockedUntil,
                    LastBet = u.LastBet,
                    CreatedAt = u.CreatedAt,
                    TotalBets = u.Bets.Count,
                    TotalWagered = u.Bets.Sum(b => b.Stake)
                })
                .ToListAsync();

            model.Users = users;
            model.TotalUsers = totalUsers;
            model.ActiveUsers = await _context.UserAccounts.CountAsync(u => u.IsActive == true);
            model.LockedUsers = await _context.UserAccounts.CountAsync(u => u.LockedUntil > DateTime.Now);
            model.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.RoleId, r => r.RoleName);

            return View(model);
        }

        public async Task<IActionResult> Details(int id)
        {
            var user = await _context.UserAccounts
                .Include(u => u.Role)
                .Include(u => u.Bets)
                .Include(u => u.PaymentTransactions)
                .Include(u => u.LoginAttempts)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            var profile = await _userService.GetUserProfileAsync(id);
            ViewBag.RecentActivity = await GetUserRecentActivity(id);

            return View(profile);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = user.IsActive == true ? "activada" : "desactivada";
            AddSuccessMessage($"Cuenta {status} exitosamente");

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccount(int id, int hours, string reason)
        {
            var until = DateTime.Now.AddHours(hours);
            var success = await _userService.LockUserAccountAsync(id, until, reason);

            if (success)
            {
                await _notificationService.SendNotificationAsync(id,
                    $"Tu cuenta ha sido bloqueada temporalmente hasta {until:dd/MM/yyyy HH:mm}. Motivo: {reason}");

                AddSuccessMessage("Cuenta bloqueada exitosamente");
            }
            else
            {
                AddErrorMessage("Error al bloquear la cuenta");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(int id)
        {
            var success = await _userService.UnlockUserAccountAsync(id);

            if (success)
            {
                await _notificationService.SendNotificationAsync(id,
                    "Tu cuenta ha sido desbloqueada. Ya puedes acceder nuevamente.");

                AddSuccessMessage("Cuenta desbloqueada exitosamente");
            }
            else
            {
                AddErrorMessage("Error al desbloquear la cuenta");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, int roleId)
        {
            var user = await _context.UserAccounts.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.RoleId = roleId;
            user.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            AddSuccessMessage("Rol actualizado exitosamente");
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustBalance(int id, decimal amount, string type, string reason)
        {
            var transactionType = type == "add" ? "DEPOSIT" : "WITHDRAWAL";
            var success = await _userService.UpdateUserBalanceAsync(id, Math.Abs(amount), transactionType);

            if (success)
            {
                await _notificationService.SendNotificationAsync(id,
                    $"Ajuste de saldo: {(type == "add" ? "+" : "-")}${Math.Abs(amount):N2}. Motivo: {reason}");

                AddSuccessMessage("Saldo ajustado exitosamente");
            }
            else
            {
                AddErrorMessage("Error al ajustar el saldo");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        private async Task<List<object>> GetUserRecentActivity(int userId)
        {
            var activities = new List<object>();

            // Últimas apuestas
            var recentBets = await _context.Bets
                .Where(b => b.Users.Any(u => u.UserId == userId))
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new { Type = "Bet", Date = b.CreatedAt, Amount = b.Stake, Status = b.BetStatus })
                .ToListAsync();

            // Últimas transacciones
            var recentTransactions = await _context.PaymentTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .Select(t => new { Type = t.TransactionType, Date = t.CreatedAt, t.Amount, t.Status })
                .ToListAsync();

            activities.AddRange(recentBets);
            activities.AddRange(recentTransactions);

            return activities.OrderByDescending(a => ((dynamic)a).Date).Take(10).ToList();
        }
    }
}