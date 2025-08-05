namespace Proyecto_Apuestas.ViewModels
{
    public class CreateBetViewModel
    {
        public int EventId { get; set; }
        public int TeamId { get; set; }
        public decimal Stake { get; set; }
        public decimal Odds { get; set; }
        public decimal PotentialPayout { get; set; }

        // Información adicional para la vista
        public string? EventName { get; set; }
        public string? TeamName { get; set; }
        public DateTime EventDate { get; set; }
        public decimal UserBalance { get; set; }
    }

    public class BetDetailsViewModel
    {
        public int BetId { get; set; }
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

        // Información del evento
        public string? EventOutcome { get; set; }
        public bool IsEventFinished { get; set; }
    }

    public class BetHistoryViewModel
    {
        public List<BetDetailsViewModel> Bets { get; set; }
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
        public int? SportId { get; set; }
    }

    public class BetSlipViewModel
    {
        public List<BetSlipItemViewModel> Items { get; set; } = new List<BetSlipItemViewModel>();
        public decimal TotalStake { get; set; }
        public decimal TotalPotentialPayout { get; set; }
        public decimal UserBalance { get; set; }
        public bool CanPlaceBet { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class BetSlipItemViewModel
    {
        public int EventId { get; set; }
        public int TeamId { get; set; }
        public string EventName { get; set; }
        public string TeamName { get; set; }
        public DateTime EventDate { get; set; }
        public decimal Odds { get; set; }
        public decimal Stake { get; set; }
        public decimal PotentialPayout { get; set; }
    }
}