namespace Proyecto_Apuestas.ViewModels
{
    public class CompetitionViewModel
    {
        public int CompetitionId { get; set; }
        public string Name { get; set; }
        public string SportName { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public bool IsActive { get; set; }
        public string? LogoUrl { get; set; }
        public int TotalEvents { get; set; }
        public int UpcomingEvents { get; set; }
    }

    public class CompetitionDetailsViewModel : CompetitionViewModel
    {
        public List<EventListViewModel> UpcomingEvents { get; set; }
        public List<TeamViewModel> Teams { get; set; }
        public Dictionary<string, decimal> BettingStats { get; set; }
    }
}