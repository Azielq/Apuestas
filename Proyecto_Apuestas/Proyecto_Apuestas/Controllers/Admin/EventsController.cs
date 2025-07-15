using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Proyecto_Apuestas.Models;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Controllers.Admin
{
    public class EventsController : AdminBaseController
    {
        private readonly apuestasDbContext _context;
        private readonly IEventService _eventService;
        private readonly IBettingService _bettingService;
        private readonly IOddsService _oddsService;

        public EventsController(
            apuestasDbContext context,
            IEventService eventService,
            IBettingService bettingService,
            IOddsService oddsService,
            ILogger<EventsController> logger) : base(logger)
        {
            _context = context;
            _eventService = eventService;
            _bettingService = bettingService;
            _oddsService = oddsService;
        }

        public async Task<IActionResult> Index(EventManagementViewModel model)
        {
            var query = _context.Events
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                        .ThenInclude(t => t.Sport)
                .Include(e => e.Bets)
                .AsQueryable();

            // Filtros
            if (model.SportId.HasValue)
            {
                query = query.Where(e => e.EventHasTeams.Any(et => et.Team.SportId == model.SportId.Value));
            }

            if (!string.IsNullOrEmpty(model.Status))
            {
                query = model.Status switch
                {
                    "upcoming" => query.Where(e => e.Date > DateTime.Now),
                    "live" => query.Where(e => e.Date <= DateTime.Now && e.Date >= DateTime.Now.AddHours(-3)),
                    "finished" => query.Where(e => !string.IsNullOrEmpty(e.Outcome)),
                    "cancelled" => query.Where(e => e.Outcome == "CANCELLED"),
                    _ => query
                };
            }

            var totalEvents = await query.CountAsync();
            var currentPage = model.CurrentPage > 0 ? model.CurrentPage : 1;
            var pageSize = 20;

            var events = await query
                .OrderBy(e => e.Date)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EventAdminViewModel
                {
                    EventId = e.EventId,
                    ExternalEventId = e.ExternalEventId,
                    Date = e.Date,
                    Outcome = e.Outcome,
                    Teams = string.Join(" vs ", e.EventHasTeams.Select(et => et.Team.TeamName)),
                    Sport = e.EventHasTeams.First().Team.Sport.Name,
                    Competition = "Liga Principal", // Deberías obtener esto de la competición real
                    TotalBets = e.Bets.Count,
                    TotalStaked = e.Bets.Sum(b => b.Stake),
                    CanEdit = e.Date > DateTime.Now,
                    CanSettle = e.Date < DateTime.Now && string.IsNullOrEmpty(e.Outcome)
                })
                .ToListAsync();

            model.Events = events;
            model.CurrentPage = currentPage;
            model.TotalPages = (int)Math.Ceiling(totalEvents / (double)pageSize);
            model.Sports = await _context.Sports.ToDictionaryAsync(s => s.SportId, s => s.Name);

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Sports = await _context.Sports
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .ToListAsync();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Event model, List<int> teamIds, Dictionary<int, decimal> initialOdds)
        {
            if (!ModelState.IsValid || teamIds.Count < 2)
            {
                AddErrorMessage("Datos inválidos. Verifica que hayas seleccionado al menos 2 equipos.");
                ViewBag.Sports = await _context.Sports.Where(s => s.IsActive == true).ToListAsync();
                return View(model);
            }

            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;
            model.Outcome = "";

            var success = await _eventService.CreateEventAsync(model, teamIds);

            if (success)
            {
                // Establecer cuotas iniciales
                if (initialOdds.Any())
                {
                    await _oddsService.SetInitialOddsAsync(model.EventId, initialOdds);
                }

                AddSuccessMessage("Evento creado exitosamente");
                return RedirectToAction(nameof(Details), new { id = model.EventId });
            }

            AddErrorMessage("Error al crear el evento");
            ViewBag.Sports = await _context.Sports.Where(s => s.IsActive == true).ToListAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var eventEntity = await _context.Events
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                .FirstOrDefaultAsync(e => e.EventId == id);

            if (eventEntity == null)
            {
                return NotFound();
            }

            if (eventEntity.Date <= DateTime.Now)
            {
                AddErrorMessage("No se pueden editar eventos que ya han comenzado");
                return RedirectToAction(nameof(Details), new { id });
            }

            ViewBag.Teams = eventEntity.EventHasTeams.Select(et => et.Team).ToList();
            return View(eventEntity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Event model)
        {
            if (id != model.EventId)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var success = await _eventService.UpdateEventAsync(id, model);

            if (success)
            {
                AddSuccessMessage("Evento actualizado exitosamente");
                return RedirectToAction(nameof(Details), new { id });
            }

            AddErrorMessage("Error al actualizar el evento");
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var eventDetails = await _eventService.GetEventDetailsAsync(id);
            if (eventDetails == null)
            {
                return NotFound();
            }

            ViewBag.PendingBets = await _bettingService.GetPendingBetsByEventAsync(id);
            ViewBag.OddsAnalysis = await _oddsService.AnalyzeOddsPatternsAsync(id);

            return View(eventDetails);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOdds(int id, Dictionary<int, decimal> newOdds)
        {
            var success = await _oddsService.UpdateOddsAsync(id, newOdds);

            if (success)
            {
                AddSuccessMessage("Cuotas actualizadas exitosamente");
            }
            else
            {
                AddErrorMessage("Error al actualizar las cuotas");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ImportOdds(int id)
        {
            var success = await _oddsService.ImportOddsFromExternalSourceAsync(id);

            if (success)
            {
                AddSuccessMessage("Cuotas importadas exitosamente");
            }
            else
            {
                AddErrorMessage("Error al importar las cuotas");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settle(int id, string outcome, int? winningTeamId)
        {
            var success = await _eventService.UpdateEventOutcomeAsync(id, outcome, winningTeamId);

            if (success)
            {
                AddSuccessMessage("Evento liquidado exitosamente. Las apuestas han sido procesadas.");
            }
            else
            {
                AddErrorMessage("Error al liquidar el evento");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string reason)
        {
            var success = await _eventService.CancelEventAsync(id, reason);

            if (success)
            {
                AddSuccessMessage("Evento cancelado. Todas las apuestas han sido reembolsadas.");
            }
            else
            {
                AddErrorMessage("Error al cancelar el evento");
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpGet]
        public async Task<IActionResult> GetTeamsBySport(int sportId)
        {
            var teams = await _context.Teams
                .Where(t => t.SportId == sportId && t.IsActive == true)
                .OrderBy(t => t.TeamName)
                .Select(t => new { value = t.TeamId, text = t.TeamName })
                .ToListAsync();

            return Json(teams);
        }
    }
}