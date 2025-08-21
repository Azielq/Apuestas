using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Proyecto_Apuestas.Controllers
{
    public class BetController : Controller
    {
        private readonly apuestasDbContext _context;

        public BetController(apuestasDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string searchTerm = "", string betStatus = "",
            int eventId = 0, decimal? minStake = null, decimal? maxStake = null,
            DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 10)
        {
            ViewBag.SearchTerm = searchTerm;
            ViewBag.BetStatus = betStatus;
            ViewBag.EventId = eventId;
            ViewBag.MinStake = minStake;
            ViewBag.MaxStake = maxStake;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var query = _context.Bets
                .Include(b => b.Event)
                .Include(b => b.PaymentTransaction)
                .Include(b => b.Users)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(b =>
                    b.Event.ExternalEventId.Contains(searchTerm) ||
                    b.Users.Any(u => u.UserName.Contains(searchTerm) || u.Email.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(betStatus))
            {
                query = query.Where(b => b.BetStatus == betStatus);
            }

            if (eventId > 0)
            {
                query = query.Where(b => b.EventId == eventId);
            }

            if (minStake.HasValue)
            {
                query = query.Where(b => b.Stake >= minStake.Value);
            }

            if (maxStake.HasValue)
            {
                query = query.Where(b => b.Stake <= maxStake.Value);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(b => b.Date >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                query = query.Where(b => b.Date <= toDate.Value.AddDays(1));
            }

            // Estadísticas
            var totalBets = await query.CountAsync();
            var activeBets = await query.CountAsync(b => b.BetStatus == "A");
            var wonBets = await query.CountAsync(b => b.BetStatus == "W");
            var lostBets = await query.CountAsync(b => b.BetStatus == "L");
            var pendingBets = await query.CountAsync(b => b.BetStatus == "P");
            var totalStake = await query.SumAsync(b => (decimal?)b.Stake) ?? 0;
            var totalPayout = await query.Where(b => b.BetStatus == "W").SumAsync(b => (decimal?)b.Payout) ?? 0;

            ViewBag.TotalBets = totalBets;
            ViewBag.ActiveBets = activeBets;
            ViewBag.WonBets = wonBets;
            ViewBag.LostBets = lostBets;
            ViewBag.PendingBets = pendingBets;
            ViewBag.TotalStake = totalStake;
            ViewBag.TotalPayout = totalPayout;
            var totalCount = await query.CountAsync();
            var bets = await query
                .OrderByDescending(b => b.Date)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

            ViewBag.Events = await _context.Events
                .OrderBy(e => e.Date)
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = $"{e.ExternalEventId ?? $"Evento {e.EventId}"} - {e.Date:dd/MM/yyyy}"
                })
                .ToListAsync();

            ViewBag.BetStatuses = new List<SelectListItem>
            {
                new SelectListItem { Value = "A", Text = "Activa" },
                new SelectListItem { Value = "W", Text = "Ganada" },
                new SelectListItem { Value = "L", Text = "Perdida" },
                new SelectListItem { Value = "P", Text = "Pendiente" },
                new SelectListItem { Value = "C", Text = "Cancelada" }
            };

            return View(bets);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bet = await _context.Bets
                .Include(b => b.Event)
                .Include(b => b.PaymentTransaction)
                .Include(b => b.Users)
                .FirstOrDefaultAsync(m => m.BetId == id);

            if (bet == null)
            {
                return NotFound();
            }

            return View(bet);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bet = await _context.Bets
                .Include(b => b.Users)
                .FirstOrDefaultAsync(b => b.BetId == id);

            if (bet == null)
            {
                return NotFound();
            }

            ViewBag.Events = await _context.Events
                .OrderBy(e => e.Date)
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = $"{e.ExternalEventId ?? $"Evento {e.EventId}"} - {e.Date:dd/MM/yyyy}"
                })
                .ToListAsync();

            ViewBag.Users = await _context.UserAccounts
                .OrderBy(u => u.UserName)
                .ToListAsync();

            ViewBag.SelectedUsers = bet.Users.Select(u => u.UserId).ToArray();

            return View(bet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("BetId,EventId,Odds,Stake,BetStatus,Date,PaymentTransactionId,CreatedAt")] Bet bet, int[] selectedUsers)
        {
            if (id != bet.BetId)
            {
                return NotFound();
            }

            try
            {
                if (ModelState.IsValid)
                {
                    var existingBet = await _context.Bets
                        .Include(b => b.Users)
                        .FirstOrDefaultAsync(b => b.BetId == id);

                    if (existingBet == null)
                    {
                        return NotFound();
                    }

                    // Verificar si la apuesta ya está expirada
                    if (existingBet.BetStatus == "W" || existingBet.BetStatus == "L")
                    {
                        // Solo permitir cambios limitados en apuestas resueltas
                        existingBet.BetStatus = bet.BetStatus;
                        existingBet.UpdatedAt = DateTime.Now;
                    }
                    else
                    {
                        // Actualizar todas las propiedades para apuestas no resueltas
                        existingBet.EventId = bet.EventId;
                        existingBet.Odds = bet.Odds;
                        existingBet.Stake = bet.Stake;
                        existingBet.Payout = bet.Stake * bet.Odds;
                        existingBet.BetStatus = bet.BetStatus;
                        existingBet.PaymentTransactionId = bet.PaymentTransactionId;
                        existingBet.UpdatedAt = DateTime.Now;

                        // Actualizar usuarios solo si la apuesta no está resuelta
                        if (selectedUsers != null)
                        {
                            existingBet.Users.Clear();
                            var users = await _context.UserAccounts
                                .Where(u => selectedUsers.Contains(u.UserId))
                                .ToListAsync();

                            foreach (var user in users)
                            {
                                existingBet.Users.Add(user);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Apuesta actualizada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }

                await LoadEditViewData(bet);
                return View(bet);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al actualizar la apuesta: " + ex.Message;
                await LoadEditViewData(bet);
                return View(bet);
            }
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var bet = await _context.Bets
                .Include(b => b.Event)
                .Include(b => b.PaymentTransaction)
                .Include(b => b.Users)
                .FirstOrDefaultAsync(m => m.BetId == id);

            if (bet == null)
            {
                return NotFound();
            }

            return View(bet);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var bet = await _context.Bets
                    .Include(b => b.Users)
                    .FirstOrDefaultAsync(b => b.BetId == id);

                if (bet != null)
                {
                    // Verificar si se puede eliminar
                    if (bet.BetStatus == "W" || bet.BetStatus == "L")
                    {
                        TempData["Error"] = "No se puede eliminar una apuesta que ya ha sido resuelta.";
                        return RedirectToAction(nameof(Index));
                    }

                    _context.Bets.Remove(bet);
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Apuesta eliminada exitosamente.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error al eliminar la apuesta: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadEditViewData(Bet bet)
        {
            ViewBag.Events = await _context.Events
                .OrderBy(e => e.Date)
                .Select(e => new SelectListItem
                {
                    Value = e.EventId.ToString(),
                    Text = $"{e.ExternalEventId ?? $"Evento {e.EventId}"} - {e.Date:dd/MM/yyyy}"
                })
                .ToListAsync();

            ViewBag.Users = await _context.UserAccounts
                .OrderBy(u => u.UserName)
                .ToListAsync();

            if (bet.Users != null)
            {
                ViewBag.SelectedUsers = bet.Users.Select(u => u.UserId).ToArray();
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateBetStatus(int betId, string status)
        {
            try
            {
                var bet = await _context.Bets.FindAsync(betId);
                if (bet == null)
                {
                    return Json(new { success = false, message = "Apuesta no encontrada" });
                }

                bet.BetStatus = status;
                bet.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Estado actualizado correctamente" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error: " + ex.Message });
            }
        }

        private bool BetExists(int id)
        {
            return _context.Bets.Any(e => e.BetId == id);
        }
    }
}