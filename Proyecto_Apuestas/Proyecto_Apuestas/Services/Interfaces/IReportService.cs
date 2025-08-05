using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IReportService
    {
        Task<ReportViewModel> GenerateUserActivityReportAsync(DateTime startDate, DateTime endDate);
        Task<ReportViewModel> GenerateRevenueReportAsync(DateTime startDate, DateTime endDate);
        Task<ReportViewModel> GenerateBettingStatsReportAsync(DateTime startDate, DateTime endDate, int? sportId = null);
        Task<ReportViewModel> GeneratePaymentSummaryReportAsync(DateTime startDate, DateTime endDate);
        Task<DashboardViewModel> GetAdminDashboardDataAsync();
        Task<byte[]> ExportReportToCsvAsync(ReportViewModel report);
        Task<byte[]> ExportReportToPdfAsync(ReportViewModel report);
        Task<bool> ScheduleReportAsync(string reportType, string frequency, int userId, string email);
        Task<List<ReportLog>> GetReportHistoryAsync(int userId);
        Task<Dictionary<string, object>> GetRealTimeStatsAsync();
    }
}
