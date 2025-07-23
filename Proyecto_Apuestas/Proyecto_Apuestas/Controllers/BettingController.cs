using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Extensions;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Controllers
{
    [Authorize]
    public class BettingController : BaseController
    {
        private readonly IBettingService _bettingService;
        private readonly IEventService _eventService;
        private readonly IUserService _userService;
        private readonly IOddsService _oddsService;

        public BettingController(
            IBettingService bettingService,
            IEventService eventService,
            IUserService userService,
            IOddsService oddsService,
            ILogger<BettingController> logger) : base(logger)
        {
            _bettingService = bettingService;
            _eventService = eventService;
            _userService = userService;
            _oddsService = oddsService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, string? status = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var userId = _userService.GetCurrentUserId();
            var filter = new BetHistoryFilter
            {
                Status = status,
                StartDate = startDate,
                EndDate = endDate
            };

            var history = await _bettingService.GetUserBetHistoryAsync(userId, page, 20, filter);
            return View(history);
        }

        [HttpGet]
        public async Task<IActionResult> Create(int eventId)
        {
            var eventDetails = await _eventService.GetEventDetailsAsync(eventId);
            if (eventDetails == null)
            {
                return NotFound();
            }

            if (!eventDetails.CanBet)
            {
                AddErrorMessage("No se puede apostar en este evento");
                return RedirectToAction("Details", "Event", new { id = eventId });
            }

            var user = await _userService.GetCurrentUserAsync();
            var model = new CreateBetViewModel
            {
                EventId = eventId,
                EventName = $"{string.Join(" vs ", eventDetails.Teams.Select(t => t.TeamName))}",
                EventDate = eventDetails.Date,
                UserBalance = user?.CreditBalance ?? 0
            };

            ViewBag.Teams = eventDetails.Teams;
            ViewBag.CurrentOdds = eventDetails.CurrentOdds;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateBetViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Esto nos recarga datos necesarios
                await LoadBetViewData(model);
                return View(model);
            }

            var result = await _bettingService.PlaceBetAsync(model);

            if (result.Success)
            {
                AddSuccessMessage("¡Apuesta realizada con éxito!");
                return RedirectToAction(nameof(Details), new { id = result.BetId });
            }

            AddModelErrors(result.ErrorMessage ?? "Error al procesar la apuesta");
            await LoadBetViewData(model);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var bet = await _bettingService.GetBetDetailsAsync(id, userId);

            if (bet == null)
            {
                return NotFound();
            }

            return View(bet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _bettingService.CancelBetAsync(id, userId);

            if (success)
            {
                AddSuccessMessage("Apuesta cancelada exitosamente");
            }
            else
            {
                AddErrorMessage("No se pudo cancelar la apuesta");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> BetSlip()
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _userService.GetCurrentUserAsync();

            // Esto nos obtiene apuestas del carrito desde sesión o cookie
            var betSlip = GetBetSlipFromSession();
            betSlip.UserBalance = user?.CreditBalance ?? 0;
            betSlip.CanPlaceBet = betSlip.TotalStake <= betSlip.UserBalance && betSlip.Items.Any();

            return View(betSlip);
        }

        [HttpPost]
        public async Task<IActionResult> AddToBetSlip([FromBody] BetSlipItemViewModel item)
        {
            try
            {
                // Valida que el evento existe y se puede apostar
                if (!await _eventService.IsEventBettableAsync(item.EventId))
                {
                    return JsonError("No se puede apostar en este evento");
                }

                var betSlip = GetBetSlipFromSession();

                // Verifica si ya existe
                var existing = betSlip.Items.FirstOrDefault(i => i.EventId == item.EventId && i.TeamId == item.TeamId);
                if (existing != null)
                {
                    existing.Stake = item.Stake;
                    existing.PotentialPayout = await _bettingService.CalculatePotentialPayoutAsync(item.Stake, item.Odds);
                }
                else
                {
                    item.PotentialPayout = await _bettingService.CalculatePotentialPayoutAsync(item.Stake, item.Odds);
                    betSlip.Items.Add(item);
                }

                SaveBetSlipToSession(betSlip);
                return JsonSuccess(new { itemCount = betSlip.Items.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to bet slip");
                return JsonError("Error al agregar la apuesta");
            }
        }

        [HttpPost]
        public IActionResult RemoveFromBetSlip([FromBody] int index)
        {
            var betSlip = GetBetSlipFromSession();

            if (index >= 0 && index < betSlip.Items.Count)
            {
                betSlip.Items.RemoveAt(index);
                SaveBetSlipToSession(betSlip);
                return JsonSuccess(new { itemCount = betSlip.Items.Count });
            }

            return JsonError("Índice inválido");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceBetSlip()
        {
            var betSlip = GetBetSlipFromSession();

            if (!betSlip.Items.Any())
            {
                AddErrorMessage("El boleto de apuestas está vacío");
                return RedirectToAction(nameof(BetSlip));
            }

            var result = await _bettingService.PlaceMultipleBetsAsync(betSlip);

            if (result.Success)
            {
                ClearBetSlip();
                AddSuccessMessage("¡Boleto de apuestas realizado con éxito!");
                return RedirectToAction(nameof(Index));
            }

            AddErrorMessage(result.ErrorMessage ?? "Error al procesar el boleto de apuestas");
            return RedirectToAction(nameof(BetSlip));
        }

        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var userId = _userService.GetCurrentUserId();
            var stats = await _bettingService.GetBettingStatisticsAsync(userId);

            return View(stats);
        }

        [HttpGet]
        public async Task<IActionResult> Active()
        {
            var userId = _userService.GetCurrentUserId();
            var activeBets = await _bettingService.GetActiveBetsByUserAsync(userId);

            return View(activeBets);
        }

        // Métodos privados
        private async Task LoadBetViewData(CreateBetViewModel model)
        {
            var eventDetails = await _eventService.GetEventDetailsAsync(model.EventId);
            if (eventDetails != null)
            {
                ViewBag.Teams = eventDetails.Teams;
                ViewBag.CurrentOdds = eventDetails.CurrentOdds;
                model.EventName = $"{string.Join(" vs ", eventDetails.Teams.Select(t => t.TeamName))}";
                model.EventDate = eventDetails.Date;
            }

            var user = await _userService.GetCurrentUserAsync();
            model.UserBalance = user?.CreditBalance ?? 0;
        }

        private BetSlipViewModel GetBetSlipFromSession()
        {
            // En producción, esto nos vendría de Redis o una base de datos de sesión
            var betSlip = HttpContext.Session.GetObject<BetSlipViewModel>("BetSlip");
            if (betSlip == null)
            {
                betSlip = new BetSlipViewModel();
            }

            // Recalcula totales
            betSlip.TotalStake = betSlip.Items.Sum(i => i.Stake);
            betSlip.TotalPotentialPayout = betSlip.Items.Sum(i => i.PotentialPayout);

            return betSlip;
        }

        private void SaveBetSlipToSession(BetSlipViewModel betSlip)
        {
            HttpContext.Session.SetObject("BetSlip", betSlip);
        }

        private void ClearBetSlip()
        {
            HttpContext.Session.Remove("BetSlip");
        }
    }
}