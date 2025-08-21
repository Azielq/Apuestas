using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Proyecto_Apuestas.Controllers
{
    public class CompetitionController : Controller
    {
        private readonly apuestasDbContext _context;

        public CompetitionController(apuestasDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm = "", int sportId = 0,
            bool? isActive = null, DateTime? fromDate = null, DateTime? toDate = null,
            int page = 1, int pageSize = 10)
        {
            ViewBag.SearchTerm = searchTerm;
            ViewBag.SportId = sportId;
            ViewBag.IsActive = isActive;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var query = _context.Competitions
                .Include(c => c.Sport)
                .Include(c => c.Images)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(c => c.Name.Contains(searchTerm) || c.Sport.Name.Contains(searchTerm));
            }

            if (sportId > 0)
            {
                query = query.Where(c => c.SportId == sportId);
            }

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            if (fromDate.HasValue)
            {
                var fromDateOnly = DateOnly.FromDateTime(fromDate.Value);
                query = query.Where(c => c.StartDate >= fromDateOnly);
            }

            if (toDate.HasValue)
            {
                var toDateOnly = DateOnly.FromDateTime(toDate.Value);
                query = query.Where(c => c.EndDate <= toDateOnly);
            }

            // Estadísticas
            var totalCompetitions = await query.CountAsync();
            var activeCompetitions = await query.CountAsync(c => c.IsActive == true);
            var inactiveCompetitions = await query.CountAsync(c => c.IsActive == false);
            var currentCompetitions = await query.CountAsync(c =>
                c.StartDate <= DateOnly.FromDateTime(DateTime.Now) &&
                c.EndDate >= DateOnly.FromDateTime(DateTime.Now));
            var upcomingCompetitions = await query.CountAsync(c =>
                c.StartDate > DateOnly.FromDateTime(DateTime.Now));
            var finishedCompetitions = await query.CountAsync(c =>
                c.EndDate < DateOnly.FromDateTime(DateTime.Now));

            ViewBag.TotalCompetitions = totalCompetitions;
            ViewBag.ActiveCompetitions = activeCompetitions;
            ViewBag.InactiveCompetitions = inactiveCompetitions;
            ViewBag.CurrentCompetitions = currentCompetitions;
            ViewBag.UpcomingCompetitions = upcomingCompetitions;
            ViewBag.FinishedCompetitions = finishedCompetitions;

            // Paginación
            var totalCount = await query.CountAsync();
            var competitions = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

            // Datos para filtros
            ViewBag.Sports = await _context.Sports
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.SportId.ToString(),
                    Text = s.Name
                })
                .ToListAsync();

            ViewBag.StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "true", Text = "Activa" },
                new SelectListItem { Value = "false", Text = "Inactiva" }
            };

            return View(competitions);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competition = await _context.Competitions
                .Include(c => c.Sport)
                .Include(c => c.Images)
                .FirstOrDefaultAsync(m => m.CompetitionId == id);

            if (competition == null)
            {
                return NotFound();
            }

            return View(competition);
        }

        public IActionResult Create()
        {
            LoadCreateEditViewData();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,SportId,StartDate,EndDate,IsActive")] Competition competition)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    competition.CreatedAt = DateTime.Now;
                    competition.UpdatedAt = DateTime.Now;

                    _context.Add(competition);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Competición creada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                LoadCreateEditViewData();
                return View(competition);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al crear la competición: " + ex.Message;
                LoadCreateEditViewData();
                return View(competition);
            }
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competition = await _context.Competitions.FindAsync(id);
            if (competition == null)
            {
                return NotFound();
            }

            LoadCreateEditViewData();
            return View(competition);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CompetitionId,Name,SportId,StartDate,EndDate,IsActive,CreatedAt")] Competition competition)
        {
            if (id != competition.CompetitionId)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    competition.UpdatedAt = DateTime.Now;
                    _context.Update(competition);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "Competición actualizada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                LoadCreateEditViewData();
                return View(competition);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CompetitionExists(competition.CompetitionId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar la competición: " + ex.Message;
                LoadCreateEditViewData();
                return View(competition);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var competition = await _context.Competitions
                .Include(c => c.Sport)
                .FirstOrDefaultAsync(m => m.CompetitionId == id);

            if (competition == null)
            {
                return NotFound();
            }

            return View(competition);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var competition = await _context.Competitions.FindAsync(id);
                if (competition != null)
                {
                    _context.Competitions.Remove(competition);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Competición eliminada exitosamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar la competición: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int competitionId)
        {
            try
            {
                var competition = await _context.Competitions.FindAsync(competitionId);
                if (competition == null)
                {
                    return Json(new { success = false, message = "Competición no encontrada" });
                }

                competition.IsActive = !competition.IsActive;
                competition.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Estado cambiado a {(competition.IsActive == true ? "Activa" : "Inactiva")}",
                    isActive = competition.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private void LoadCreateEditViewData()
        {
            ViewBag.Sports = _context.Sports
                .Where(s => s.IsActive == true)
                .OrderBy(s => s.Name)
                .Select(s => new SelectListItem
                {
                    Value = s.SportId.ToString(),
                    Text = s.Name
                });
        }

        private bool CompetitionExists(int id)
        {
            return _context.Competitions.Any(e => e.CompetitionId == id);
        }
    }
}