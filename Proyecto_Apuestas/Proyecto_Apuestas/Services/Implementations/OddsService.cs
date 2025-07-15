using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class OddsService : IOddsService
    {
        private readonly apuestasDbContext _context;
        private readonly ILogger<OddsService> _logger;
        private readonly IConfiguration _configuration;

        public OddsService(
            apuestasDbContext context,
            ILogger<OddsService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<Dictionary<int, decimal>> GetLatestOddsForEventAsync(int eventId)
        {
            var latestOdds = await _context.OddsHistories
                .Where(o => o.EventId == eventId)
                .GroupBy(o => o.TeamId)
                .Select(g => new
                {
                    TeamId = g.Key,
                    Odds = g.OrderByDescending(o => o.RetrievedAt).First().Odds
                })
                .ToDictionaryAsync(x => x.TeamId, x => x.Odds);

            return latestOdds;
        }

        public async Task<bool> UpdateOddsAsync(int eventId, Dictionary<int, decimal> teamOdds)
        {
            try
            {
                var oddsHistories = teamOdds.Select(kvp => new OddsHistory
                {
                    EventId = eventId,
                    TeamId = kvp.Key,
                    Odds = kvp.Value,
                    Source = "SYSTEM",
                    RetrievedAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }).ToList();

                await _context.OddsHistories.AddRangeAsync(oddsHistories);
                await _context.SaveChangesAsync();

                // NOTE: Esto nos ajusta cuotas si hay mucho volumen en un lado
                await AdjustOddsBasedOnBettingVolumeAsync(eventId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating odds for event {EventId}", eventId);
                return false;
            }
        }

        public async Task<List<OddsHistory>> GetOddsHistoryAsync(int eventId, int? teamId = null, int days = 7)
        {
            var since = DateTime.Now.AddDays(-days);
            var query = _context.OddsHistories
                .Where(o => o.EventId == eventId && o.RetrievedAt >= since);

            if (teamId.HasValue)
            {
                query = query.Where(o => o.TeamId == teamId.Value);
            }

            return await query
                .OrderBy(o => o.RetrievedAt)
                .ToListAsync();
        }

        public async Task<decimal> CalculateOddsMovementAsync(int eventId, int teamId)
        {
            var recentOdds = await _context.OddsHistories
                .Where(o => o.EventId == eventId && o.TeamId == teamId)
                .OrderByDescending(o => o.RetrievedAt)
                .Take(2)
                .Select(o => o.Odds)
                .ToListAsync();

            if (recentOdds.Count < 2) return 0;

            return ((recentOdds[0] - recentOdds[1]) / recentOdds[1]) * 100;
        }

        public async Task<bool> ImportOddsFromExternalSourceAsync(int eventId)
        {
            try
            {
                // Simula importación desde API externa
                // En producción, aquí se llamaría a APIs reales de proveedores de cuotas
                var random = new Random();
                var teams = await _context.EventHasTeams
                    .Where(et => et.EventId == eventId)
                    .Select(et => et.TeamId)
                    .ToListAsync();

                var externalOdds = teams.ToDictionary(
                    teamId => teamId,
                    teamId => Math.Round((decimal)(random.NextDouble() * 3 + 1.5), 2)
                );

                var oddsHistories = externalOdds.Select(kvp => new OddsHistory
                {
                    EventId = eventId,
                    TeamId = kvp.Key,
                    Odds = kvp.Value,
                    Source = "EXTERNAL_API",
                    RetrievedAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }).ToList();

                await _context.OddsHistories.AddRangeAsync(oddsHistories);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing odds from external source");
                return false;
            }
        }

        public async Task<Dictionary<string, object>> AnalyzeOddsPatternsAsync(int eventId)
        {
            var oddsHistory = await GetOddsHistoryAsync(eventId, days: 30);
            var teamOdds = oddsHistory.GroupBy(o => o.TeamId);

            var analysis = new Dictionary<string, object>();

            foreach (var teamGroup in teamOdds)
            {
                var odds = teamGroup.Select(o => o.Odds).ToList();

                analysis[$"Team_{teamGroup.Key}_Average"] = odds.Average();
                analysis[$"Team_{teamGroup.Key}_Min"] = odds.Min();
                analysis[$"Team_{teamGroup.Key}_Max"] = odds.Max();
                analysis[$"Team_{teamGroup.Key}_Volatility"] = CalculateVolatility(odds);
                analysis[$"Team_{teamGroup.Key}_Trend"] = CalculateTrend(odds);
            }

            analysis["TotalUpdates"] = oddsHistory.Count;
            analysis["LastUpdate"] = oddsHistory.MaxBy(o => o.RetrievedAt)?.RetrievedAt ?? DateTime.MinValue;

            return analysis;
        }

        public async Task<bool> SetInitialOddsAsync(int eventId, Dictionary<int, decimal> initialOdds)
        {
            try
            {
                // Verifica que no existan cuotas previas
                var existingOdds = await _context.OddsHistories
                    .AnyAsync(o => o.EventId == eventId);

                if (existingOdds) return false;

                var oddsHistories = initialOdds.Select(kvp => new OddsHistory
                {
                    EventId = eventId,
                    TeamId = kvp.Key,
                    Odds = kvp.Value,
                    Source = "INITIAL",
                    RetrievedAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }).ToList();

                await _context.OddsHistories.AddRangeAsync(oddsHistories);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting initial odds");
                return false;
            }
        }

        public async Task<decimal> GetAverageOddsAsync(int teamId, int sportId)
        {
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            var averageOdds = await _context.OddsHistories
                .Where(o => o.TeamId == teamId &&
                           o.Team.SportId == sportId &&
                           o.RetrievedAt >= thirtyDaysAgo)
                .AverageAsync(o => (decimal?)o.Odds) ?? 2.0m;

            return Math.Round(averageOdds, 2);
        }

        public async Task<bool> AdjustOddsBasedOnBettingVolumeAsync(int eventId)
        {
            try
            {
                // Obtiene volumen de apuestas por equipo
                var bettingVolume = await _context.Bets
                    .Where(b => b.EventId == eventId && b.BetStatus == "P")
                    .GroupBy(b => b.Event.EventHasTeams.First().TeamId) // Simplificado
                    .Select(g => new
                    {
                        TeamId = g.Key,
                        TotalStake = g.Sum(b => b.Stake),
                        BetCount = g.Count()
                    })
                    .ToListAsync();

                if (!bettingVolume.Any()) return true;

                var totalVolume = bettingVolume.Sum(v => v.TotalStake);
                var currentOdds = await GetLatestOddsForEventAsync(eventId);
                var adjustedOdds = new Dictionary<int, decimal>();

                foreach (var teamVolume in bettingVolume)
                {
                    if (!currentOdds.ContainsKey(teamVolume.TeamId)) continue;

                    var volumePercentage = teamVolume.TotalStake / totalVolume;
                    var currentOdd = currentOdds[teamVolume.TeamId];

                    // Si hay mucho volumen en un equipo, reduce sus cuotas
                    decimal adjustment = 1m;
                    if (volumePercentage > 0.6m)
                        adjustment = 0.95m; // Reduce 5%
                    else if (volumePercentage > 0.7m)
                        adjustment = 0.90m; // Reduce 10%
                    else if (volumePercentage < 0.3m)
                        adjustment = 1.05m; // Aumenta 5%

                    adjustedOdds[teamVolume.TeamId] = Math.Round(currentOdd * adjustment, 2);
                }

                if (adjustedOdds.Any())
                {
                    await UpdateOddsAsync(eventId, adjustedOdds);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting odds based on betting volume");
                return false;
            }
        }

        public async Task<List<Event>> GetEventsWithFavorableOddsAsync(decimal threshold)
        {
            var upcomingEvents = await _context.Events
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                .Where(e => e.Date > DateTime.Now && string.IsNullOrEmpty(e.Outcome))
                .ToListAsync();

            var favorableEvents = new List<Event>();

            foreach (var evt in upcomingEvents)
            {
                var odds = await GetLatestOddsForEventAsync(evt.EventId);
                if (odds.Any(o => o.Value >= threshold))
                {
                    favorableEvents.Add(evt);
                }
            }

            return favorableEvents;
        }

        // Métodos auxiliares privados
        private decimal CalculateVolatility(List<decimal> odds)
        {
            if (odds.Count < 2) return 0;

            var mean = odds.Average();
            var sumOfSquares = odds.Sum(o => Math.Pow((double)(o - mean), 2));
            var variance = sumOfSquares / (odds.Count - 1);

            return (decimal)Math.Sqrt(variance);
        }

        private string CalculateTrend(List<decimal> odds)
        {
            if (odds.Count < 2) return "stable";

            var firstHalf = odds.Take(odds.Count / 2).Average();
            var secondHalf = odds.Skip(odds.Count / 2).Average();

            if (secondHalf > firstHalf * 1.05m) return "increasing";
            if (secondHalf < firstHalf * 0.95m) return "decreasing";

            return "stable";
        }
    }
}