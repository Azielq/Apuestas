using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using System.Formats.Asn1;
using System.Globalization;
using System.Text;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class ReportService : IReportService
    {
        private readonly apuestasDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            apuestasDbContext context,
            ILogger<ReportService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ReportViewModel> GenerateUserActivityReportAsync(DateTime startDate, DateTime endDate)
        {
            var users = await _context.UserAccounts
                .Include(u => u.Bets)
                .Include(u => u.PaymentTransactions)
                .Include(u => u.LoginAttempts)
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .ToListAsync();

            var summary = new Dictionary<string, decimal>
            {
                ["TotalUsers"] = users.Count,
                ["ActiveUsers"] = users.Count(u => u.LastBet >= startDate),
                ["NewUsers"] = users.Count(u => u.CreatedAt >= startDate),
                ["TotalBets"] = users.Sum(u => u.Bets.Count(b => b.CreatedAt >= startDate)),
                ["AverageBalance"] = users.Average(u => u.CreditBalance)
            };

            var detailedData = users.Select(u => new Dictionary<string, object>
            {
                ["UserId"] = u.UserId,
                ["UserName"] = u.UserName,
                ["Email"] = u.Email,
                ["RegisterDate"] = u.CreatedAt,
                ["LastActivity"] = u.LastBet ?? u.CreatedAt,
                ["TotalBets"] = u.Bets.Count,
                ["TotalDeposits"] = u.PaymentTransactions.Where(t => t.TransactionType == "DEPOSIT").Sum(t => t.Amount),
                ["CurrentBalance"] = u.CreditBalance,
                ["LoginCount"] = u.LoginAttempts.Count(la => la.AttemptTime >= startDate && la.IsSuccessful)
            }).ToList();

            return new ReportViewModel
            {
                ReportType = "UserActivity",
                StartDate = startDate,
                EndDate = endDate,
                Summary = summary,
                DetailedData = detailedData
            };
        }

        public async Task<ReportViewModel> GenerateRevenueReportAsync(DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Where(t => t.CreatedAt >= startDate &&
                           t.CreatedAt <= endDate &&
                           t.Status == "COMPLETED")
                .ToListAsync();

            var bets = await _context.Bets
                .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate)
                .ToListAsync();

            var summary = new Dictionary<string, decimal>
            {
                ["TotalDeposits"] = transactions.Where(t => t.TransactionType == "DEPOSIT").Sum(t => t.Amount),
                ["TotalWithdrawals"] = transactions.Where(t => t.TransactionType == "WITHDRAWAL").Sum(t => t.Amount),
                ["TotalBetsPlaced"] = bets.Sum(b => b.Stake),
                ["TotalPayouts"] = bets.Where(b => b.BetStatus == "W").Sum(b => b.Payout),
                ["GrossRevenue"] = bets.Where(b => b.BetStatus == "L").Sum(b => b.Stake) -
                                  bets.Where(b => b.BetStatus == "W").Sum(b => b.Payout - b.Stake),
                ["NetRevenue"] = 0 // Calcula después de costos operativos
            };

            summary["NetRevenue"] = summary["GrossRevenue"] * 0.7m; // Asumiendo 30% de costos operativos

            // Datos diarios
            var dailyData = new List<Dictionary<string, object>>();
            for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
            {
                var dayEnd = date.AddDays(1);
                dailyData.Add(new Dictionary<string, object>
                {
                    ["Date"] = date,
                    ["Deposits"] = transactions.Where(t => t.TransactionType == "DEPOSIT" &&
                                                          t.CreatedAt >= date &&
                                                          t.CreatedAt < dayEnd).Sum(t => t.Amount),
                    ["Withdrawals"] = transactions.Where(t => t.TransactionType == "WITHDRAWAL" &&
                                                            t.CreatedAt >= date &&
                                                            t.CreatedAt < dayEnd).Sum(t => t.Amount),
                    ["BetsPlaced"] = bets.Where(b => b.CreatedAt >= date && b.CreatedAt < dayEnd).Sum(b => b.Stake),
                    ["Payouts"] = bets.Where(b => b.BetStatus == "W" &&
                                                 b.UpdatedAt >= date &&
                                                 b.UpdatedAt < dayEnd).Sum(b => b.Payout)
                });
            }

            return new ReportViewModel
            {
                ReportType = "Revenue",
                StartDate = startDate,
                EndDate = endDate,
                Summary = summary,
                DetailedData = dailyData
            };
        }

        public async Task<ReportViewModel> GenerateBettingStatsReportAsync(DateTime startDate, DateTime endDate, int? sportId = null)
        {
            var query = _context.Bets
                .Include(b => b.Event)
                    .ThenInclude(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate);

            if (sportId.HasValue)
            {
                query = query.Where(b => b.Event.EventHasTeams.Any(et => et.Team.SportId == sportId.Value));
            }

            var bets = await query.ToListAsync();

            var summary = new Dictionary<string, decimal>
            {
                ["TotalBets"] = bets.Count,
                ["TotalStaked"] = bets.Sum(b => b.Stake),
                ["WonBets"] = bets.Count(b => b.BetStatus == "W"),
                ["LostBets"] = bets.Count(b => b.BetStatus == "L"),
                ["PendingBets"] = bets.Count(b => b.BetStatus == "P"),
                ["WinRate"] = bets.Count > 0 ? (decimal)bets.Count(b => b.BetStatus == "W") / bets.Count * 100 : 0,
                ["AverageStake"] = bets.Count > 0 ? bets.Average(b => b.Stake) : 0,
                ["AverageOdds"] = bets.Count > 0 ? bets.Average(b => b.Odds) : 0,
                ["TotalPayout"] = bets.Where(b => b.BetStatus == "W").Sum(b => b.Payout)
            };

            // Estadísticas por deporte
            var sportStats = bets
                .SelectMany(b => b.Event.EventHasTeams.Select(et => new { Bet = b, Sport = et.Team.Sport }))
                .GroupBy(x => x.Sport.Name)
                .Select(g => new Dictionary<string, object>
                {
                    ["Sport"] = g.Key,
                    ["BetCount"] = g.Count(),
                    ["TotalStaked"] = g.Sum(x => x.Bet.Stake),
                    ["WinRate"] = g.Count() > 0 ? (decimal)g.Count(x => x.Bet.BetStatus == "W") / g.Count() * 100 : 0,
                    ["Revenue"] = g.Where(x => x.Bet.BetStatus == "L").Sum(x => x.Bet.Stake) -
                                 g.Where(x => x.Bet.BetStatus == "W").Sum(x => x.Bet.Payout - x.Bet.Stake)
                })
                .ToList();

            return new ReportViewModel
            {
                ReportType = "BettingStats",
                StartDate = startDate,
                EndDate = endDate,
                Summary = summary,
                DetailedData = sportStats
            };
        }

        public async Task<ReportViewModel> GeneratePaymentSummaryReportAsync(DateTime startDate, DateTime endDate)
        {
            var transactions = await _context.PaymentTransactions
                .Include(t => t.PaymentMethod)
                .Include(t => t.User)
                .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToListAsync();

            var summary = new Dictionary<string, decimal>
            {
                ["TotalTransactions"] = transactions.Count,
                ["SuccessfulTransactions"] = transactions.Count(t => t.Status == "COMPLETED"),
                ["FailedTransactions"] = transactions.Count(t => t.Status == "FAILED"),
                ["PendingTransactions"] = transactions.Count(t => t.Status == "PENDING"),
                ["TotalProcessed"] = transactions.Where(t => t.Status == "COMPLETED").Sum(t => t.Amount),
                ["AverageTransactionAmount"] = transactions.Where(t => t.Status == "COMPLETED").DefaultIfEmpty().Average(t => t?.Amount ?? 0)
            };

            // Por método de pago
            var paymentMethodStats = transactions
                .GroupBy(t => t.PaymentMethod?.ProviderName ?? "Unknown")
                .Select(g => new Dictionary<string, object>
                {
                    ["PaymentMethod"] = g.Key,
                    ["TransactionCount"] = g.Count(),
                    ["SuccessRate"] = g.Count() > 0 ? (decimal)g.Count(t => t.Status == "COMPLETED") / g.Count() * 100 : 0,
                    ["TotalAmount"] = g.Where(t => t.Status == "COMPLETED").Sum(t => t.Amount),
                    ["AverageAmount"] = g.Where(t => t.Status == "COMPLETED").DefaultIfEmpty().Average(t => t?.Amount ?? 0)
                })
                .ToList();

            return new ReportViewModel
            {
                ReportType = "PaymentSummary",
                StartDate = startDate,
                EndDate = endDate,
                Summary = summary,
                DetailedData = paymentMethodStats
            };
        }

        public async Task<DashboardViewModel> GetAdminDashboardDataAsync()
        {
            var now = DateTime.Now;
            var today = now.Date;
            var thirtyDaysAgo = now.AddDays(-30);

            // Usuarios
            var totalUsers = await _context.UserAccounts.CountAsync();
            var activeUsers = await _context.UserAccounts
                .CountAsync(u => u.LastBet >= thirtyDaysAgo);

            // Apuestas
            var totalBets = await _context.Bets.CountAsync();
            var todayBets = await _context.Bets
                .CountAsync(b => b.CreatedAt >= today);

            // Ingresos
            var completedBets = await _context.Bets
                .Where(b => b.BetStatus == "W" || b.BetStatus == "L")
                .ToListAsync();

            var totalRevenue = completedBets.Where(b => b.BetStatus == "L").Sum(b => b.Stake) -
                              completedBets.Where(b => b.BetStatus == "W").Sum(b => b.Payout - b.Stake);

            var todayRevenue = completedBets
                .Where(b => b.UpdatedAt >= today)
                .Where(b => b.BetStatus == "L").Sum(b => b.Stake) -
                completedBets
                .Where(b => b.UpdatedAt >= today && b.BetStatus == "W")
                .Sum(b => b.Payout - b.Stake);

            var totalPayouts = completedBets.Where(b => b.BetStatus == "W").Sum(b => b.Payout);

            // Gráficos de ingresos (últimos 7 días)
            var revenueChart = new List<ChartDataPoint>();
            var betsChart = new List<ChartDataPoint>();

            for (int i = 6; i >= 0; i--)
            {
                var date = today.AddDays(-i);
                var nextDate = date.AddDays(1);

                var dayRevenue = await _context.Bets
                    .Where(b => b.UpdatedAt >= date && b.UpdatedAt < nextDate)
                    .Where(b => b.BetStatus == "L")
                    .SumAsync(b => b.Stake);

                var dayBets = await _context.Bets
                    .CountAsync(b => b.CreatedAt >= date && b.CreatedAt < nextDate);

                revenueChart.Add(new ChartDataPoint
                {
                    Label = date.ToString("dd/MM"),
                    Value = dayRevenue
                });

                betsChart.Add(new ChartDataPoint
                {
                    Label = date.ToString("dd/MM"),
                    Value = dayBets
                });
            }

            // Estadísticas por deporte
            var sportStats = await _context.Bets
                .Include(b => b.Event)
                    .ThenInclude(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                    .ThenInclude(t => t.Sport)
                .Where(b => b.CreatedAt >= thirtyDaysAgo)
                .SelectMany(b => b.Event.EventHasTeams.Select(et => new { Bet = b, Sport = et.Team.Sport }))
                .GroupBy(x => x.Sport.Name)
                .Select(g => new SportStatsViewModel
                {
                    SportName = g.Key,
                    TotalBets = g.Count(),
                    TotalStaked = g.Sum(x => x.Bet.Stake),
                    TotalPayout = g.Where(x => x.Bet.BetStatus == "W").Sum(x => x.Bet.Payout),
                    Profit = g.Where(x => x.Bet.BetStatus == "L").Sum(x => x.Bet.Stake) -
                            g.Where(x => x.Bet.BetStatus == "W").Sum(x => x.Bet.Payout - x.Bet.Stake),
                    ProfitMargin = 0 // Se calcula después
                })
                .ToListAsync();

            foreach (var sport in sportStats)
            {
                if (sport.TotalStaked > 0)
                {
                    sport.ProfitMargin = (sport.Profit / sport.TotalStaked) * 100;
                }
            }

            // Actividades recientes
            var recentActivities = new List<RecentActivityViewModel>();

            // Últimas apuestas
            var recentBets = await _context.Bets
                .Include(b => b.Users)
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var bet in recentBets)
            {
                var user = bet.Users.FirstOrDefault();
                recentActivities.Add(new RecentActivityViewModel
                {
                    Timestamp = bet.CreatedAt,
                    ActivityType = "BET",
                    Description = $"Nueva apuesta realizada",
                    UserName = user?.UserName ?? "Unknown",
                    Amount = bet.Stake
                });
            }

            // Últimas transacciones
            var recentTransactions = await _context.PaymentTransactions
                .Include(t => t.User)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();

            foreach (var transaction in recentTransactions)
            {
                recentActivities.Add(new RecentActivityViewModel
                {
                    Timestamp = transaction.CreatedAt,
                    ActivityType = transaction.TransactionType,
                    Description = GetTransactionDescription(transaction.TransactionType),
                    UserName = transaction.User.UserName,
                    Amount = transaction.Amount
                });
            }

            // Ordena actividades por timestamp
            recentActivities = recentActivities.OrderByDescending(a => a.Timestamp).Take(10).ToList();

            return new DashboardViewModel
            {
                TotalUsers = totalUsers,
                ActiveUsers = activeUsers,
                TotalBets = totalBets,
                TodayBets = todayBets,
                TotalRevenue = totalRevenue,
                TodayRevenue = todayRevenue,
                TotalPayouts = totalPayouts,
                HouseEdge = totalRevenue > 0 ? (totalRevenue / (totalRevenue + totalPayouts)) * 100 : 0,
                RevenueChart = revenueChart,
                BetsChart = betsChart,
                SportStats = sportStats,
                RecentActivities = recentActivities
            };
        }

        public async Task<byte[]> ExportReportToCsvAsync(ReportViewModel report)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new StreamWriter(memoryStream, Encoding.UTF8);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            // Escribe resumen
            await writer.WriteLineAsync($"Reporte: {report.ReportType}");
            await writer.WriteLineAsync($"Período: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}");
            await writer.WriteLineAsync("");
            await writer.WriteLineAsync("RESUMEN");

            foreach (var item in report.Summary)
            {
                await writer.WriteLineAsync($"{item.Key},{item.Value}");
            }

            await writer.WriteLineAsync("");
            await writer.WriteLineAsync("DETALLE");

            // Escribe datos detallados
            if (report.DetailedData.Any())
            {
                // Headers
                var headers = report.DetailedData.First().Keys;
                await csv.WriteRecordsAsync(new[] { headers });

                // Data
                foreach (var row in report.DetailedData)
                {
                    await csv.WriteRecordsAsync(new[] { row.Values });
                }
            }

            await writer.FlushAsync();
            return memoryStream.ToArray();
        }

        public async Task<byte[]> ExportReportToPdfAsync(ReportViewModel report)
        {
            // Implementación simplificada - en producción usarías una librería como iTextSharp o similar
            var pdfContent = new StringBuilder();
            pdfContent.AppendLine($"REPORTE: {report.ReportType}");
            pdfContent.AppendLine($"Fecha: {DateTime.Now:dd/MM/yyyy HH:mm}");
            pdfContent.AppendLine($"Período: {report.StartDate:dd/MM/yyyy} - {report.EndDate:dd/MM/yyyy}");
            pdfContent.AppendLine();
            pdfContent.AppendLine("RESUMEN:");

            foreach (var item in report.Summary)
            {
                pdfContent.AppendLine($"{item.Key}: {item.Value:N2}");
            }

            // En producción, aquí se generaría un PDF real
            return Encoding.UTF8.GetBytes(pdfContent.ToString());
        }

        public async Task<bool> ScheduleReportAsync(string reportType, string frequency, int userId, string email)
        {
            try
            {
                // En producción, esto se integraría con un servicio de scheduling como Hangfire o Azure Functions
                _logger.LogInformation("Scheduling {ReportType} report with {Frequency} frequency for user {UserId}",
                    reportType, frequency, userId);

                // Registra en log
                var reportLog = new ReportLog
                {
                    UserId = userId,
                    ReportType = $"SCHEDULED_{reportType}_{frequency}",
                    GeneratedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.ReportLogs.Add(reportLog);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling report");
                return false;
            }
        }

        public async Task<List<ReportLog>> GetReportHistoryAsync(int userId)
        {
            return await _context.ReportLogs
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.GeneratedAt)
                .Take(50)
                .ToListAsync();
        }

        public async Task<Dictionary<string, object>> GetRealTimeStatsAsync()
        {
            var now = DateTime.Now;
            var fiveMinutesAgo = now.AddMinutes(-5);
            var oneHourAgo = now.AddHours(-1);

            var stats = new Dictionary<string, object>
            {
                ["ActiveUsers"] = await _context.LoginAttempts
                    .Where(la => la.AttemptTime >= fiveMinutesAgo && la.IsSuccessful)
                    .Select(la => la.UserId)
                    .Distinct()
                    .CountAsync(),

                ["RecentBets"] = await _context.Bets
                    .CountAsync(b => b.CreatedAt >= fiveMinutesAgo),

                ["LiveEvents"] = await _context.Events
                    .CountAsync(e => e.Date <= now && e.Date >= now.AddHours(-3) && string.IsNullOrEmpty(e.Outcome)),

                ["PendingWithdrawals"] = await _context.PaymentTransactions
                    .CountAsync(t => t.TransactionType == "WITHDRAWAL" && t.Status == "PENDING"),

                ["HourlyVolume"] = await _context.Bets
                    .Where(b => b.CreatedAt >= oneHourAgo)
                    .SumAsync(b => b.Stake),

                ["ServerTime"] = now
            };

            return stats;
        }

        // Métodos auxiliares privados
        private string GetTransactionDescription(string transactionType)
        {
            return transactionType switch
            {
                "DEPOSIT" => "Depósito realizado",
                "WITHDRAWAL" => "Retiro solicitado",
                "BET" => "Apuesta realizada",
                "PAYOUT" => "Pago de ganancia",
                "REFUND" => "Reembolso procesado",
                _ => transactionType
            };
        }
    }
}