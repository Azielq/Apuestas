using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IBettingService
    {
        Task<BetResult> PlaceBetAsync(CreateBetViewModel model);
        Task<BetResult> PlaceMultipleBetsAsync(BetSlipViewModel model);
        Task<BetDetailsViewModel?> GetBetDetailsAsync(int betId, int userId);
        Task<BetHistoryViewModel> GetUserBetHistoryAsync(int userId, int page = 1, int pageSize = 20, BetHistoryFilter? filter = null);
        Task<bool> CancelBetAsync(int betId, int userId);
        Task<bool> SettleBetAsync(int betId, string outcome);
        Task<decimal> CalculatePotentialPayoutAsync(decimal stake, decimal odds);
        Task<List<Bet>> GetPendingBetsByEventAsync(int eventId);
        Task<bool> SettleEventBetsAsync(int eventId, int winningTeamId);
        Task<Dictionary<string, decimal>> GetBettingStatisticsAsync(int userId);
        Task<List<Bet>> GetActiveBetsByUserAsync(int userId);
        Task<bool> ValidateBetLimitsAsync(int userId, decimal amount);
    }

    public class BetResult
    {
        public bool Success { get; set; }
        public int? BetId { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string>? ValidationErrors { get; set; }
    }

    public class BetHistoryFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public int? SportId { get; set; }
    }
}
