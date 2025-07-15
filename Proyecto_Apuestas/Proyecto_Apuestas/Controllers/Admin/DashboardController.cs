using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services.Interfaces;

namespace Proyecto_Apuestas.Controllers.Admin
{
    public class DashboardController : AdminBaseController
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;
        private readonly IBettingService _bettingService;

        public DashboardController(
            IReportService reportService,
            IUserService userService,
            IBettingService bettingService,
            ILogger<DashboardController> logger) : base(logger)
        {
            _reportService = reportService;
            _userService = userService;
            _bettingService = bettingService;
        }

        public async Task<IActionResult> Index()
        {
            var dashboard = await _reportService.GetAdminDashboardDataAsync();
            return View(dashboard);
        }

        [HttpGet]
        public async Task<IActionResult> GetRealTimeStats()
        {
            var stats = await _reportService.GetRealTimeStatsAsync();
            return Json(stats);
        }
    }
}
