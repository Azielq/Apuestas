using Proyecto_Apuestas.Models;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IOddsService
    {
        Task<Dictionary<int, decimal>> GetLatestOddsForEventAsync(int eventId);
        Task<bool> UpdateOddsAsync(int eventId, Dictionary<int, decimal> teamOdds);
        Task<List<OddsHistory>> GetOddsHistoryAsync(int eventId, int? teamId = null, int days = 7);
        Task<decimal> CalculateOddsMovementAsync(int eventId, int teamId);
        Task<bool> ImportOddsFromExternalSourceAsync(int eventId);
        Task<Dictionary<string, object>> AnalyzeOddsPatternsAsync(int eventId);
        Task<bool> SetInitialOddsAsync(int eventId, Dictionary<int, decimal> initialOdds);
        Task<decimal> GetAverageOddsAsync(int teamId, int sportId);
        Task<bool> AdjustOddsBasedOnBettingVolumeAsync(int eventId);
        Task<List<Event>> GetEventsWithFavorableOddsAsync(decimal threshold);
    }
}