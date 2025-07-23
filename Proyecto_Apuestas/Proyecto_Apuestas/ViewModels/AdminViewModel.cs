namespace Proyecto_Apuestas.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int TotalBets { get; set; }
        public int TodayBets { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal TotalPayouts { get; set; }
        public decimal HouseEdge { get; set; }

        public List<ChartDataPoint> RevenueChart { get; set; }
        public List<ChartDataPoint> BetsChart { get; set; }
        public List<SportStatsViewModel> SportStats { get; set; }
        public List<RecentActivityViewModel> RecentActivities { get; set; }
    }

    public class ChartDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
    }

    public class SportStatsViewModel
    {
        public string SportName { get; set; }
        public int TotalBets { get; set; }
        public decimal TotalStaked { get; set; }
        public decimal TotalPayout { get; set; }
        public decimal Profit { get; set; }
        public decimal ProfitMargin { get; set; }
    }

    public class RecentActivityViewModel
    {
        public DateTime Timestamp { get; set; }
        public string ActivityType { get; set; }
        public string Description { get; set; }
        public string UserName { get; set; }
        public decimal? Amount { get; set; }
    }

    public class UserManagementViewModel
    {
        public List<UserAdminViewModel> Users { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int LockedUsers { get; set; }

        // Paginación
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }

        // Filtros
        public string? SearchTerm { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; }
    }

    public class UserAdminViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }
        public decimal CreditBalance { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LockedUntil { get; set; }
        public DateTime? LastBet { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalBets { get; set; }
        public decimal TotalWagered { get; set; }
    }

    public class EventManagementViewModel
    {
        public List<EventAdminViewModel> Events { get; set; }
        public Dictionary<int, string> Sports { get; set; }
        public Dictionary<int, string> Competitions { get; set; }

        // Paginación y filtros
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int? SportId { get; set; }
        public int? CompetitionId { get; set; }
        public string? Status { get; set; }
    }

    public class EventAdminViewModel
    {
        public int EventId { get; set; }
        public string? ExternalEventId { get; set; }
        public DateTime Date { get; set; }
        public string Outcome { get; set; }
        public string Teams { get; set; }
        public string Sport { get; set; }
        public string Competition { get; set; }
        public int TotalBets { get; set; }
        public decimal TotalStaked { get; set; }
        public bool CanEdit { get; set; }
        public bool CanSettle { get; set; }
    }

    public class ReportViewModel
    {
        public string ReportType { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Dictionary<string, decimal> Summary { get; set; }
        public List<Dictionary<string, object>> DetailedData { get; set; }
        public byte[]? ExportData { get; set; }
    }
}