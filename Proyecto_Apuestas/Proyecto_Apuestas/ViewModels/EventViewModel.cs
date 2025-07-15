namespace Proyecto_Apuestas.ViewModels
{
    public class EventListViewModel
    {
        public int EventId { get; set; }
        public string? ExternalEventId { get; set; }
        public DateTime Date { get; set; }
        public string SportName { get; set; }
        public string CompetitionName { get; set; }
        public List<EventTeamViewModel> Teams { get; set; }
        public bool IsLive { get; set; }
        public bool IsFinished { get; set; }
        public string? Outcome { get; set; }
    }

    public class EventDetailsViewModel
    {
        public int EventId { get; set; }
        public string? ExternalEventId { get; set; }
        public DateTime Date { get; set; }
        public string Outcome { get; set; }
        public List<EventTeamDetailsViewModel> Teams { get; set; }
        public List<OddsViewModel> CurrentOdds { get; set; }
        public string SportName { get; set; }
        public string CompetitionName { get; set; }
        public int TotalBets { get; set; }
        public decimal TotalStaked { get; set; }
        public bool CanBet { get; set; }
    }

    public class EventTeamViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public decimal CurrentOdds { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsHomeTeam { get; set; }
    }

    public class EventTeamDetailsViewModel : EventTeamViewModel
    {
        public decimal TeamWinPercent { get; set; }
        public decimal TeamDrawPercent { get; set; }
        public DateOnly? LastWin { get; set; }
        public List<decimal> OddsHistory { get; set; }
    }

    public class OddsViewModel
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public decimal Odds { get; set; }
        public string Source { get; set; }
        public DateTime RetrievedAt { get; set; }
        public decimal? PreviousOdds { get; set; }
        public string Trend { get; set; } // "up", "down", "stable"
    }

    public class UpcomingEventsViewModel
    {
        public List<EventListViewModel> TodayEvents { get; set; }
        public List<EventListViewModel> TomorrowEvents { get; set; }
        public List<EventListViewModel> WeekEvents { get; set; }
        public Dictionary<int, string> Sports { get; set; }
        public int? SelectedSportId { get; set; }
        public string? SearchTerm { get; set; }
    }
}