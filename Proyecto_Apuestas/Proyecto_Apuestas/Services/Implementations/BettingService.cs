using AutoMapper;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class BettingService : IBettingService
    {
        private readonly apuestasDbContext _context;
        private readonly IUserService _userService;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<BettingService> _logger;

        public BettingService(
            apuestasDbContext context,
            IUserService userService,
            IPaymentService paymentService,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<BettingService> logger)
        {
            _context = context;
            _userService = userService;
            _paymentService = paymentService;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<BetResult> PlaceBetAsync(CreateBetViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = _userService.GetCurrentUserId();

                // Valida límites
                if (!await ValidateBetLimitsAsync(userId, model.Stake))
                {
                    return new BetResult { Success = false, ErrorMessage = "Monto excede los límites permitidos" };
                }

                // Actualiza balance
                if (!await _userService.UpdateUserBalanceAsync(userId, model.Stake, "BET"))
                {
                    return new BetResult { Success = false, ErrorMessage = "Saldo insuficiente" };
                }

                // Crea apuesta
                var bet = new Bet
                {
                    EventId = model.EventId,
                    Odds = model.Odds,
                    Stake = model.Stake,
                    Payout = await CalculatePotentialPayoutAsync(model.Stake, model.Odds),
                    Date = DateTime.Now,
                    BetStatus = "P", // Pending
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Bets.Add(bet);
                await _context.SaveChangesAsync();

                // Asocia usuario con apuesta
                var user = await _userService.GetUserByIdAsync(userId);
                if (user != null)
                {
                    user.Bets.Add(bet);
                    user.LastBet = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Crea transacción de pago
                await _paymentService.CreateTransactionAsync(new PaymentTransactionRequest
                {
                    UserId = userId,
                    Amount = model.Stake,
                    TransactionType = "BET",
                    RelatedBetId = bet.BetId
                });

                // Envia notificación
                await _notificationService.SendNotificationAsync(userId,
                    $"Apuesta realizada con éxito. Monto: ${model.Stake:N2}");

                await transaction.CommitAsync();
                return new BetResult { Success = true, BetId = bet.BetId };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing bet for user {UserId}", _userService.GetCurrentUserId());
                return new BetResult { Success = false, ErrorMessage = "Error al procesar la apuesta" };
            }
        }

        public async Task<BetResult> PlaceMultipleBetsAsync(BetSlipViewModel model)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var userId = _userService.GetCurrentUserId();
                var totalStake = model.Items.Sum(i => i.Stake);

                // NOTE: Valida límites totales
                if (!await ValidateBetLimitsAsync(userId, totalStake))
                {
                    return new BetResult { Success = false, ErrorMessage = "Monto total excede los límites permitidos" };
                }

                // Actualiza balance
                if (!await _userService.UpdateUserBalanceAsync(userId, totalStake, "BET"))
                {
                    return new BetResult { Success = false, ErrorMessage = "Saldo insuficiente" };
                }

                var betIds = new List<int>();
                var user = await _userService.GetUserByIdAsync(userId);

                foreach (var item in model.Items)
                {
                    var bet = new Bet
                    {
                        EventId = item.EventId,
                        Odds = item.Odds,
                        Stake = item.Stake,
                        Payout = await CalculatePotentialPayoutAsync(item.Stake, item.Odds),
                        Date = DateTime.Now,
                        BetStatus = "P",
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.Bets.Add(bet);
                    await _context.SaveChangesAsync();

                    user?.Bets.Add(bet);
                    betIds.Add(bet.BetId);
                }

                if (user != null)
                {
                    user.LastBet = DateTime.Now;
                    await _context.SaveChangesAsync();
                }

                // Crea transacción única para todas las apuestas
                await _paymentService.CreateTransactionAsync(new PaymentTransactionRequest
                {
                    UserId = userId,
                    Amount = totalStake,
                    TransactionType = "BET",
                    RelatedBetIds = betIds
                });

                await _notificationService.SendNotificationAsync(userId,
                    $"Boleto de apuestas realizado. Total: ${totalStake:N2}");

                await transaction.CommitAsync();
                return new BetResult { Success = true };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing multiple bets");
                return new BetResult { Success = false, ErrorMessage = "Error al procesar las apuestas" };
            }
        }

        public async Task<BetDetailsViewModel?> GetBetDetailsAsync(int betId, int userId)
        {
            var bet = await _context.Bets
                .Include(b => b.Event)
                    .ThenInclude(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                .Include(b => b.PaymentTransaction)
                .FirstOrDefaultAsync(b => b.BetId == betId && b.Users.Any(u => u.UserId == userId));

            if (bet == null) return null;

            var teams = bet.Event.EventHasTeams.Select(et => et.Team).ToList();
            var betTeam = teams.FirstOrDefault(); // Aquí se debería determinar en qué equipo apostó

            return new BetDetailsViewModel
            {
                BetId = bet.BetId,
                EventName = $"{string.Join(" vs ", teams.Select(t => t.TeamName))}",
                EventDate = bet.Event.Date,
                TeamName = betTeam?.TeamName ?? "N/A",
                Odds = bet.Odds,
                Stake = bet.Stake,
                Payout = bet.Payout,
                BetStatus = bet.BetStatus,
                BetStatusDisplay = GetBetStatusDisplay(bet.BetStatus),
                CreatedAt = bet.CreatedAt,
                TransactionStatus = bet.PaymentTransaction?.Status,
                EventOutcome = bet.Event.Outcome,
                IsEventFinished = bet.Event.Date < DateTime.Now && !string.IsNullOrEmpty(bet.Event.Outcome)
            };
        }

        public async Task<BetHistoryViewModel> GetUserBetHistoryAsync(int userId, int page = 1, int pageSize = 20, BetHistoryFilter? filter = null)
        {
            var query = _context.Bets
                .Include(b => b.Event)
                    .ThenInclude(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
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

                if (filter.SportId.HasValue)
                    query = query.Where(b => b.Event.EventHasTeams.Any(et => et.Team.SportId == filter.SportId.Value));
            }

            var totalBets = await query.CountAsync();
            var bets = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var betDetails = bets.Select(bet =>
            {
                var teams = bet.Event.EventHasTeams.Select(et => et.Team).ToList();
                return new BetDetailsViewModel
                {
                    BetId = bet.BetId,
                    EventName = $"{string.Join(" vs ", teams.Select(t => t.TeamName))}",
                    EventDate = bet.Event.Date,
                    TeamName = teams.FirstOrDefault()?.TeamName ?? "N/A",
                    Odds = bet.Odds,
                    Stake = bet.Stake,
                    Payout = bet.Payout,
                    BetStatus = bet.BetStatus,
                    BetStatusDisplay = GetBetStatusDisplay(bet.BetStatus),
                    CreatedAt = bet.CreatedAt
                };
            }).ToList();

            // Calcula estadísticas
            var allUserBets = await _context.Bets
                .Where(b => b.Users.Any(u => u.UserId == userId))
                .ToListAsync();

            return new BetHistoryViewModel
            {
                Bets = betDetails,
                TotalBets = allUserBets.Count,
                WonBets = allUserBets.Count(b => b.BetStatus == "W"),
                LostBets = allUserBets.Count(b => b.BetStatus == "L"),
                PendingBets = allUserBets.Count(b => b.BetStatus == "P"),
                TotalStaked = allUserBets.Sum(b => b.Stake),
                TotalWon = allUserBets.Where(b => b.BetStatus == "W").Sum(b => b.Payout),
                NetProfit = allUserBets.Where(b => b.BetStatus == "W").Sum(b => b.Payout) -
                           allUserBets.Where(b => b.BetStatus == "L").Sum(b => b.Stake),
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling(totalBets / (double)pageSize),
                PageSize = pageSize
            };
        }

        public async Task<bool> CancelBetAsync(int betId, int userId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bet = await _context.Bets
                    .Include(b => b.Event)
                    .Include(b => b.Users)
                    .FirstOrDefaultAsync(b => b.BetId == betId &&
                                              b.Users.Any(u => u.UserId == userId) &&
                                              b.BetStatus == "P");

                if (bet == null) return false;

                // Verifica que el evento no haya comenzado
                if (bet.Event.Date <= DateTime.Now.AddMinutes(5))
                    return false;

                bet.BetStatus = "C"; // Cancelled
                bet.UpdatedAt = DateTime.Now;

                // Devuelve el dinero
                await _userService.UpdateUserBalanceAsync(userId, bet.Stake, "PAYOUT");

                // Crea transacción de devolución
                await _paymentService.CreateTransactionAsync(new PaymentTransactionRequest
                {
                    UserId = userId,
                    Amount = bet.Stake,
                    TransactionType = "REFUND",
                    RelatedBetId = bet.BetId
                });

                await _notificationService.SendNotificationAsync(userId,
                    $"Apuesta cancelada. Se devolvieron ${bet.Stake:N2} a tu cuenta");

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling bet {BetId}", betId);
                return false;
            }
        }

        public async Task<bool> SettleBetAsync(int betId, string outcome)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bet = await _context.Bets
                    .Include(b => b.Users)
                    .FirstOrDefaultAsync(b => b.BetId == betId && b.BetStatus == "P");

                if (bet == null) return false;

                bet.BetStatus = outcome; // "W" or "L"
                bet.UpdatedAt = DateTime.Now;

                if (outcome == "W" && bet.Users.Any())
                {
                    var userId = bet.Users.First().UserId;

                    // Paga ganancia
                    await _userService.UpdateUserBalanceAsync(userId, bet.Payout, "PAYOUT");

                    // Crea transacción
                    await _paymentService.CreateTransactionAsync(new PaymentTransactionRequest
                    {
                        UserId = userId,
                        Amount = bet.Payout,
                        TransactionType = "PAYOUT",
                        RelatedBetId = bet.BetId
                    });

                    await _notificationService.SendNotificationAsync(userId,
                        $"¡Felicidades! Ganaste ${bet.Payout:N2} en tu apuesta");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error settling bet {BetId}", betId);
                return false;
            }
        }

        public async Task<decimal> CalculatePotentialPayoutAsync(decimal stake, decimal odds)
        {
            return await Task.FromResult(stake * odds);
        }

        public async Task<List<Bet>> GetPendingBetsByEventAsync(int eventId)
        {
            return await _context.Bets
                .Include(b => b.Users)
                .Where(b => b.EventId == eventId && b.BetStatus == "P")
                .ToListAsync();
        }

        public async Task<bool> SettleEventBetsAsync(int eventId, int winningTeamId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var bets = await GetPendingBetsByEventAsync(eventId);

                foreach (var bet in bets)
                {
                    // Aquí se debería determinar si la apuesta fue ganadora basándose en el equipo
                    // Por ahora, asumimos que todas las apuestas son al equipo ganador
                    var isWinner = true; // Implementar lógica real

                    await SettleBetAsync(bet.BetId, isWinner ? "W" : "L");
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error settling event bets for eventId: {EventId}", eventId);
                return false;
            }
        }

        public async Task<Dictionary<string, decimal>> GetBettingStatisticsAsync(int userId)
        {
            var bets = await _context.Bets
                .Where(b => b.Users.Any(u => u.UserId == userId))
                .ToListAsync();

            return new Dictionary<string, decimal>
            {
                ["TotalBets"] = bets.Count,
                ["TotalStaked"] = bets.Sum(b => b.Stake),
                ["TotalWon"] = bets.Where(b => b.BetStatus == "W").Sum(b => b.Payout),
                ["TotalLost"] = bets.Where(b => b.BetStatus == "L").Sum(b => b.Stake),
                ["PendingBets"] = bets.Count(b => b.BetStatus == "P"),
                ["WinRate"] = bets.Count > 0 ?
                    (decimal)bets.Count(b => b.BetStatus == "W") / bets.Count * 100 : 0,
                ["AverageStake"] = bets.Count > 0 ? bets.Average(b => b.Stake) : 0,
                ["AverageOdds"] = bets.Count > 0 ? bets.Average(b => b.Odds) : 0
            };
        }

        public async Task<List<Bet>> GetActiveBetsByUserAsync(int userId)
        {
            return await _context.Bets
                .Include(b => b.Event)
                .Where(b => b.Users.Any(u => u.UserId == userId) && b.BetStatus == "P")
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> ValidateBetLimitsAsync(int userId, decimal amount)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null) return false;

            // Límites por rol
            var maxBetAmount = user.Role.RoleName switch
            {
                "VIP" => 50000m,
                "Premium" => 20000m,
                "Regular" => 5000m,
                _ => 1000m
            };

            if (amount > maxBetAmount) return false;

            // Límite diario
            var today = DateTime.Today;
            var todayTotal = await _context.Bets
                .Where(b => b.Users.Any(u => u.UserId == userId) && b.CreatedAt >= today)
                .SumAsync(b => b.Stake);

            var dailyLimit = user.Role.RoleName switch
            {
                "VIP" => 100000m,
                "Premium" => 50000m,
                "Regular" => 10000m,
                _ => 5000m
            };

            return (todayTotal + amount) <= dailyLimit;
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
    }
}