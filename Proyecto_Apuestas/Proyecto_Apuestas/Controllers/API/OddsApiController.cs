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
        private readonly IApiBettingService _apiBettingService;
        private readonly IUserService _userService;

        public OddsApiController(
            IOddsApiService oddsApiService,
            IEventService eventService,
            IBettingService bettingService,
            IApiBettingService apiBettingService,
            IUserService userService,
            ILogger<OddsApiController> logger) : base(logger)
        {
            _oddsApiService = oddsApiService;
            _eventService = eventService;
            _bettingService = bettingService;
            _apiBettingService = apiBettingService;
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
        public async Task<IActionResult> CreateBetFromApi(string sportKey, string ApiEventId, string teamName, decimal odds)
        {
            var apiEvent = await _oddsApiService.GetEventAsync(sportKey, ApiEventId);
            if (apiEvent == null)
            {
                return NotFound();
            }

            var user = await _userService.GetCurrentUserAsync();

            var model = new CreateBetFromApiViewModel
            {
                ApiEventId = ApiEventId,
                SportKey = sportKey,
                EventName = $"{apiEvent.HomeTeam} vs {apiEvent.AwayTeam}",
                EventDate = apiEvent.CommenceTime,
                TeamName = teamName,
                Odds = odds,
                UserBalance = user?.CreditBalance ?? 0
            };

            // Prefijar un monto por defecto válido para facilitar la confirmación
            var preferred = 1000m;
            var defaultStake = Math.Min(model.UserBalance, preferred);
            model.Stake = defaultStake >= 100m ? defaultStake : model.UserBalance >= 100m ? 100m : 0m;

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBetFromApi(CreateBetFromApiViewModel model)
        {
            var currentUserId = _userService.GetCurrentUserId();
            _logger.LogInformation("CreateBetFromApi POST called with model: ApiEventId={ApiEventId}, Stake={Stake}, TeamName={TeamName}, Odds={Odds}, UserId={UserId}", 
                model.ApiEventId, model.Stake, model.TeamName, model.Odds, currentUserId);

            // Verificar que el usuario esté autenticado correctamente
            if (currentUserId <= 0)
            {
                _logger.LogError("Usuario no autenticado o ID de usuario inválido: {UserId}", currentUserId);
                AddErrorMessage("Error de autenticación. Por favor, inicie sesión nuevamente.");
                return RedirectToAction("Login", "Account");
            }

            // Log form data for debugging
            _logger.LogInformation("Form data received: {FormData}", 
                string.Join(", ", Request.Form.Select(kv => $"{kv.Key}={kv.Value}")));

            // Manual handling of decimal parsing for odds if needed
            if (Request.Form.ContainsKey("Odds"))
            {
                var oddsValue = Request.Form["Odds"].ToString();
                _logger.LogInformation("Raw odds value from form: {OddsValue}", oddsValue);
                
                if (decimal.TryParse(oddsValue.Replace('.', ','), out var parsedOdds))
                {
                    model.Odds = parsedOdds;
                    ModelState.Remove("Odds"); // Remove any validation errors for odds
                    _logger.LogInformation("Successfully parsed odds with comma: {Odds}", parsedOdds);
                }
                else if (decimal.TryParse(oddsValue, System.Globalization.CultureInfo.InvariantCulture, out parsedOdds))
                {
                    model.Odds = parsedOdds;
                    ModelState.Remove("Odds"); // Remove any validation errors for odds
                    _logger.LogInformation("Successfully parsed odds with invariant culture: {Odds}", parsedOdds);
                }
                else
                {
                    _logger.LogWarning("Failed to parse odds value: {OddsValue}", oddsValue);
                }
            }

            // Obtener usuario actual al inicio para validaciones
            var currentUser = await _userService.GetCurrentUserAsync();
            if (currentUser == null)
            {
                _logger.LogError("Current user not found");
                AddErrorMessage("Error de autenticación. Por favor, inicie sesión nuevamente.");
                return RedirectToAction("Login", "Account");
            }

            // Actualizar saldo del modelo
            model.UserBalance = currentUser.CreditBalance;

            // Validaciones adicionales del servidor
            if (model.Stake < 100)
            {
                ModelState.AddModelError(nameof(model.Stake), "El monto mínimo para apostar es ₡100");
            }

            if (model.Stake > currentUser.CreditBalance)
            {
                ModelState.AddModelError(nameof(model.Stake), "No tienes saldo suficiente para esta apuesta");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid for user {UserId}: {Errors}", 
                    _userService.GetCurrentUserId(),
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                
                AddErrorMessage("Por favor, corrija los errores en el formulario.");
                return View(model);
            }

            try
            {
                _logger.LogInformation("Calling ApiBettingService.PlaceBetAsync for user {UserId} with stake {Stake}", 
                    _userService.GetCurrentUserId(), model.Stake);
                
                // Usar el servicio de apuestas de API directamente sin sincronización
                var result = await _apiBettingService.PlaceBetAsync(model);

                _logger.LogInformation("PlaceBetAsync result: Success={Success}, ApiBetId={ApiBetId}, Error={Error}", 
                    result.Success, result.ApiBetId, result.ErrorMessage);

                if (result.Success && result.ApiBetId.HasValue)
                {
                    AddSuccessMessage($"¡Apuesta #{result.ApiBetId} realizada con éxito! Tu apuesta de ₡{model.Stake:N0} ha sido procesada.");
                    _logger.LogInformation("Bet created successfully with id={ApiBetId} for user {UserId}", 
                        result.ApiBetId, _userService.GetCurrentUserId());
                    
                    _logger.LogInformation("Redirecting to ApiBetDetails with URL: /OddsApi/ApiBetDetails/{ApiBetId}", result.ApiBetId);
                    
                    // Redirección explícita a la ruta con atributo: OddsApi/ApiBetDetails/{id}
                    return Redirect($"/OddsApi/ApiBetDetails/{result.ApiBetId}");
                }

                // Si llegamos aquí, la apuesta falló
                _logger.LogWarning("Bet placement failed for user {UserId}: Success={Success}, ApiBetId={ApiBetId}, Error={Error}", 
                    _userService.GetCurrentUserId(), result.Success, result.ApiBetId, result.ErrorMessage);
                
                var errorMessage = result.ErrorMessage ?? "Error desconocido al procesar la apuesta";
                AddErrorMessage($"No se pudo procesar tu apuesta: {errorMessage}");
                
                // Actualizar saldo del usuario en caso de que haya cambiado
                var updatedUser = await _userService.GetCurrentUserAsync();
                model.UserBalance = updatedUser?.CreditBalance ?? 0;
                
                _logger.LogInformation("Returning to CreateBetFromApi view with error message");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while creating API bet for user {UserId}: {Message}", 
                    _userService.GetCurrentUserId(), ex.Message);
                
                AddErrorMessage("Error interno del sistema. Por favor, intente nuevamente en unos momentos.");
                
                // Actualizar saldo del usuario
                var updatedUser = await _userService.GetCurrentUserAsync();
                model.UserBalance = updatedUser?.CreditBalance ?? 0;
                
                return View(model);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("OddsApi/ApiBetDetails/{id}")]
        public async Task<IActionResult> ApiBetDetails(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var apiBetDetails = await _apiBettingService.GetApiBetDetailsAsync(id, userId);

            if (apiBetDetails == null)
            {
                AddErrorMessage("Apuesta no encontrada.");
                return RedirectToAction("Index");
            }

            return View(apiBetDetails);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> ApiBetHistory(int page = 1, string? status = null, string? sportKey = null)
        {
            var userId = _userService.GetCurrentUserId();
            var filter = new ApiBetHistoryFilter
            {
                Status = status,
                SportKey = sportKey
            };

            var history = await _apiBettingService.GetUserApiBetHistoryAsync(userId, page, 20, filter);
            return View(history);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CancelApiBet(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _apiBettingService.CancelApiBetAsync(id, userId);

            if (success)
            {
                AddSuccessMessage("Apuesta cancelada exitosamente.");
            }
            else
            {
                AddErrorMessage("No se pudo cancelar la apuesta. Verifica que el evento no haya iniciado.");
            }

            return RedirectToAction("ApiBetDetails", new { id });
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