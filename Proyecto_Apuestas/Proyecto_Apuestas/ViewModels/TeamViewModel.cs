namespace Proyecto_Apuestas.ViewModels
{
    public class TeamViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public string SportName { get; set; }
        public decimal TeamWinPercent { get; set; }
        public decimal TeamDrawPercent { get; set; }
        public DateOnly? LastWin { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class TeamDetailsViewModel : TeamViewModel
    {
        public List<EventListViewModel> UpcomingEvents { get; set; }
        public List<EventListViewModel> RecentEvents { get; set; }
        public Dictionary<string, decimal> PerformanceStats { get; set; }
        public List<OddsHistoryViewModel> OddsHistory { get; set; }
    }

    public class OddsHistoryViewModel
    {
        public DateTime Date { get; set; }
        public decimal Odds { get; set; }
        public string EventName { get; set; }
        public string Result { get; set; }
    }
}