using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Controllers
{
    public class TeamController : BaseController
    {
        private readonly apuestasDbContext _context;
        private readonly IEventService _eventService;
        private readonly IOddsService _oddsService;

        public TeamController(
            apuestasDbContext context,
            IEventService eventService,
            IOddsService oddsService,
            ILogger<TeamController> logger) : base(logger)
        {
            _context = context;
            _eventService = eventService;
            _oddsService = oddsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? sportId = null)
        {
            var query = _context.Teams
                .Include(t => t.Sport)
                .Include(t => t.Images)
                .Where(t => t.IsActive == true);

            if (sportId.HasValue)
            {
                query = query.Where(t => t.SportId == sportId.Value);
            }

            var teams = await query
                .Select(t => new TeamViewModel
                {
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    SportName = t.Sport.Name,
                    TeamWinPercent = t.TeamWinPercent,
                    TeamDrawPercent = t.TeamDrawPercent,
                    LastWin = t.LastWin,
                    LogoUrl = t.Images.FirstOrDefault().Url,
                    IsActive = t.IsActive ?? false
                })
                .OrderBy(t => t.SportName)
                .ThenBy(t => t.TeamName)
                .ToListAsync();

            ViewBag.Sports = await _context.Sports
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .ToDictionaryAsync(s => s.SportId, s => s.Name);

            ViewBag.SelectedSportId = sportId;

            return View(teams);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var team = await _context.Teams
                .Include(t => t.Sport)
                .Include(t => t.Images)
                .Include(t => t.EventHasTeams)
                    .ThenInclude(et => et.Event)
                .FirstOrDefaultAsync(t => t.TeamId == id && t.IsActive == true);

            if (team == null)
            {
                return NotFound();
            }

            // Eventos próximos
            var upcomingEvents = team.EventHasTeams
                .Where(et => et.Event.Date > DateTime.Now)
                .OrderBy(et => et.Event.Date)
                .Take(5)
                .Select(et => et.Event)
                .ToList();

            // Eventos recientes
            var recentEvents = team.EventHasTeams
                .Where(et => et.Event.Date <= DateTime.Now && !string.IsNullOrEmpty(et.Event.Outcome))
                .OrderByDescending(et => et.Event.Date)
                .Take(10)
                .Select(et => et.Event)
                .ToList();

            // Estadísticas de rendimiento
            var performanceStats = new Dictionary<string, decimal>
            {
                ["TotalGames"] = team.EventHasTeams.Count(et => !string.IsNullOrEmpty(et.Event.Outcome)),
                ["HomeGames"] = team.EventHasTeams.Count(et => et.IsHomeTeam && !string.IsNullOrEmpty(et.Event.Outcome)),
                ["AwayGames"] = team.EventHasTeams.Count(et => !et.IsHomeTeam && !string.IsNullOrEmpty(et.Event.Outcome)),
                ["AverageOdds"] = await _oddsService.GetAverageOddsAsync(team.TeamId, team.SportId)
            };

            var viewModel = new TeamDetailsViewModel
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                SportName = team.Sport.Name,
                TeamWinPercent = team.TeamWinPercent,
                TeamDrawPercent = team.TeamDrawPercent,
                LastWin = team.LastWin,
                LogoUrl = team.Images.FirstOrDefault()?.Url,
                IsActive = team.IsActive ?? false,
                UpcomingEvents = await MapEventsToViewModels(upcomingEvents),
                RecentEvents = await MapEventsToViewModels(recentEvents),
                PerformanceStats = performanceStats,
                OddsHistory = await GetTeamOddsHistory(team.TeamId)
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Compare(int team1Id, int team2Id)
        {
            var team1 = await GetTeamDetailsAsync(team1Id);
            var team2 = await GetTeamDetailsAsync(team2Id);

            if (team1 == null || team2 == null)
            {
                return NotFound();
            }

            ViewBag.Team1 = team1;
            ViewBag.Team2 = team2;

            // Aquí se podría agregar más lógica de comparación
            return View();
        }

        // Métodos privados
        private async Task<List<EventListViewModel>> MapEventsToViewModels(List<Models.Event> events)
        {
            var eventIds = events.Select(e => e.EventId).ToList();
            var allEventTeams = await _context.EventHasTeams
                .Include(et => et.Team)
                    .ThenInclude(t => t.Sport)
                .Include(et => et.Team)
                    .ThenInclude(t => t.Images)
                .Where(et => eventIds.Contains(et.EventId))
                .ToListAsync();

            return events.Select(e =>
            {
                var eventTeams = allEventTeams.Where(et => et.EventId == e.EventId).ToList();
                var teams = eventTeams.Select(et => et.Team).ToList();
                var sport = teams.FirstOrDefault()?.Sport;

                return new EventListViewModel
                {
                    EventId = e.EventId,
                    ExternalEventId = e.ExternalEventId,
                    Date = e.Date,
                    SportName = sport?.Name ?? "N/A",
                    CompetitionName = "Liga Principal",
                    Teams = teams.Select(t => new EventTeamViewModel
                    {
                        TeamId = t.TeamId,
                        TeamName = t.TeamName,
                        CurrentOdds = 2.0m, // Placeholder
                        LogoUrl = t.Images.FirstOrDefault()?.Url,
                        IsHomeTeam = eventTeams.First(et => et.TeamId == t.TeamId).IsHomeTeam
                    }).ToList(),
                    IsLive = e.Date <= DateTime.Now && e.Date >= DateTime.Now.AddHours(-3),
                    IsFinished = !string.IsNullOrEmpty(e.Outcome),
                    Outcome = e.Outcome
                };
            }).ToList();
        }

        private async Task<TeamDetailsViewModel?> GetTeamDetailsAsync(int teamId)
        {
            var team = await _context.Teams
                .Include(t => t.Sport)
                .Include(t => t.Images)
                .FirstOrDefaultAsync(t => t.TeamId == teamId && t.IsActive == true);

            if (team == null) return null;

            return new TeamDetailsViewModel
            {
                TeamId = team.TeamId,
                TeamName = team.TeamName,
                SportName = team.Sport.Name,
                TeamWinPercent = team.TeamWinPercent,
                TeamDrawPercent = team.TeamDrawPercent,
                LastWin = team.LastWin,
                LogoUrl = team.Images.FirstOrDefault()?.Url,
                IsActive = team.IsActive ?? false
            };
        }

        private async Task<List<OddsHistoryViewModel>> GetTeamOddsHistory(int teamId)
        {
            var oddsHistory = await _context.OddsHistories
                .Include(o => o.Event)
                    .ThenInclude(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                .Where(o => o.TeamId == teamId)
                .OrderByDescending(o => o.RetrievedAt)
                .Take(20)
                .ToListAsync();

            return oddsHistory.Select(o => new OddsHistoryViewModel
            {
                Date = o.RetrievedAt,
                Odds = o.Odds,
                EventName = string.Join(" vs ", o.Event.EventHasTeams.Select(et => et.Team.TeamName)),
                Result = o.Event.Outcome ?? "Pendiente"
            }).ToList();
        }
    }
}