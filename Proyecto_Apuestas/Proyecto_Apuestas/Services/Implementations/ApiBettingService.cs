using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels.API;
using System.Text.Json;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class ApiBettingService : IApiBettingService
    {
        private readonly apuestasDbContext _context;
        private readonly IUserService _userService;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<ApiBettingService> _logger;

        public ApiBettingService(
            apuestasDbContext context,
            IUserService userService,
            IPaymentService paymentService,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<ApiBettingService> logger)
        {
            _context = context;
            _userService = userService;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiBetResult> PlaceBetAsync(CreateBetFromApiViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = _userService.GetCurrentUserId();

                // Valida límites
                var limitValidation = await ValidateBetLimitsAsync(userId, model.Stake);
                if (!limitValidation.IsValid)
                {
                    return new ApiBetResult { Success = false, ErrorMessage = limitValidation.ErrorMessage };
                }

                // Actualiza balance (sin transacción porque ya estamos en una)
                if (!await _userService.UpdateUserBalanceAsync(userId, model.Stake, "BET", useTransaction: false))
                {
                    return new ApiBetResult { Success = false, ErrorMessage = "Saldo insuficiente" };
                }

                // Crea apuesta de API
                var apiBet = new ApiBet
                {
                    ApiEventId = model.ApiEventId,
                    SportKey = model.SportKey,
                    EventName = model.EventName,
                    TeamName = model.TeamName,
                    EventDate = model.EventDate,
                    Odds = model.Odds,
                    Stake = model.Stake,
                    Payout = await CalculatePotentialPayoutAsync(model.Stake, model.Odds),
                    Date = DateTime.Now,
                    BetStatus = "P", // Pending
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    HomeTeam = ExtractHomeTeam(model.EventName),
                    AwayTeam = ExtractAwayTeam(model.EventName),
                    Region = "us", // Por defecto, se podría obtener del modelo
                    Market = "h2h", // Por defecto, se podría obtener del modelo
                    Bookmaker = "API" // Por defecto
                };

                _context.ApiBets.Add(apiBet);
                await _context.SaveChangesAsync();

                // Asocia usuario con apuesta
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.ApiBets.Add(apiBet);
                    user.LastBet = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Crea transacción de pago
                await _paymentService.CreateTransactionAsync(new PaymentTransactionRequest
                {
                    UserId = userId,
                    Amount = model.Stake,
                    TransactionType = "BET",
                    Description = $"Apuesta API: {model.EventName} - {model.TeamName}"
                });

                // Envia notificación
                await _notificationService.SendNotificationAsync(userId,
                    $"Apuesta realizada con éxito. {model.TeamName} - Cuota: {model.Odds:0.00} - Monto: ₡{model.Stake:N2}");

                await transaction.CommitAsync();
                return new ApiBetResult { Success = true, ApiBetId = apiBet.ApiBetId };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing API bet for user {UserId}", _userService.GetCurrentUserId());
                return new ApiBetResult { Success = false, ErrorMessage = "Error al procesar la apuesta" };
            }
        }

        public async Task<ApiBetDetailsViewModel?> GetApiBetDetailsAsync(int apiBetId, int userId)
        {
            var apiBet = await _context.ApiBets
                .Include(b => b.PaymentTransaction)
                .FirstOrDefaultAsync(b => b.ApiBetId == apiBetId && b.Users.Any(u => u.UserId == userId));

            if (apiBet == null) return null;

            return new ApiBetDetailsViewModel
            {
                ApiBetId = apiBet.ApiBetId,
                ApiEventId = apiBet.ApiEventId,
                SportKey = apiBet.SportKey,
                EventName = apiBet.EventName,
                EventDate = apiBet.EventDate,
                TeamName = apiBet.TeamName,
                Odds = apiBet.Odds,
                Stake = apiBet.Stake,
                Payout = apiBet.Payout,
                BetStatus = apiBet.BetStatus,
                BetStatusDisplay = GetBetStatusDisplay(apiBet.BetStatus),
                CreatedAt = apiBet.CreatedAt,
                TransactionStatus = apiBet.PaymentTransaction?.Status,
                HomeTeam = apiBet.HomeTeam,
                AwayTeam = apiBet.AwayTeam,
                Region = apiBet.Region,
                Market = apiBet.Market,
                Bookmaker = apiBet.Bookmaker,
                EventResult = apiBet.EventResult,
                IsEventFinished = apiBet.EventDate < DateTime.Now && !string.IsNullOrEmpty(apiBet.EventResult)
            };
        }

        public async Task<ApiBetHistoryViewModel> GetUserApiBetHistoryAsync(int userId, int page = 1, int pageSize = 20, ApiBetHistoryFilter? filter = null)
        {
            var query = _context.ApiBets
                .Where(b => b.Users.Any(u => u.UserId == userId));

            // Aplica filtros
            if (filter != null)
            {
                if (filter.StartDate.HasValue)
                    query = query.Where(b => b.CreatedAt >= filter.StartDate.Value);

                if (filter.EndDate.HasValue)
                    query = query.Where(b => b.CreatedAt <= filter.EndDate.Value);

                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(b => b.BetStatus == filter.Status);

                if (!string.IsNullOrEmpty(filter.SportKey))
                    query = query.Where(b => b.SportKey == filter.SportKey);
            }

            var totalBets = await query.CountAsync();
            var apiBets = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var apiBetDetails = apiBets.Select(bet => new ApiBetDetailsViewModel
            {
                ApiBetId = bet.ApiBetId,
                ApiEventId = bet.ApiEventId,
                SportKey = bet.SportKey,
                EventName = bet.EventName,
                EventDate = bet.EventDate,
                TeamName = bet.TeamName,
                Odds = bet.Odds,
                Stake = bet.Stake,
                Payout = bet.Payout,
                BetStatus = bet.BetStatus,
                BetStatusDisplay = GetBetStatusDisplay(bet.BetStatus),
                CreatedAt = bet.CreatedAt,
                HomeTeam = bet.HomeTeam,
                AwayTeam = bet.AwayTeam,
                Region = bet.Region,
                Market = bet.Market,
                Bookmaker = bet.Bookmaker,
                EventResult = bet.EventResult,
                IsEventFinished = bet.EventDate < DateTime.Now && !string.IsNullOrEmpty(bet.EventResult)
            }).ToList();

            // Calcula estadísticas
            var allUserApiBets = await _context.ApiBets
                .Where(b => b.Users.Any(u => u.UserId == userId))
                .ToListAsync();

            return new ApiBetHistoryViewModel
            {
                ApiBets = apiBetDetails,
                TotalBets = allUserApiBets.Count,
                WonBets = allUserApiBets.Count(b => b.BetStatus == "W"),
                LostBets = allUserApiBets.Count(b => b.BetStatus == "L"),
                PendingBets = allUserApiBets.Count(b => b.BetStatus == "P"),
                TotalStaked = allUserApiBets.Sum(b => b.Stake),
                TotalWon = allUserApiBets.Where(b => b.BetStatus == "W").Sum(b => b.Payout),
                NetProfit = allUserApiBets.Where(b => b.BetStatus == "W").Sum(b => b.Payout) -
                           allUserApiBets.Where(b => b.BetStatus == "L").Sum(b => b.Stake),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalBets / (double)pageSize),
                PageSize = pageSize
            };
        }

        public async Task<bool> CancelApiBetAsync(int apiBetId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var apiBet = await _context.ApiBets
                    .Include(b => b.Users)
                    .FirstOrDefaultAsync(b => b.ApiBetId == apiBetId &&
                                              b.Users.Any(u => u.UserId == userId) &&
                                              b.BetStatus == "P");

                if (apiBet == null) return false;

                // Verifica que el evento no haya comenzado
                if (apiBet.EventDate <= DateTime.Now.AddMinutes(5))
                    return false;

                apiBet.BetStatus = "C"; // Cancelled
                apiBet.UpdatedAt = DateTime.Now;

                // Devuelve el dinero
                await _userService.UpdateUserBalanceAsync(userId, apiBet.Stake, "PAYOUT");

                // Crea transacción de devolución
                await _paymentService.CreateTransactionAsync(new PaymentTransactionRequest
                {
                    UserId = userId,
                    Amount = apiBet.Stake,
                    TransactionType = "REFUND",
                    Description = $"Cancelación apuesta: {apiBet.EventName}"
                });

                await _notificationService.SendNotificationAsync(userId,
                    $"Apuesta cancelada. Se devolvieron ₡{apiBet.Stake:N2} a tu cuenta");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling API bet {ApiBetId}", apiBetId);
                return false;
            }
        }

        public async Task<bool> SettleApiBetAsync(int apiBetId, string outcome, string? eventResult = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var apiBet = await _context.ApiBets
                    .Include(b => b.Users)
                    .FirstOrDefaultAsync(b => b.ApiBetId == apiBetId && b.BetStatus == "P");

                if (apiBet == null) return false;

                apiBet.BetStatus = outcome; // "W" or "L"
                apiBet.UpdatedAt = DateTime.Now;
                apiBet.EventResult = eventResult;

                if (outcome == "W" && apiBet.Users.Any())
                {
                    var userId = apiBet.Users.First().UserId;

                    // Paga ganancia
                    await _userService.UpdateUserBalanceAsync(userId, apiBet.Payout, "PAYOUT");

                    // Crea transacción
                    await _paymentService.CreateTransactionAsync(new PaymentTransactionRequest
                    {
                        UserId = userId,
                        Amount = apiBet.Payout,
                        TransactionType = "PAYOUT",
                        Description = $"Ganancia apuesta: {apiBet.EventName}"
                    });

                    await _notificationService.SendNotificationAsync(userId,
                        $"¡Felicidades! Ganaste ₡{apiBet.Payout:N2} en tu apuesta de {apiBet.EventName}");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error settling API bet {ApiBetId}", apiBetId);
                return false;
            }
        }

        public async Task<decimal> CalculatePotentialPayoutAsync(decimal stake, decimal odds)
        {
            return await Task.FromResult(stake * odds);
        }

        public async Task<List<ApiBet>> GetPendingApiBetsByEventAsync(string apiEventId)
        {
            return await _context.ApiBets
                .Include(b => b.Users)
                .Where(b => b.ApiEventId == apiEventId && b.BetStatus == "P")
                .ToListAsync();
        }

        public async Task<bool> SettleEventApiBetsAsync(string apiEventId, string eventResult)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var apiBets = await GetPendingApiBetsByEventAsync(apiEventId);

                foreach (var apiBet in apiBets)
                {
                    // Determinar si la apuesta fue ganadora basándose en el resultado
                    var isWinner = DetermineWinner(apiBet, eventResult);
                    await SettleApiBetAsync(apiBet.ApiBetId, isWinner ? "W" : "L", eventResult);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error settling event API bets for eventId: {ApiEventId}", apiEventId);
                return false;
            }
        }

        public async Task<Dictionary<string, decimal>> GetApiBettingStatisticsAsync(int userId)
        {
            var apiBets = await _context.ApiBets
                .Where(b => b.Users.Any(u => u.UserId == userId))
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                ["TotalBets"] = apiBets.Count,
                ["TotalStaked"] = apiBets.Sum(b => b.Stake),
                ["TotalWon"] = apiBets.Where(b => b.BetStatus == "W").Sum(b => b.Payout),
                ["TotalLost"] = apiBets.Where(b => b.BetStatus == "L").Sum(b => b.Stake),
                ["PendingBets"] = apiBets.Count(b => b.BetStatus == "P"),
                ["WinRate"] = apiBets.Count > 0 ?
                    (decimal)apiBets.Count(b => b.BetStatus == "W") / apiBets.Count * 100 : 0,
                ["AverageStake"] = apiBets.Count > 0 ? apiBets.Average(b => b.Stake) : 0,
                ["AverageOdds"] = apiBets.Count > 0 ? apiBets.Average(b => b.Odds) : 0
            };
        }

        public async Task<List<ApiBet>> GetActiveApiBetsByUserAsync(int userId)
        {
            return await _context.ApiBets
                .Where(b => b.Users.Any(u => u.UserId == userId) && b.BetStatus == "P")
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<BetLimitValidationResult> ValidateBetLimitsAsync(int userId, decimal amount)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) 
                return new BetLimitValidationResult { IsValid = false, ErrorMessage = "Usuario no encontrado" };

            // Límites por rol (en colones costarricenses)
            var maxBetAmount = user.Role.RoleName switch
            {
                "VIP" => 26000000m,      // $50,000 * 520 = ₡26,000,000
                "Premium" => 10400000m,  // $20,000 * 520 = ₡10,400,000
                "Regular" => 2600000m,   // $5,000 * 520 = ₡2,600,000
                _ => 520000m             // $1,000 * 520 = ₡520,000
            };

            if (amount > maxBetAmount) 
                return new BetLimitValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = $"El monto máximo por apuesta para tu nivel ({user.Role.RoleName}) es ₡{maxBetAmount:N0}" 
                };

            // Límite diario
            var today = DateTime.Today;
            var todayTotal = await _context.ApiBets
                .Where(b => b.Users.Any(u => u.UserId == userId) && b.CreatedAt >= today)
                .SumAsync(b => b.Stake);

            var dailyLimit = user.Role.RoleName switch
            {
                "VIP" => 52000000m,      // $100,000 * 520 = ₡52,000,000
                "Premium" => 26000000m,  // $50,000 * 520 = ₡26,000,000
                "Regular" => 5200000m,   // $10,000 * 520 = ₡5,200,000
                _ => 2600000m            // $5,000 * 520 = ₡2,600,000
            };

            if ((todayTotal + amount) > dailyLimit)
                return new BetLimitValidationResult 
                { 
                    IsValid = false, 
                    ErrorMessage = $"Límite diario excedido. Has apostado ₡{todayTotal:N0} hoy. Límite: ₡{dailyLimit:N0}" 
                };

            return new BetLimitValidationResult { IsValid = true };
        }

        public async Task<BettingLimitsInfo> GetBettingLimitsAsync(int userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return new BettingLimitsInfo();

            // Límites por rol (en colones costarricenses)
            var maxBetAmount = user.Role.RoleName switch
            {
                "VIP" => 26000000m,      // $50,000 * 520 = ₡26,000,000
                "Premium" => 10400000m,  // $20,000 * 520 = ₡10,400,000
                "Regular" => 2600000m,   // $5,000 * 520 = ₡2,600,000
                _ => 520000m             // $1,000 * 520 = ₡520,000
            };

            var dailyLimit = user.Role.RoleName switch
            {
                "VIP" => 52000000m,      // $100,000 * 520 = ₡52,000,000
                "Premium" => 26000000m,  // $50,000 * 520 = ₡26,000,000
                "Regular" => 5200000m,   // $10,000 * 520 = ₡5,200,000
                _ => 2600000m            // $5,000 * 520 = ₡2,600,000
            };

            // Calcular total apostado hoy
            var today = DateTime.Today;
            var todayTotal = await _context.ApiBets
                .Where(b => b.Users.Any(u => u.UserId == userId) && b.CreatedAt >= today)
                .SumAsync(b => b.Stake);

            return new BettingLimitsInfo
            {
                MaxBetAmount = maxBetAmount,
                DailyLimit = dailyLimit,
                TodayStaked = todayTotal,
                UserRole = user.Role.RoleName
            };
        }

        private string GetBetStatusDisplay(string status)
        {
            return status switch
            {
                "P" => "Pendiente",
                "W" => "Ganada",
                "L" => "Perdida",
                "C" => "Cancelada",
                _ => "Desconocido"
            };
        }

        private string? ExtractHomeTeam(string eventName)
        {
            var parts = eventName.Split(" vs ");
            return parts.Length > 0 ? parts[0].Trim() : null;
        }

        private string? ExtractAwayTeam(string eventName)
        {
            var parts = eventName.Split(" vs ");
            return parts.Length > 1 ? parts[1].Trim() : null;
        }

        private bool DetermineWinner(ApiBet apiBet, string eventResult)
        {
            // Lógica simple para determinar ganador
            // En una implementación real, esto dependería del tipo de apuesta y el resultado
            try
            {
                var result = JsonSerializer.Deserialize<Dictionary<string, object>>(eventResult);
                // Por ahora, asumimos que si el equipo apostado está en el resultado como ganador
                return result.ContainsKey("winner") && result["winner"].ToString()?.Contains(apiBet.TeamName) == true;
            }
            catch
            {
                // Si no se puede parsear el resultado, asumir pérdida por seguridad
                return false;
            }
        }
    }
}