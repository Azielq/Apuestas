using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Controllers
{
    public class EventController : BaseController
    {
        private readonly IEventService _eventService;
        private readonly IOddsService _oddsService;
        private readonly apuestasDbContext _context;

        public EventController(
            IEventService eventService,
            IOddsService oddsService,
            apuestasDbContext context,
            ILogger<EventController> logger) : base(logger)
        {
            _eventService = eventService;
            _oddsService = oddsService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? sportId = null, string? search = null)
        {
            var events = await _eventService.GetUpcomingEventsAsync(sportId, search);

            ViewBag.Sports = await _context.Sports
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .ToDictionaryAsync(s => s.SportId, s => s.Name);

            ViewBag.SelectedSportId = sportId;
            ViewBag.SearchTerm = search;

            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var eventDetails = await _eventService.GetEventDetailsAsync(id);
            if (eventDetails == null)
            {
                return NotFound();
            }

            //  NOTE: Esto obtiene el conteo de apuestas si el usuario está autenticado
            if (User.Identity?.IsAuthenticated == true)
            {
                var betCounts = await _eventService.GetEventBetCountsAsync(new List<int> { id });
                ViewBag.UserBetCount = betCounts.GetValueOrDefault(id, 0);
            }

            return View(eventDetails);
        }

        [HttpGet]
        public async Task<IActionResult> Live()
        {
            var liveEvents = await _eventService.GetLiveEventsAsync();
            return View(liveEvents);
        }

        [HttpGet]
        public async Task<IActionResult> Upcoming()
        {
            var upcomingEvents = await _eventService.GetUpcomingEventsByCategoryAsync();
            return View(upcomingEvents);
        }

        [HttpGet]
        public async Task<IActionResult> ByDate(DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            var events = await _eventService.GetEventsByDateRangeAsync(
                targetDate.Date,
                targetDate.Date.AddDays(1).AddSeconds(-1));

            ViewBag.SelectedDate = targetDate;
            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> Calendar(int? year = null, int? month = null)
        {
            var targetYear = year ?? DateTime.Now.Year;
            var targetMonth = month ?? DateTime.Now.Month;

            var startDate = new DateTime(targetYear, targetMonth, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var events = await _eventService.GetEventsByDateRangeAsync(startDate, endDate);

            ViewBag.Year = targetYear;
            ViewBag.Month = targetMonth;
            ViewBag.MonthName = startDate.ToString("MMMM");

            return View(events);
        }

        [HttpGet]
        public async Task<IActionResult> OddsHistory(int id)
        {
            var eventDetails = await _eventService.GetEventDetailsAsync(id);
            if (eventDetails == null)
            {
                return NotFound();
            }

            var oddsHistory = await _oddsService.GetOddsHistoryAsync(id, days: 30);
            var oddsAnalysis = await _oddsService.AnalyzeOddsPatternsAsync(id);

            ViewBag.EventDetails = eventDetails;
            ViewBag.OddsAnalysis = oddsAnalysis;

            return View(oddsHistory);
        }

        [HttpPost]
        public async Task<IActionResult> GetLatestOdds([FromBody] int eventId)
        {
            try
            {
                var odds = await _eventService.GetEventOddsAsync(eventId);
                return JsonSuccess(odds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest odds for event {EventId}", eventId);
                return JsonError("Error al obtener las cuotas");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                return PartialView("_SearchResults", new List<EventListViewModel>());
            }

            var results = await _eventService.GetUpcomingEventsAsync(searchTerm: query);
            return PartialView("_SearchResults", results.Take(10));
        }
    }
}