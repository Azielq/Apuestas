namespace Proyecto_Apuestas.ViewModels
{
    public class SportListViewModel
    {
        public int SportId { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }
        public int ActiveCompetitions { get; set; }
        public int ActiveTeams { get; set; }
        public int UpcomingEvents { get; set; }
    }

    public class SportDetailsViewModel
    {
        public int SportId { get; set; }
        public string Name { get; set; }
        public List<CompetitionViewModel> Competitions { get; set; }
        public List<TeamViewModel> TopTeams { get; set; }
        public int TotalEvents { get; set; }
        public int LiveEvents { get; set; }
    }
}