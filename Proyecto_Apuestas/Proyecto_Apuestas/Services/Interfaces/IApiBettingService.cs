using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.ViewModels.API;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IApiBettingService
    {
        Task<ApiBetResult> PlaceBetAsync(CreateBetFromApiViewModel model);
        Task<ApiBetDetailsViewModel?> GetApiBetDetailsAsync(int apiBetId, int userId);
        Task<ApiBetHistoryViewModel> GetUserApiBetHistoryAsync(int userId, int page = 1, int pageSize = 20, ApiBetHistoryFilter? filter = null);
        Task<bool> CancelApiBetAsync(int apiBetId, int userId);
        Task<bool> SettleApiBetAsync(int apiBetId, string outcome, string? eventResult = null);
        Task<decimal> CalculatePotentialPayoutAsync(decimal stake, decimal odds);
        Task<List<ApiBet>> GetPendingApiBetsByEventAsync(string apiEventId);
        Task<bool> SettleEventApiBetsAsync(string apiEventId, string eventResult);
        Task<Dictionary<string, decimal>> GetApiBettingStatisticsAsync(int userId);
        Task<List<ApiBet>> GetActiveApiBetsByUserAsync(int userId);
        Task<bool> ValidateBetLimitsAsync(int userId, decimal amount);
    }

    public class ApiBetResult
    {
        public bool Success { get; set; }
        public int? ApiBetId { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, string>? ValidationErrors { get; set; }
    }

    public class ApiBetHistoryFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? SportKey { get; set; }
    }

    public class ApiBetDetailsViewModel
    {
        public int ApiBetId { get; set; }
        public string ApiEventId { get; set; }
        public string SportKey { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string TeamName { get; set; }
        public decimal Odds { get; set; }
        public decimal Stake { get; set; }
        public decimal Payout { get; set; }
        public string BetStatus { get; set; }
        public string BetStatusDisplay { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? TransactionStatus { get; set; }
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public string? Region { get; set; }
        public string? Market { get; set; }
        public string? Bookmaker { get; set; }
        public string? EventResult { get; set; }
        public bool IsEventFinished { get; set; }
    }

    public class ApiBetHistoryViewModel
    {
        public List<ApiBetDetailsViewModel> ApiBets { get; set; } = new();
        public int TotalBets { get; set; }
        public int WonBets { get; set; }
        public int LostBets { get; set; }
        public int PendingBets { get; set; }
        public decimal TotalStaked { get; set; }
        public decimal TotalWon { get; set; }
        public decimal NetProfit { get; set; }

        // Paginación
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }

        // Filtros
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? Status { get; set; }
        public string? SportKey { get; set; }
    }
}