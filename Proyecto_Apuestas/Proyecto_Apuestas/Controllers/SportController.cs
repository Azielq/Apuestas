using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Controllers
{
    public class SportController : BaseController
    {
        private readonly apuestasDbContext _context;
        private readonly IEventService _eventService;

        public SportController(
            apuestasDbContext context,
            IEventService eventService,
            ILogger<SportController> logger) : base(logger)
        {
            _context = context;
            _eventService = eventService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var sports = await _context.Sports
                .Where(s => s.IsActive == true)
                .Select(s => new SportListViewModel
                {
                    SportId = s.SportId,
                    Name = s.Name,
                    IsActive = s.IsActive ?? false,
                    ActiveCompetitions = s.Competitions.Count(c => c.IsActive == true),
                    ActiveTeams = s.Teams.Count(t => t.IsActive == true),
                    UpcomingEvents = _context.Events
                        .Count(e => e.EventHasTeams.Any(et => et.Team.SportId == s.SportId) &&
                                   e.Date > DateTime.Now)
                })
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View(sports);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var sport = await _context.Sports
                .Include(s => s.Competitions)
                .Include(s => s.Teams)
                .FirstOrDefaultAsync(s => s.SportId == id && s.IsActive == true);

            if (sport == null)
            {
                return NotFound();
            }

            var upcomingEvents = await _eventService.GetUpcomingEventsAsync(sportId: id);

            var viewModel = new SportDetailsViewModel
            {
                SportId = sport.SportId,
                Name = sport.Name,
                Competitions = sport.Competitions
                    .Where(c => c.IsActive == true)
                    .Select(c => new CompetitionViewModel
                    {
                        CompetitionId = c.CompetitionId,
                        Name = c.Name,
                        SportName = sport.Name,
                        StartDate = c.StartDate,
                        EndDate = c.EndDate,
                        IsActive = c.IsActive ?? false
                    })
                    .OrderBy(c => c.Name)
                    .ToList(),
                TopTeams = sport.Teams
                    .Where(t => t.IsActive == true)
                    .OrderByDescending(t => t.TeamWinPercent)
                    .Take(10)
                    .Select(t => new ViewModels.TeamViewModel
                    {
                        TeamId = t.TeamId,
                        TeamName = t.TeamName,
                        SportName = sport.Name,
                        TeamWinPercent = t.TeamWinPercent,
                        TeamDrawPercent = t.TeamDrawPercent,
                        LastWin = t.LastWin,
                        LogoUrl = t.Images.FirstOrDefault()?.Url,
                        IsActive = t.IsActive ?? false
                    })
                    .ToList(),
                TotalEvents = upcomingEvents.Count,
                LiveEvents = upcomingEvents.Count(e => e.IsLive)
            };

            return View(viewModel);
        }
    }
}