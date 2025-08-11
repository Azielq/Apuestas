using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using AutoMapper;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class EventService : IEventService
    {
        private readonly apuestasDbContext _context;
        private readonly IBettingService _bettingService;
        private readonly INotificationService _notificationService;
        private readonly IMapper _mapper;
        private readonly ILogger<EventService> _logger;

        public EventService(
            apuestasDbContext context,
            IBettingService bettingService,
            INotificationService notificationService,
            IMapper mapper,
            ILogger<EventService> logger)
        {
            _context = context;
            _bettingService = bettingService;
            _notificationService = notificationService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<EventListViewModel>> GetUpcomingEventsAsync(int? sportId = null, string? searchTerm = null)
        {
            var query = _context.Events
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                        .ThenInclude(t => t.Sport)
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                        .ThenInclude(t => t.Images)
                .Where(e => e.Date > DateTime.Now);

            if (sportId.HasValue)
            {
                query = query.Where(e => e.EventHasTeams.Any(et => et.Team.SportId == sportId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(e => e.EventHasTeams.Any(et =>
                    et.Team.TeamName.Contains(searchTerm)));
            }

            var events = await query
                .OrderBy(e => e.Date)
                .Take(50)
                .ToListAsync();

            return events.Select(e => MapToEventListViewModel(e)).ToList();
        }

        public async Task<EventDetailsViewModel?> GetEventDetailsAsync(int eventId)
        {
            var eventEntity = await _context.Events
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                        .ThenInclude(t => t.Sport)
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                        .ThenInclude(t => t.Images)
                .Include(e => e.OddsHistories)
                .Include(e => e.Bets)
                .FirstOrDefaultAsync(e => e.EventId == eventId);

            if (eventEntity == null) return null;

            var viewModel = new EventDetailsViewModel
            {
                EventId = eventEntity.EventId,
                ExternalEventId = eventEntity.ExternalEventId,
                Date = eventEntity.Date,
                Outcome = eventEntity.Outcome,
                Teams = await GetEventTeamDetailsAsync(eventEntity),
                CurrentOdds = await GetEventOddsAsync(eventId),
                SportName = eventEntity.EventHasTeams.FirstOrDefault()?.Team.Sport.Name ?? "N/A",
                CompetitionName = "Liga Principal", // Se debería obtener esto de la competición real
                TotalBets = eventEntity.Bets.Count,
                TotalStaked = eventEntity.Bets.Sum(b => b.Stake),
                CanBet = await IsEventBettableAsync(eventId)
            };

            return viewModel;
        }

        public async Task<UpcomingEventsViewModel> GetUpcomingEventsByCategoryAsync()
        {
            var now = DateTime.Now;
            var tomorrow = now.Date.AddDays(1);
            var weekEnd = now.Date.AddDays(7);

            var allUpcomingEvents = await GetUpcomingEventsAsync();

            var viewModel = new UpcomingEventsViewModel
            {
                TodayEvents = allUpcomingEvents.Where(e => e.Date.Date == now.Date).ToList(),
                TomorrowEvents = allUpcomingEvents.Where(e => e.Date.Date == tomorrow).ToList(),
                WeekEvents = allUpcomingEvents.Where(e => e.Date > tomorrow && e.Date <= weekEnd).ToList(),
                Sports = await _context.Sports
                    .Where(s => s.IsActive == true)
                    .ToDictionaryAsync(s => s.SportId, s => s.Name)
            };

            return viewModel;
        }

        public async Task<List<Event>> GetLiveEventsAsync()
        {
            var now = DateTime.Now;
            return await _context.Events
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                .Where(e => e.Date <= now &&
                           e.Date >= now.AddHours(-3) && // Asumiendo eventos de 3 horas máximo
                           string.IsNullOrEmpty(e.Outcome))
                .ToListAsync();
        }

        public async Task<bool> CreateEventAsync(Event eventModel, List<int> teamIds)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _context.Events.Add(eventModel);
                await _context.SaveChangesAsync();

                // Asocia equipos
                foreach (var teamId in teamIds)
                {
                    _context.EventHasTeams.Add(new EventHasTeam
                    {
                        EventId = eventModel.EventId,
                        TeamId = teamId,
                        IsHomeTeam = teamIds.IndexOf(teamId) == 0
                    });
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notifica a usuarios interesados
                await NotifyUsersAboutNewEvent(eventModel.EventId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating event");
                return false;
            }
        }

        public async Task<bool> UpdateEventAsync(int eventId, Event eventModel)
        {
            try
            {
                var existingEvent = await _context.Events.FindAsync(eventId);
                if (existingEvent == null) return false;

                existingEvent.Date = eventModel.Date;
                existingEvent.ExternalEventId = eventModel.ExternalEventId;
                existingEvent.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", eventId);
                return false;
            }
        }

        public async Task<bool> UpdateEventOutcomeAsync(int eventId, string outcome, int? winningTeamId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var eventEntity = await _context.Events.FindAsync(eventId);
                if (eventEntity == null) return false;

                eventEntity.Outcome = outcome;
                eventEntity.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                // Liquida apuestas
                if (winningTeamId.HasValue)
                {
                    await _bettingService.SettleEventBetsAsync(eventId, winningTeamId.Value);
                }

                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error updating event outcome {EventId}", eventId);
                return false;
            }
        }

        public async Task<bool> CancelEventAsync(int eventId, string reason)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.Bets)
                    .FirstOrDefaultAsync(e => e.EventId == eventId);

                if (eventEntity == null) return false;

                eventEntity.Outcome = "CANCELLED";
                eventEntity.UpdatedAt = DateTime.Now;

                // Cancela todas las apuestas pendientes
                foreach (var bet in eventEntity.Bets.Where(b => b.BetStatus == "P"))
                {
                    bet.BetStatus = "C";
                    bet.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Notifica a usuarios afectados
                await NotifyUsersAboutCancelledEvent(eventId, reason);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error cancelling event {EventId}", eventId);
                return false;
            }
        }

        public async Task<List<OddsViewModel>> GetEventOddsAsync(int eventId)
        {
            var latestOdds = await _context.OddsHistories
                .Include(o => o.Team)
                .Where(o => o.EventId == eventId)
                .GroupBy(o => o.TeamId)
                .Select(g => g.OrderByDescending(o => o.RetrievedAt).First())
                .ToListAsync();

            return latestOdds.Select(o => new OddsViewModel
            {
                TeamId = o.TeamId,
                TeamName = o.Team.TeamName,
                Odds = o.Odds,
                Source = o.Source,
                RetrievedAt = o.RetrievedAt,
                Trend = CalculateOddsTrend(o.EventId, o.TeamId, o.Odds)
            }).ToList();
        }

        public async Task<bool> UpdateEventOddsAsync(int eventId, int teamId, decimal newOdds)
        {
            try
            {
                var oddsHistory = new OddsHistory
                {
                    EventId = eventId,
                    TeamId = teamId,
                    Odds = newOdds,
                    Source = "SISTEMA",
                    RetrievedAt = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.OddsHistories.Add(oddsHistory);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating odds for event {EventId} team {TeamId}", eventId, teamId);
                return false;
            }
        }

        public async Task<List<Event>> GetEventsByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Events
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                .Where(e => e.Date >= startDate && e.Date <= endDate)
                .OrderBy(e => e.Date)
                .ToListAsync();
        }

        public async Task<Dictionary<int, int>> GetEventBetCountsAsync(List<int> eventIds)
        {
            return await _context.Bets
                .Where(b => eventIds.Contains(b.EventId))
                .GroupBy(b => b.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventId, x => x.Count);
        }

        public async Task<bool> IsEventBettableAsync(int eventId)
        {
            var eventEntity = await _context.Events.FindAsync(eventId);
            if (eventEntity == null) return false;

            // No se puede apostar si:
            // 1. El evento ya comenzó (con 5 minutos de margen)
            // 2. El evento está cancelado
            // 3. El evento ya tiene resultado
            return eventEntity.Date > DateTime.Now.AddMinutes(5) &&
                   eventEntity.Outcome != "CANCELLED" &&
                   string.IsNullOrEmpty(eventEntity.Outcome);
        }

        private EventListViewModel MapToEventListViewModel(Event e)
        {
            var teams = e.EventHasTeams.Select(et => et.Team).ToList();
            var sport = teams.FirstOrDefault()?.Sport;

            return new EventListViewModel
            {
                EventId = e.EventId,
                ExternalEventId = e.ExternalEventId,
                Date = e.Date,
                SportName = sport?.Name ?? "N/A",
                CompetitionName = "Liga Principal", // Se debería obtener esto de la competición real
                Teams = teams.Select(t => new EventTeamViewModel
                {
                    TeamId = t.TeamId,
                    TeamName = t.TeamName,
                    CurrentOdds = GetLatestOdds(e.EventId, t.TeamId),
                    LogoUrl = t.Images.FirstOrDefault()?.Url,
                    IsHomeTeam = e.EventHasTeams.First(et => et.TeamId == t.TeamId).IsHomeTeam
                }).ToList(),
                IsLive = e.Date <= DateTime.Now && e.Date >= DateTime.Now.AddHours(-3),
                IsFinished = !string.IsNullOrEmpty(e.Outcome) || e.Date < DateTime.Now.AddHours(-3),
                Outcome = e.Outcome
            };
        }

        private async Task<List<EventTeamDetailsViewModel>> GetEventTeamDetailsAsync(Event eventEntity)
        {
            var teamDetails = new List<EventTeamDetailsViewModel>();

            foreach (var eventTeam in eventEntity.EventHasTeams)
            {
                var team = eventTeam.Team;
                var oddsHistory = await _context.OddsHistories
                    .Where(o => o.EventId == eventEntity.EventId && o.TeamId == team.TeamId)
                    .OrderByDescending(o => o.RetrievedAt)
                    .Take(10)
                    .Select(o => o.Odds)
                    .ToListAsync();

                teamDetails.Add(new EventTeamDetailsViewModel
                {
                    TeamId = team.TeamId,
                    TeamName = team.TeamName,
                    CurrentOdds = oddsHistory.FirstOrDefault(),
                    LogoUrl = team.Images.FirstOrDefault()?.Url,
                    IsHomeTeam = eventTeam.IsHomeTeam,
                    TeamWinPercent = team.TeamWinPercent,
                    TeamDrawPercent = team.TeamDrawPercent,
                    LastWin = team.LastWin,
                    OddsHistory = oddsHistory
                });
            }

            return teamDetails;
        }

        private decimal GetLatestOdds(int eventId, int teamId)
        {
            var latestOdds = _context.OddsHistories
                .Where(o => o.EventId == eventId && o.TeamId == teamId)
                .OrderByDescending(o => o.RetrievedAt)
                .FirstOrDefault();

            return latestOdds?.Odds ?? 2.0m; // Valor por defecto si no hay cuotas
        }

        private string CalculateOddsTrend(int eventId, int teamId, decimal currentOdds)
        {
            var previousOdds = _context.OddsHistories
                .Where(o => o.EventId == eventId && o.TeamId == teamId)
                .OrderByDescending(o => o.RetrievedAt)
                .Skip(1)
                .FirstOrDefault();

            if (previousOdds == null) return "stable";

            if (currentOdds > previousOdds.Odds) return "up";
            if (currentOdds < previousOdds.Odds) return "down";
            return "stable";
        }

        private async Task NotifyUsersAboutNewEvent(int eventId)
        {
            // Implementa lógica para notificar a usuarios interesados
            var eventEntity = await GetEventDetailsAsync(eventId);
            if (eventEntity == null) return;

            // Por ejemplo, notifica a usuarios que siguen a estos equipos
            var teamIds = eventEntity.Teams.Select(t => t.TeamId).ToList();

            // Aquí se implementaría la lógica real de notificaciones
            _logger.LogInformation("Notifying users about new event {EventId}", eventId);
        }

        private async Task NotifyUsersAboutCancelledEvent(int eventId, string reason)
        {
            var affectedBets = await _context.Bets
                .Include(b => b.Users)
                .Where(b => b.EventId == eventId && b.BetStatus == "C")
                .ToListAsync();

            foreach (var bet in affectedBets)
            {
                foreach (var user in bet.Users)
                {
                    await _notificationService.SendNotificationAsync(user.UserId,
                        $"El evento en el que apostaste ha sido cancelado. Motivo: {reason}. " +
                        $"Se te ha devuelto ${bet.Stake:N2} a tu cuenta.");
                }
            }
        }

        public async Task<EventDetailsViewModel?> GetEventByExternalIdAsync(string externalId)
        {
            var eventEntity = await _context.Events
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                        .ThenInclude(t => t.Sport)
                .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                        .ThenInclude(t => t.Images)
                .Include(e => e.OddsHistories)
                .Include(e => e.Bets)
                .FirstOrDefaultAsync(e => e.ExternalEventId == externalId);

            if (eventEntity == null) return null;

            var teams = eventEntity.EventHasTeams.Select(et => et.Team).ToList();
            var sport = teams.FirstOrDefault()?.Sport;

            var viewModel = new EventDetailsViewModel
            {
                EventId = eventEntity.EventId,
                ExternalEventId = eventEntity.ExternalEventId,
                Date = eventEntity.Date,
                Outcome = eventEntity.Outcome,
                Teams = new List<EventTeamDetailsViewModel>(),
                CurrentOdds = await GetEventOddsAsync(eventEntity.EventId),
                SportName = sport?.Name ?? "N/A",
                CompetitionName = "Liga Principal", // Puedes obtener esto de una relación real si existe
                TotalBets = eventEntity.Bets.Count,
                TotalStaked = eventEntity.Bets.Sum(b => b.Stake),
                CanBet = await IsEventBettableAsync(eventEntity.EventId)
            };

            // Mapear los equipos
            foreach (var eventTeam in eventEntity.EventHasTeams)
            {
                var team = eventTeam.Team;
                var oddsHistory = await _context.OddsHistories
                    .Where(o => o.EventId == eventEntity.EventId && o.TeamId == team.TeamId)
                    .OrderByDescending(o => o.RetrievedAt)
                    .Take(10)
                    .Select(o => o.Odds)
                    .ToListAsync();

                viewModel.Teams.Add(new EventTeamDetailsViewModel
                {
                    TeamId = team.TeamId,
                    TeamName = team.TeamName,
                    CurrentOdds = oddsHistory.FirstOrDefault(),
                    LogoUrl = team.Images.FirstOrDefault()?.Url,
                    IsHomeTeam = eventTeam.IsHomeTeam,
                    TeamWinPercent = team.TeamWinPercent,
                    TeamDrawPercent = team.TeamDrawPercent,
                    LastWin = team.LastWin,
                    OddsHistory = oddsHistory
                });
            }

            return viewModel;
        }
    }
}
