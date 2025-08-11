using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using System.Collections.Generic;
using Proyecto_Apuestas.ViewModels.API;

namespace Proyecto_Apuestas.Controllers
{
    public class OddsApiController : BaseController
    {
        private readonly IOddsApiService _oddsApiService;
        private readonly IEventService _eventService;
        private readonly IBettingService _bettingService;
        private readonly IUserService _userService;

        public OddsApiController(
            IOddsApiService oddsApiService,
            IEventService eventService,
            IBettingService bettingService,
            IUserService userService,
            ILogger<OddsApiController> logger) : base(logger)
        {
            _oddsApiService = oddsApiService;
            _eventService = eventService;
            _bettingService = bettingService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var sports = await _oddsApiService.GetSportsAsync();
            var apiUsage = await _oddsApiService.GetApiUsageAsync();

            var viewModel = new OddsApiDashboardViewModel
            {
                ActiveSports = sports.Where(s => s.Active).ToList(),
                ApiUsage = apiUsage,
                IsApiAvailable = await _oddsApiService.IsApiAvailableAsync()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Sports()
        {
            var sports = await _oddsApiService.GetSportsAsync();
            var activeSports = sports.Where(s => s.Active)
                .OrderBy(s => s.Group)
                .ThenBy(s => s.Title)
                .ToList();

            return View(activeSports);
        }

        [HttpGet]
        public async Task<IActionResult> Events(string sportKey, string? region = "us", string? market = "h2h")
        {
            if (string.IsNullOrEmpty(sportKey))
            {
                return RedirectToAction(nameof(Sports));
            }

            var sport = await _oddsApiService.GetSportAsync(sportKey);
            var events = await _oddsApiService.GetOddsAsync(sportKey, region, market);

            var viewModel = new SportEventsViewModel
            {
                Sport = sport,
                Events = events.OrderBy(e => e.CommenceTime).ToList(),
                Region = region,
                Market = market,
                SportKey = sportKey
            };

            ViewBag.Regions = new Dictionary<string, string>
            {
                ["us"] = "Estados Unidos",
                ["uk"] = "Reino Unido",
                ["eu"] = "Europa",
                ["au"] = "Australia"
            };

            ViewBag.Markets = new Dictionary<string, string>
            {
                ["h2h"] = "Ganador del Partido",
                ["spreads"] = "Handicap",
                ["totals"] = "Total de Puntos/Goles"
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> EventDetails(string sportKey, string eventId)
        {
            var eventDetails = await _oddsApiService.GetEventAsync(sportKey, eventId);
            if (eventDetails == null)
            {
                return NotFound();
            }

            var viewModel = new OddsEventDetailsViewModel
            {
                Event = eventDetails,
                SportKey = sportKey
            };

            // Si el usuario está autenticado, verificar si puede apostar
            if (User.Identity?.IsAuthenticated == true)
            {
                var user = await _userService.GetCurrentUserAsync();
                viewModel.UserBalance = user?.CreditBalance ?? 0;
                viewModel.CanBet = viewModel.UserBalance > 0;
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> LiveEvents()
        {
            var liveEventsDict = new Dictionary<string, List<Models.API.EventApiModel>>();
            var sports = await _oddsApiService.GetSportsAsync();

            foreach (var sport in sports.Where(s => s.Active))
            {
                var liveEvents = await _oddsApiService.GetLiveEventsAsync(sport.Key);
                if (liveEvents.Any())
                {
                    liveEventsDict[sport.Title] = liveEvents;
                }
            }

            return View(liveEventsDict);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CreateBetFromApi(string sportKey, string eventId, string teamName, decimal odds)
        {
            var apiEvent = await _oddsApiService.GetEventAsync(sportKey, eventId);
            if (apiEvent == null)
            {
                return NotFound();
            }

            var user = await _userService.GetCurrentUserAsync();

            var model = new CreateBetFromApiViewModel
            {
                ApiEventId = eventId,
                SportKey = sportKey,
                EventName = $"{apiEvent.HomeTeam} vs {apiEvent.AwayTeam}",
                EventDate = apiEvent.CommenceTime,
                TeamName = teamName,
                Odds = odds,
                UserBalance = user?.CreditBalance ?? 0
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBetFromApi(CreateBetFromApiViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var user = await _userService.GetCurrentUserAsync();
                model.UserBalance = user?.CreditBalance ?? 0;
                return View(model);
            }

            // Primero sincronizar el evento con la base de datos local
            await _oddsApiService.SyncEventsWithDatabaseAsync(model.SportKey);

            // Buscar el evento local
            var localEvent = await _eventService.GetEventByExternalIdAsync(model.ApiEventId);
            if (localEvent == null)
            {
                AddErrorMessage("No se pudo procesar la apuesta. Intente nuevamente.");
                return View(model);
            }

            // Crear la apuesta usando el sistema existente
            var betModel = new CreateBetViewModel
            {
                EventId = localEvent.EventId,
                TeamId = localEvent.Teams.First(t => t.TeamName == model.TeamName).TeamId,
                Stake = model.Stake,
                Odds = model.Odds
            };

            var result = await _bettingService.PlaceBetAsync(betModel);

            if (result.Success)
            {
                AddSuccessMessage("¡Apuesta realizada con éxito!");
                return RedirectToAction("Details", "Betting", new { id = result.BetId });
            }

            AddModelErrors(result.ErrorMessage ?? "Error al procesar la apuesta");
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SyncSports()
        {
            var success = await _oddsApiService.SyncSportsWithDatabaseAsync();

            if (success)
            {
                return JsonSuccess("Deportes sincronizados exitosamente");
            }

            return JsonError("Error al sincronizar deportes");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SyncEvents(string sportKey)
        {
            if (string.IsNullOrEmpty(sportKey))
            {
                return JsonError("Debe especificar un deporte");
            }

            var success = await _oddsApiService.SyncEventsWithDatabaseAsync(sportKey);

            if (success)
            {
                return JsonSuccess("Eventos sincronizados exitosamente");
            }

            return JsonError("Error al sincronizar eventos");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOdds(int eventId, string apiEventId)
        {
            var success = await _oddsApiService.UpdateOddsForEventAsync(eventId, apiEventId);

            if (success)
            {
                return JsonSuccess("Cuotas actualizadas exitosamente");
            }

            return JsonError("Error al actualizar cuotas");
        }

        [HttpGet]
        public async Task<IActionResult> ApiStatus()
        {
            var usage = await _oddsApiService.GetApiUsageAsync();
            var isAvailable = await _oddsApiService.IsApiAvailableAsync();

            return PartialView("_ApiStatus", new ApiStatusViewModel
            {
                IsAvailable = isAvailable,
                RequestsUsed = usage.RequestsUsed,
                RequestsRemaining = usage.RequestsRemaining,
                UsagePercentage = usage.RequestsUsed > 0
                    ? (decimal)usage.RequestsUsed / (usage.RequestsUsed + usage.RequestsRemaining) * 100
                    : 0
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchEvents(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                return PartialView("_EventSearchResults", new List<Models.API.OddsApiModel>());
            }

            var allEvents = new List<Models.API.OddsApiModel>();
            var sports = await _oddsApiService.GetSportsAsync();

            // Buscar en los deportes más populares
            var popularSports = new[] { "soccer_epl", "basketball_nba", "baseball_mlb", "americanfootball_nfl" };

            foreach (var sportKey in popularSports)
            {
                // GetOddsAsync devuelve List<OddsApiModel>, no List<EventApiModel>
                var odds = await _oddsApiService.GetOddsAsync(sportKey, "us", "h2h");
                var filtered = odds.Where(e =>
                    e.HomeTeam.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    e.AwayTeam.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(5);

                allEvents.AddRange(filtered);
            }

            return PartialView("_EventSearchResults", allEvents.Take(10).ToList());
        }
    }


}