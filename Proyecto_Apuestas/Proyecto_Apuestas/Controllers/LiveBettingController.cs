using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services.Interfaces;

namespace Proyecto_Apuestas.Controllers
{
    public class LiveBettingController : BaseController
    {
        private readonly IEventService _eventService;
        private readonly IBettingService _bettingService;
        private readonly IOddsService _oddsService;

        public LiveBettingController(
            IEventService eventService,
            IBettingService bettingService,
            IOddsService oddsService,
            ILogger<LiveBettingController> logger) : base(logger)
        {
            _eventService = eventService;
            _bettingService = bettingService;
            _oddsService = oddsService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var liveEvents = await _eventService.GetLiveEventsAsync();
                return View(liveEvents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading live events");
                AddErrorMessage("Error al cargar eventos en vivo. Por favor intenta nuevamente.");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Event(int id)
        {
            try
            {
                var eventDetails = await _eventService.GetEventDetailsAsync(id);
                if (eventDetails == null)
                {
                    return NotFound();
                }

                return View(eventDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading event details for event {EventId}", id);
                AddErrorMessage("Error al cargar los detalles del evento.");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLiveOdds(int eventId)
        {
            try
            {
                var odds = await _oddsService.GetLatestOddsForEventAsync(eventId);
                return Json(new { success = true, data = odds });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting live odds for event {EventId}", eventId);
                return Json(new { success = false, message = "Error obteniendo odds en vivo" });
            }
        }
    }
}