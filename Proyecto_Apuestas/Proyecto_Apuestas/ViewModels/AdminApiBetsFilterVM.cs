namespace Proyecto_Apuestas.ViewModels
{
    public class AdminApiBetsFilterVM
    {
        public string? SportKey { get; set; }
        public string? Region { get; set; }
        public string? Market { get; set; }
        public string? Bookmaker { get; set; }
        public string? Q { get; set; }
        public bool LiveOnly { get; set; } = false;
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    public class AdminApiBetRowVM
    {
        public int ApiBetId { get; set; }
        public string ApiEventId { get; set; } = "";
        public string SportKey { get; set; } = "";
        public string EventName { get; set; } = "";
        public string? HomeTeam { get; set; }
        public string? AwayTeam { get; set; }
        public string? TeamName { get; set; }
        public string? Region { get; set; }
        public string? Market { get; set; }
        public string? Bookmaker { get; set; }
        public DateTime EventDate { get; set; }
        public decimal Odds { get; set; }
        public decimal Stake { get; set; }
        public decimal Payout { get; set; }
        public string BetStatus { get; set; } = "P";
        public int? PaymentTransactionId { get; set; }

        // <- estas dos propiedades son las que usa tu vista
        public int UsersCount { get; set; }
        public List<string> UserNames { get; set; } = new();
    }

    public class AdminApiBetsViewVM
    {
        public List<AdminApiBetRowVM> Items { get; set; } = new();
        public AdminApiBetsFilterVM Filters { get; set; } = new();
        public int TotalItems { get; set; }
        public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalItems / Math.Max(1, Filters.PageSize)));
    }
}
