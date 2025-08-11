using Proyecto_Apuestas.Models.API;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IOddsApiService
    {
        // Deportes
        Task<List<SportApiModel>> GetSportsAsync();
        Task<SportApiModel?> GetSportAsync(string sportKey);

        // Eventos/Partidos
        Task<List<EventApiModel>> GetEventsAsync(string sportKey, string? regions = null, string? markets = null);
        Task<List<EventApiModel>> GetLiveEventsAsync(string sportKey);
        Task<EventApiModel?> GetEventAsync(string sportKey, string eventId);

        // Cuotas/Odds
        Task<List<OddsApiModel>> GetOddsAsync(string sportKey, string? regions = null, string? markets = null);
        Task<List<OddsApiModel>> GetLiveOddsAsync(string sportKey);

        // Sincronización con base de datos local
        Task<bool> SyncSportsWithDatabaseAsync();
        Task<bool> SyncEventsWithDatabaseAsync(string sportKey);
        Task<bool> UpdateOddsForEventAsync(int localEventId, string apiEventId);

        // Utilidades
        Task<ApiUsageModel> GetApiUsageAsync();
        Task<bool> IsApiAvailableAsync();
    }
}