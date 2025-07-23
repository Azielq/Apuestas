using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Controllers.Admin
{
    public class ReportsController : AdminBaseController
    {
        private readonly IReportService _reportService;
        private readonly IUserService _userService;

        public ReportsController(
            IReportService reportService,
            IUserService userService,
            ILogger<ReportsController> logger) : base(logger)
        {
            _reportService = reportService;
            _userService = userService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(string reportType, DateTime startDate, DateTime endDate, int? sportId = null)
        {
            ReportViewModel? report = reportType switch
            {
                "UserActivity" => await _reportService.GenerateUserActivityReportAsync(startDate, endDate),
                "Revenue" => await _reportService.GenerateRevenueReportAsync(startDate, endDate),
                "BettingStats" => await _reportService.GenerateBettingStatsReportAsync(startDate, endDate, sportId),
                "PaymentSummary" => await _reportService.GeneratePaymentSummaryReportAsync(startDate, endDate),
                _ => null
            };

            if (report == null)
            {
                AddErrorMessage("Tipo de reporte inválido");
                return RedirectToAction(nameof(Index));
            }

            // Registrar en log
            var userId = _userService.GetCurrentUserId();
            await _reportService.ScheduleReportAsync(reportType, "MANUAL", userId, "");

            return View("Report", report);
        }

        [HttpPost]
        public async Task<IActionResult> ExportCsv(ReportViewModel model)
        {
            var csvData = await _reportService.ExportReportToCsvAsync(model);
            return File(csvData, "text/csv", $"report_{model.ReportType}_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpPost]
        public async Task<IActionResult> ExportPdf(ReportViewModel model)
        {
            var pdfData = await _reportService.ExportReportToPdfAsync(model);
            return File(pdfData, "application/pdf", $"report_{model.ReportType}_{DateTime.Now:yyyyMMdd}.pdf");
        }

        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = _userService.GetCurrentUserId();
            var history = await _reportService.GetReportHistoryAsync(userId);
            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Schedule(string reportType, string frequency, string email)
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _reportService.ScheduleReportAsync(reportType, frequency, userId, email);

            if (success)
            {
                AddSuccessMessage($"Reporte {reportType} programado exitosamente con frecuencia {frequency}");
            }
            else
            {
                AddErrorMessage("Error al programar el reporte");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}