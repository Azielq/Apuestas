using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Attributes;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;

namespace Proyecto_Apuestas.Controllers.Admin
{
    public class DashboardController : AdminBaseController
    {
        private readonly apuestasDbContext _context;
        private readonly IReportService _reportService;
        private readonly IUserService _userService;
        private readonly IBettingService _bettingService;

        public DashboardController(
            apuestasDbContext context,
            IReportService reportService,
            IUserService userService,
            IBettingService bettingService,
            ILogger<DashboardController> logger) : base(logger)
        {
            _context = context;
            _reportService = reportService;
            _userService = userService;
            _bettingService = bettingService;
        }

        [AuthorizeRole("Admin")]
        public async Task<IActionResult> Index()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            ViewBag.TotalUsers = await _context.UserAccounts.CountAsync();
            ViewBag.ActiveUsers = await _context.UserAccounts.CountAsync(u => u.IsActive == true);
            ViewBag.NewUsersThisMonth = await _context.UserAccounts.CountAsync(u => u.CreatedAt >= startOfMonth);
            ViewBag.LockedUsers = await _context.UserAccounts.CountAsync(u => u.LockedUntil > now);

            ViewBag.TotalBets = await _context.Bets.CountAsync();
            ViewBag.ActiveBets = await _context.Bets.CountAsync(b => b.BetStatus == "A");
            ViewBag.WonBets = await _context.Bets.CountAsync(b => b.BetStatus == "W");
            ViewBag.LostBets = await _context.Bets.CountAsync(b => b.BetStatus == "L");
            ViewBag.PendingBets = await _context.Bets.CountAsync(b => b.BetStatus == "P");

            ViewBag.TotalStake = await _context.Bets.SumAsync(b => (decimal?)b.Stake) ?? 0;
            ViewBag.TotalPayout = await _context.Bets.Where(b => b.BetStatus == "W").SumAsync(b => (decimal?)b.Payout) ?? 0;
            ViewBag.MonthlyRevenue = await _context.Bets.Where(b => b.Date >= startOfMonth).SumAsync(b => (decimal?)b.Stake) ?? 0;

            ViewBag.TotalCompetitions = await _context.Competitions.CountAsync();
            ViewBag.ActiveCompetitions = await _context.Competitions.CountAsync(c => c.IsActive == true);
            ViewBag.CurrentCompetitions = await _context.Competitions.CountAsync(c =>
                c.StartDate <= DateOnly.FromDateTime(now) &&
                c.EndDate >= DateOnly.FromDateTime(now));

            await LoadUsersRoleChartData();
            await LoadCompetitionsSportChartData();

            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Now.AddDays(-6 + i).Date)
                .ToList();

            var betsPerDay = await _context.Bets
                .Where(b => b.Date >= last7Days.First())
                .GroupBy(b => b.Date.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var betsChartLabels = last7Days.Select(d => d.ToString("dd/MM")).ToArray();
            var betsChartData = last7Days.Select(date =>
                betsPerDay.FirstOrDefault(b => b.Date == date)?.Count ?? 0).ToArray();

            ViewBag.BetsChartLabels = betsChartLabels;
            ViewBag.BetsChartData = betsChartData;

            var last6Months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-5 + i))
                .Select(date => new DateTime(date.Year, date.Month, 1))
                .ToList();

            var monthlyRevenue = await _context.Bets
                .Where(b => b.Date >= last6Months.First())
                .GroupBy(b => new { b.Date.Year, b.Date.Month })
                .Select(g => new
                {
                    Revenue = g.Sum(b => b.Stake),
                    Payout = g.Where(b => b.BetStatus == "W").Sum(b => b.Payout)
                })
                .ToListAsync();


            await LoadRecentActivity();

            var totalStake = ViewBag.TotalStake as decimal? ?? 0;
            var totalPayout = ViewBag.TotalPayout as decimal? ?? 0;
            ViewBag.ProfitMargin = totalStake > 0 ? Math.Round(((totalStake - totalPayout) / totalStake) * 100, 2) : 0;

            return View();
        }

        private async Task LoadUsersRoleChartData()
        {
            try
            {
                var usersWithRoleData = await _context.UserAccounts
                    .Include(u => u.Role)
                    .Where(u => u.Role != null)
                    .GroupBy(u => u.Role.RoleName)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToListAsync();

                var usersByRole = new List<dynamic>(usersWithRoleData.Select(x => new { Role = x.Role, Count = x.Count }));

                var usersWithoutRole = await _context.UserAccounts.CountAsync(u => u.Role == null);

                if (usersWithoutRole > 0)
                {
                    usersByRole.Add(new { Role = "Sin Rol", Count = usersWithoutRole });
                }

                if (usersByRole.Count == 0)
                {
                    var totalUsers = await _context.UserAccounts.CountAsync();
                    if (totalUsers > 0)
                    {
                        usersByRole.Add(new { Role = "Usuarios", Count = totalUsers });
                    }
                    else
                    {
                        usersByRole.Add(new { Role = "Sin datos", Count = 1 });
                    }
                }

                ViewBag.UsersRoleLabels = usersByRole.Select(x => x.Role).ToArray();
                ViewBag.UsersRoleData = usersByRole.Select(x => x.Count).ToArray();
            }
            catch (Exception)
            {
                ViewBag.UsersRoleLabels = new string[] { "Error" };
                ViewBag.UsersRoleData = new int[] { 1 };
            }
        }

        private async Task LoadCompetitionsSportChartData()
        {
            try
            {
                var competitionsWithSportData = await _context.Competitions
                    .Include(c => c.Sport)
                    .Where(c => c.Sport != null)
                    .GroupBy(c => c.Sport.Name)
                    .Select(g => new { Sport = g.Key, Count = g.Count() })
                    .ToListAsync();

                var competitionsBySport = new List<dynamic>(competitionsWithSportData.Select(x => new { Sport = x.Sport, Count = x.Count }));

                var competitionsWithoutSport = await _context.Competitions.CountAsync(c => c.Sport == null);

                if (competitionsWithoutSport > 0)
                {
                    competitionsBySport.Add(new { Sport = "Sin Deporte", Count = competitionsWithoutSport });
                }

                if (competitionsBySport.Count == 0)
                {
                    var totalCompetitions = await _context.Competitions.CountAsync();
                    if (totalCompetitions > 0)
                    {
                        competitionsBySport.Add(new { Sport = "Competiciones", Count = totalCompetitions });
                    }
                    else
                    {
                        competitionsBySport.Add(new { Sport = "Sin datos", Count = 1 });
                    }
                }

                ViewBag.CompetitionsSportLabels = competitionsBySport.Select(x => x.Sport).ToArray();
                ViewBag.CompetitionsSportData = competitionsBySport.Select(x => x.Count).ToArray();
            }
            catch (Exception)
            {
                ViewBag.CompetitionsSportLabels = new string[] { "Error" };
                ViewBag.CompetitionsSportData = new int[] { 1 };
            }
        }

        private async Task LoadRecentActivity()
        {
            try
            {
                var recentActivity = new List<dynamic>();

                var recentBets = await _context.Bets
                    .Include(b => b.Users)
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .Select(b => new {
                        Type = "Apuesta",
                        Description = $"Nueva apuesta de ${b.Stake:N2}",
                        User = b.Users.Any() ? b.Users.First().UserName : "Usuario desconocido",
                        Date = b.CreatedAt,
                        Amount = b.Stake,
                        Icon = "fas fa-dice"
                    })
                    .ToListAsync();

                var recentUsers = await _context.UserAccounts
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5)
                    .Select(u => new {
                        Type = "Usuario",
                        Description = "Nuevo usuario registrado",
                        User = u.UserName,
                        Date = u.CreatedAt,
                        Amount = (decimal?)null,
                        Icon = "fas fa-user-plus"
                    })
                    .ToListAsync();

                recentActivity.AddRange(recentBets.Cast<dynamic>());
                recentActivity.AddRange(recentUsers.Cast<dynamic>());

                ViewBag.RecentActivity = recentActivity
                    .OrderByDescending(x => ((dynamic)x).Date)
                    .Take(8)
                    .ToList();
            }
            catch (Exception)
            {
                ViewBag.RecentActivity = new List<dynamic>();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                var stats = new
                {
                    totalUsers = await _context.UserAccounts.CountAsync(),
                    activeUsers = await _context.UserAccounts.CountAsync(u => u.IsActive == true),
                    totalBets = await _context.Bets.CountAsync(),
                    activeBets = await _context.Bets.CountAsync(b => b.BetStatus == "A"),
                    pendingBets = await _context.Bets.CountAsync(b => b.BetStatus == "P"),
                    totalCompetitions = await _context.Competitions.CountAsync(),
                    activeCompetitions = await _context.Competitions.CountAsync(c => c.IsActive == true),
                    totalStake = await _context.Bets.SumAsync(b => (decimal?)b.Stake) ?? 0,
                    totalPayout = await _context.Bets.Where(b => b.BetStatus == "W").SumAsync(b => (decimal?)b.Payout) ?? 0,
                    monthlyRevenue = await _context.Bets.Where(b => b.Date >= startOfMonth).SumAsync(b => (decimal?)b.Stake) ?? 0,
                    timestamp = now
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener estadísticas", message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRealTimeStats()
        {
            try
            {
                if (_reportService != null)
                {
                    var serviceStats = await _reportService.GetRealTimeStatsAsync();
                    return Json(serviceStats);
                }
            }
            catch (Exception)
            {
            }

            try
            {
                var now = DateTime.Now;
                var startOfMonth = new DateTime(now.Year, now.Month, 1);

                var stats = new
                {
                    totalUsers = await _context.UserAccounts.CountAsync(),
                    activeUsers = await _context.UserAccounts.CountAsync(u => u.IsActive == true),
                    totalBets = await _context.Bets.CountAsync(),
                    activeBets = await _context.Bets.CountAsync(b => b.BetStatus == "A"),
                    pendingBets = await _context.Bets.CountAsync(b => b.BetStatus == "P"),
                    totalCompetitions = await _context.Competitions.CountAsync(),
                    activeCompetitions = await _context.Competitions.CountAsync(c => c.IsActive == true),
                    totalStake = await _context.Bets.SumAsync(b => (decimal?)b.Stake) ?? 0,
                    totalPayout = await _context.Bets.Where(b => b.BetStatus == "W").SumAsync(b => (decimal?)b.Payout) ?? 0,
                    monthlyRevenue = await _context.Bets.Where(b => b.Date >= startOfMonth).SumAsync(b => (decimal?)b.Stake) ?? 0,
                    timestamp = now
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                return Json(new { error = "Error al obtener estadísticas", message = ex.Message });
            }
        }
    }
}