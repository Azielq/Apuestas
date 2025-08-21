using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IEventService
    {
        Task<List<EventListViewModel>> GetUpcomingEventsAsync(int? sportId = null, string? searchTerm = null);
        Task<EventDetailsViewModel?> GetEventDetailsAsync(int eventId);
        Task<UpcomingEventsViewModel> GetUpcomingEventsByCategoryAsync();
        Task<List<Event>> GetLiveEventsAsync();
        Task<bool> CreateEventAsync(Event eventModel, List<int> teamIds);
        Task<bool> UpdateEventAsync(int eventId, Event eventModel);
        Task<bool> UpdateEventOutcomeAsync(int eventId, string outcome, int? winningTeamId);
        Task<bool> CancelEventAsync(int eventId, string reason);
        Task<List<OddsViewModel>> GetEventOddsAsync(int eventId);
        Task<bool> UpdateEventOddsAsync(int eventId, int teamId, decimal newOdds);
        Task<List<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<int, int>> GetEventBetCountsAsync(List<int> eventIds);
        Task<bool> IsEventBettableAsync(int eventId);
        Task<EventDetailsViewModel?> GetEventByExternalIdAsync(string externalId);
    }
}
