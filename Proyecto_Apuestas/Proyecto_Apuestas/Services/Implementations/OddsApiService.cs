using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Models.API;
using Proyecto_Apuestas.Services.Interfaces;
using System.Net.Http.Headers;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class OddsApiService : IOddsApiService
    {
        private readonly HttpClient _httpClient;
        private readonly apuestasDbContext _context;
        private readonly ILogger<OddsApiService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _baseUrl;

        public OddsApiService(
            HttpClient httpClient,
            apuestasDbContext context,
            ILogger<OddsApiService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;
            _configuration = configuration;

            // Configuración de la API
            _apiKey = configuration["OddsApi:ApiKey"] ?? "d987329b3f5c3b08d0ba38ea3014abe2";
            _baseUrl = configuration["OddsApi:BaseUrl"] ?? "https://api.the-odds-api.com/";

            _httpClient.BaseAddress = new Uri(_baseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<List<SportApiModel>> GetSportsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"v4/sports?apiKey={_apiKey}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var sports = JsonConvert.DeserializeObject<List<SportApiModel>>(content);

                    // Log el uso de la API
                    LogApiUsage(response.Headers);

                    return sports ?? new List<SportApiModel>();
                }

                _logger.LogError($"Error al obtener deportes: {response.StatusCode}");
                return new List<SportApiModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al conectar con The Odds API");
                return new List<SportApiModel>();
            }
        }

        public async Task<SportApiModel?> GetSportAsync(string sportKey)
        {
            var sports = await GetSportsAsync();
            return sports.FirstOrDefault(s => s.Key == sportKey);
        }

        public async Task<List<EventApiModel>> GetEventsAsync(string sportKey, string? regions = null, string? markets = null)
        {
            try
            {
                var url = $"v4/sports/{sportKey}/events?apiKey={_apiKey}";

                if (!string.IsNullOrEmpty(regions))
                    url += $"&regions={regions}";

                if (!string.IsNullOrEmpty(markets))
                    url += $"&markets={markets}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var events = JsonConvert.DeserializeObject<List<EventApiModel>>(content);

                    LogApiUsage(response.Headers);

                    return events ?? new List<EventApiModel>();
                }

                _logger.LogError($"Error al obtener eventos: {response.StatusCode}");
                return new List<EventApiModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener eventos para {sportKey}");
                return new List<EventApiModel>();
            }
        }

        public async Task<List<EventApiModel>> GetLiveEventsAsync(string sportKey)
        {
            try
            {
                var url = $"v4/sports/{sportKey}/events/live?apiKey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var events = JsonConvert.DeserializeObject<List<EventApiModel>>(content);

                    LogApiUsage(response.Headers);

                    return events ?? new List<EventApiModel>();
                }

                return new List<EventApiModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener eventos en vivo para {sportKey}");
                return new List<EventApiModel>();
            }
        }

        public async Task<EventApiModel?> GetEventAsync(string sportKey, string eventId)
        {
            try
            {
                var url = $"v4/sports/{sportKey}/events/{eventId}?apiKey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var eventModel = JsonConvert.DeserializeObject<EventApiModel>(content);

                    LogApiUsage(response.Headers);

                    return eventModel;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener evento {eventId}");
                return null;
            }
        }

        public async Task<List<OddsApiModel>> GetOddsAsync(string sportKey, string? regions = null, string? markets = null)
        {
            try
            {
                var url = $"v4/sports/{sportKey}/odds?apiKey={_apiKey}";

                if (!string.IsNullOrEmpty(regions))
                    url += $"&regions={regions}";

                if (!string.IsNullOrEmpty(markets))
                    url += $"&markets={markets}";
                else
                    url += "&markets=h2h,spreads,totals"; // Mercados por defecto

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var odds = JsonConvert.DeserializeObject<List<OddsApiModel>>(content);

                    LogApiUsage(response.Headers);

                    return odds ?? new List<OddsApiModel>();
                }

                _logger.LogError($"Error al obtener cuotas: {response.StatusCode}");
                return new List<OddsApiModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener cuotas para {sportKey}");
                return new List<OddsApiModel>();
            }
        }

        public async Task<List<OddsApiModel>> GetLiveOddsAsync(string sportKey)
        {
            try
            {
                var url = $"v4/sports/{sportKey}/odds-live?apiKey={_apiKey}&markets=h2h";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var odds = JsonConvert.DeserializeObject<List<OddsApiModel>>(content);

                    LogApiUsage(response.Headers);

                    return odds ?? new List<OddsApiModel>();
                }

                return new List<OddsApiModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener cuotas en vivo para {sportKey}");
                return new List<OddsApiModel>();
            }
        }

        public async Task<bool> SyncSportsWithDatabaseAsync()
        {
            try
            {
                var apiSports = await GetSportsAsync();

                foreach (var apiSport in apiSports.Where(s => s.Active))
                {
                    var existingSport = await _context.Sports
                        .FirstOrDefaultAsync(s => s.Name == apiSport.Title);

                    if (existingSport == null)
                    {
                        var newSport = new Sport
                        {
                            Name = apiSport.Title,
                            IsActive = true,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        _context.Sports.Add(newSport);
                        _logger.LogInformation($"Nuevo deporte agregado: {apiSport.Title}");
                    }
                    else
                    {
                        existingSport.IsActive = true;
                        existingSport.UpdatedAt = DateTime.Now;
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al sincronizar deportes");
                return false;
            }
        }

        public async Task<bool> SyncEventsWithDatabaseAsync(string sportKey)
        {
            try
            {
                var apiEvents = await GetOddsAsync(sportKey, "us", "h2h");
                var sport = await GetSportFromKeyAsync(sportKey);

                if (sport == null)
                {
                    _logger.LogWarning($"Deporte no encontrado: {sportKey}");
                    return false;
                }

                foreach (var apiEvent in apiEvents)
                {
                    // Verificar si el evento ya existe
                    var existingEvent = await _context.Events
                        .FirstOrDefaultAsync(e => e.ExternalEventId == apiEvent.Id);

                    if (existingEvent == null)
                    {
                        // Crear nuevo evento
                        var newEvent = new Event
                        {
                            ExternalEventId = apiEvent.Id,
                            Date = apiEvent.CommenceTime,
                            Outcome = "",
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        };

                        _context.Events.Add(newEvent);
                        await _context.SaveChangesAsync();

                        // Crear o buscar equipos y asociarlos al evento
                        await CreateOrUpdateTeamsForEvent(newEvent.EventId, apiEvent, sport.SportId);

                        // Actualizar cuotas
                        await UpdateOddsFromApiEvent(newEvent.EventId, apiEvent);

                        _logger.LogInformation($"Nuevo evento agregado: {apiEvent.HomeTeam} vs {apiEvent.AwayTeam}");
                    }
                    else
                    {
                        // Actualizar evento existente
                        existingEvent.Date = apiEvent.CommenceTime;
                        existingEvent.UpdatedAt = DateTime.Now;

                        // Actualizar cuotas
                        await UpdateOddsFromApiEvent(existingEvent.EventId, apiEvent);
                    }
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al sincronizar eventos para {sportKey}");
                return false;
            }
        }

        public async Task<bool> UpdateOddsForEventAsync(int localEventId, string apiEventId)
        {
            try
            {
                var eventEntity = await _context.Events
                    .Include(e => e.EventHasTeams)
                    .ThenInclude(et => et.Team)
                    .FirstOrDefaultAsync(e => e.EventId == localEventId);

                if (eventEntity == null) return false;

                // Obtener el deporte del evento
                var sportKey = await GetSportKeyFromEventAsync(localEventId);
                if (string.IsNullOrEmpty(sportKey)) return false;

                // Obtener las cuotas actualizadas de la API
                var apiOdds = await GetOddsAsync(sportKey, "us", "h2h");
                var eventOdds = apiOdds.FirstOrDefault(o => o.Id == apiEventId);

                if (eventOdds != null)
                {
                    await UpdateOddsFromApiEvent(localEventId, eventOdds);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar cuotas para evento {localEventId}");
                return false;
            }
        }

        public async Task<ApiUsageModel> GetApiUsageAsync()
        {
            try
            {
                // Hacer una llamada simple para obtener los headers con información de uso
                var response = await _httpClient.GetAsync($"v4/sports?apiKey={_apiKey}&all=false");

                if (response.Headers.TryGetValues("x-requests-used", out var used) &&
                    response.Headers.TryGetValues("x-requests-remaining", out var remaining))
                {
                    return new ApiUsageModel
                    {
                        RequestsUsed = int.Parse(used.First()),
                        RequestsRemaining = int.Parse(remaining.First())
                    };
                }

                return new ApiUsageModel { RequestsUsed = 0, RequestsRemaining = 0 };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener uso de API");
                return new ApiUsageModel { RequestsUsed = 0, RequestsRemaining = 0 };
            }
        }

        public async Task<bool> IsApiAvailableAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"v4/sports?apiKey={_apiKey}&all=false");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // Métodos privados auxiliares
        private void LogApiUsage(HttpResponseHeaders headers)
        {
            if (headers.TryGetValues("x-requests-used", out var used) &&
                headers.TryGetValues("x-requests-remaining", out var remaining))
            {
                _logger.LogInformation($"API Usage - Used: {used.First()}, Remaining: {remaining.First()}");
            }
        }

        private async Task<Sport?> GetSportFromKeyAsync(string sportKey)
        {
            // Mapeo de keys de la API a nombres en la base de datos
            var sportMapping = new Dictionary<string, string>
            {
                ["soccer_epl"] = "Fútbol",
                ["soccer_spain_la_liga"] = "Fútbol",
                ["soccer_germany_bundesliga"] = "Fútbol",
                ["soccer_italy_serie_a"] = "Fútbol",
                ["soccer_france_ligue_one"] = "Fútbol",
                ["americanfootball_nfl"] = "Fútbol Americano",
                ["basketball_nba"] = "Basketball",
                ["baseball_mlb"] = "Béisbol",
                ["tennis_atp_french_open"] = "Tenis",
                ["tennis_wta_french_open"] = "Tenis",
                ["mma_mixed_martial_arts"] = "MMA",
                ["boxing_boxing"] = "Boxeo"
            };

            var sportName = sportMapping.ContainsKey(sportKey) ? sportMapping[sportKey] : "Otros";

            return await _context.Sports.FirstOrDefaultAsync(s => s.Name == sportName);
        }

        private async Task CreateOrUpdateTeamsForEvent(int eventId, OddsApiModel apiEvent, int sportId)
        {
            // Equipo local
            var homeTeam = await GetOrCreateTeam(apiEvent.HomeTeam, sportId);
            _context.EventHasTeams.Add(new EventHasTeam
            {
                EventId = eventId,
                TeamId = homeTeam.TeamId,
                IsHomeTeam = true
            });

            // Equipo visitante
            var awayTeam = await GetOrCreateTeam(apiEvent.AwayTeam, sportId);
            _context.EventHasTeams.Add(new EventHasTeam
            {
                EventId = eventId,
                TeamId = awayTeam.TeamId,
                IsHomeTeam = false
            });

            await _context.SaveChangesAsync();
        }

        private async Task<Team> GetOrCreateTeam(string teamName, int sportId)
        {
            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.TeamName == teamName && t.SportId == sportId);

            if (team == null)
            {
                team = new Team
                {
                    TeamName = teamName,
                    SportId = sportId,
                    TeamWinPercent = 50,
                    TeamDrawPercent = 25,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Teams.Add(team);
                await _context.SaveChangesAsync();
            }

            return team;
        }

        private async Task UpdateOddsFromApiEvent(int eventId, OddsApiModel apiEvent)
        {
            if (apiEvent.Bookmakers == null || !apiEvent.Bookmakers.Any()) return;

            // Usar el primer bookmaker disponible
            var bookmaker = apiEvent.Bookmakers.First();
            var market = bookmaker.Markets?.FirstOrDefault(m => m.Key == "h2h");

            if (market == null) return;

            var eventTeams = await _context.EventHasTeams
                .Include(et => et.Team)
                .Where(et => et.EventId == eventId)
                .ToListAsync();

            foreach (var outcome in market.Outcomes)
            {
                var team = eventTeams.FirstOrDefault(et => et.Team.TeamName == outcome.Name);
                if (team != null)
                {
                    var oddsHistory = new OddsHistory
                    {
                        EventId = eventId,
                        TeamId = team.TeamId,
                        Odds = outcome.Price,
                        Source = bookmaker.Title,
                        RetrievedAt = DateTime.Now,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };

                    _context.OddsHistories.Add(oddsHistory);
                }
            }

            await _context.SaveChangesAsync();
        }

        private async Task<string?> GetSportKeyFromEventAsync(int eventId)
        {
            var eventTeam = await _context.EventHasTeams
                .Include(et => et.Team)
                .ThenInclude(t => t.Sport)
                .FirstOrDefaultAsync(et => et.EventId == eventId);

            if (eventTeam == null) return null;

            // Mapeo inverso de nombres a keys
            var sportName = eventTeam.Team.Sport.Name;
            return sportName switch
            {
                "Fútbol" => "soccer_epl", // Por defecto usar Premier League
                "Basketball" => "basketball_nba",
                "Béisbol" => "baseball_mlb",
                "Tenis" => "tennis_atp_french_open",
                "Boxeo" => "boxing_boxing",
                _ => null
            };
        }
    }
}