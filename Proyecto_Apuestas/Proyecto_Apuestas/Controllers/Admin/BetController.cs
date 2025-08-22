using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Controllers
{
    public class BetController : Controller
    {
        private readonly apuestasDbContext _context;

        public BetController(apuestasDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> BetsView(
            string? sportKey = null,
            string? region = null,
            string? market = null,
            string? bookmaker = null,
            string? q = null,
            bool liveOnly = false,
            int page = 1,
            int pageSize = 50)
        {
            var now = DateTime.UtcNow;                   // usa DateTime.Now si EventDate es local
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 10, 200);

            IQueryable<ApiBet> query = _context.ApiBets
                .AsNoTracking()
                .Where(b => b.BetStatus == "P");         // pendientes

            if (liveOnly)
                query = query.Where(b => b.EventDate <= now);

            if (!string.IsNullOrWhiteSpace(sportKey)) query = query.Where(b => b.SportKey == sportKey);
            if (!string.IsNullOrWhiteSpace(region)) query = query.Where(b => b.Region == region);
            if (!string.IsNullOrWhiteSpace(market)) query = query.Where(b => b.Market == market);
            if (!string.IsNullOrWhiteSpace(bookmaker)) query = query.Where(b => b.Bookmaker == bookmaker);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var t = q.Trim();
                query = query.Where(b =>
                    b.EventName.Contains(t) ||
                    (b.TeamName != null && b.TeamName.Contains(t)) ||
                    (b.ApiEventId != null && b.ApiEventId.Contains(t)) ||
                    (b.HomeTeam != null && b.HomeTeam.Contains(t)) ||
                    (b.AwayTeam != null && b.AwayTeam.Contains(t)));
            }

            var total = await query.CountAsync();

            // 1) Traer la página base
            var items = await query
                .OrderBy(b => b.EventDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(b => new AdminApiBetRowVM
                {
                    ApiBetId = b.ApiBetId,
                    ApiEventId = b.ApiEventId,
                    SportKey = b.SportKey,
                    Region = b.Region,
                    Market = b.Market,
                    Bookmaker = b.Bookmaker,
                    EventName = b.EventName,
                    HomeTeam = b.HomeTeam,
                    AwayTeam = b.AwayTeam,
                    TeamName = b.TeamName,
                    EventDate = b.EventDate,
                    Odds = b.Odds,
                    Stake = b.Stake,
                    Payout = b.Payout,
                    BetStatus = b.BetStatus,
                    PaymentTransactionId = b.PaymentTransactionId
                })
                .ToListAsync();

            // 2) Completar con usuarios (conteo + primeros 3 nombres)
            if (items.Count > 0)
            {
                var ids = items.Select(x => x.ApiBetId).ToList();

                var usersByBet = await _context.ApiBetUserAccounts
                    .Where(x => ids.Contains(x.ApiBetId))
                    .Include(x => x.User)
                    .GroupBy(x => x.ApiBetId)
                    .Select(g => new
                    {
                        ApiBetId = g.Key,
                        Count = g.Count(),
                        Names = g.OrderBy(x => x.User.UserName)
                                 .Select(x => x.User.UserName)
                                 .Take(3)
                                 .ToList()
                    })
                    .ToListAsync();

                var dict = usersByBet.ToDictionary(k => k.ApiBetId, v => v);
                foreach (var row in items)
                {
                    if (dict.TryGetValue(row.ApiBetId, out var info))
                    {
                        row.UsersCount = info.Count;
                        row.UserNames = info.Names;
                    }
                }
            }

            var vm = new AdminApiBetsViewVM
            {
                Items = items,
                Filters = new AdminApiBetsFilterVM
                {
                    SportKey = sportKey,
                    Region = region,
                    Market = market,
                    Bookmaker = bookmaker,
                    Q = q,
                    LiveOnly = liveOnly,
                    Page = page,
                    PageSize = pageSize
                },
                TotalItems = total
            };

            return View(vm); // Renderiza Views/Bet/BetsView.cshtml
        }
    }
}
