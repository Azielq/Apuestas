using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Models.API;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Proyecto_Apuestas.ViewModels.API;
using System.Collections.Generic;
using System.Diagnostics;

namespace Proyecto_Apuestas.Controllers
{
    public class OddsApiController : BaseController
    {
        private readonly IOddsApiService _oddsApiService;
        private readonly IEventService _eventService;
        private readonly IBettingService _bettingService;
        private readonly IUserService _userService;

        public OddsApiController(
            IOddsApiService oddsApiService,
            IEventService eventService,
            IBettingService bettingService,
            IUserService userService,
            ILogger<OddsApiController> logger) : base(logger)
        {
            _oddsApiService = oddsApiService;
            _eventService = eventService;
            _bettingService = bettingService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var sports = await _oddsApiService.GetSportsAsync();
            var apiUsage = await _oddsApiService.GetApiUsageAsync();

            var viewModel = new OddsApiDashboardViewModel
            {
                ActiveSports = sports.Where(s => s.Active).ToList(),
                ApiUsage = apiUsage,
                IsApiAvailable = await _oddsApiService.IsApiAvailableAsync()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Sports()
        {
            var sports = await _oddsApiService.GetSportsAsync();
            var activeSports = sports.Where(s => s.Active)
                .OrderBy(s => s.Group)
                .ThenBy(s => s.Title)
                .ToList();

            return View(activeSports);
        }

        [HttpGet]
        public async Task<IActionResult> Events(string sportKey, string? region = "us", string? market = "h2h")
        {
            if (string.IsNullOrEmpty(sportKey))
            {
                return RedirectToAction(nameof(Sports));
            }

            var sport = await _oddsApiService.GetSportAsync(sportKey);
            var events = await _oddsApiService.GetOddsAsync(sportKey, region, market);

            var viewModel = new SportEventsViewModel
            {
                Sport = sport,
                Events = events.OrderBy(e => e.CommenceTime).ToList(),
                Region = region,
                Market = market,
                SportKey = sportKey
            };

            ViewBag.Regions = new Dictionary<string, string>
            {
                ["us"] = "Estados Unidos",
                ["uk"] = "Reino Unido",
                ["eu"] = "Europa",
                ["au"] = "Australia"
            };

            ViewBag.Markets = new Dictionary<string, string>
            {
                ["h2h"] = "Ganador del Partido",
                ["spreads"] = "Handicap",
                ["totals"] = "Total de Puntos/Goles"
            };

            return View(viewModel);
        }

        [HttpGet]
        [HttpGet]
        public async Task<IActionResult> EventDetails(string sportKey, string eventId)
        {
            var demoEvents = CreateDemoEvents();
            var simulatedEvents = new List<EventApiModel>();

            if (sportKey == "basketball_nba" || sportKey == "baseball_mlb")
            {
                simulatedEvents = CreateSimulatedLiveEvents(
                    sportKey,
                    sportKey == "basketball_nba" ? "NBA" : "MLB"
                );
            }

            var localEvent = demoEvents.Concat(simulatedEvents)
                                       .FirstOrDefault(e => e.Id == eventId);

            if (localEvent != null)
            {
                return View(localEvent);
            }

            var eventData = await _oddsApiService.GetEventAsync(sportKey, eventId);

            if (eventData == null)
            {
                ViewBag.ErrorMessage = "El evento no existe o no está disponible.";
                return View(); 
            }

            return View(eventData);
        }


        private List<EventApiModel> CreateSimulatedLiveEvents(string sportKey, string sportTitle)
        {
            var random = new Random();
            var liveEvents = new List<EventApiModel>();

            if (sportKey == "baseball_mlb")
            {
                var mlbTeams = new[]
                {
                    ("New York Yankees", "Minnesota Twins"),
                    ("Los Angeles Dodgers", "San Francisco Giants"),
                    ("Boston Red Sox", "Tampa Bay Rays"),
                    ("Houston Astros", "Oakland Athletics"),
                    ("Chicago Cubs", "Milwaukee Brewers")
                };

                foreach (var (home, away) in mlbTeams.Take(3))
                {
                    var homeScore = random.Next(0, 8);
                    var awayScore = random.Next(0, 8);

                    liveEvents.Add(new EventApiModel
                    {
                        Id = $"sim_mlb_{Guid.NewGuid().ToString("N")[..8]}",
                        SportKey = sportKey,
                        SportTitle = sportTitle,
                        CommenceTime = DateTime.UtcNow.AddMinutes(-random.Next(30, 180)),
                        HomeTeam = home,
                        AwayTeam = away,
                        Completed = false,
                        Scores = new ScoreModel
                        {
                            Score = $"{home} {homeScore} - {away} {awayScore}",
                            Name = "Live Score"
                        },
                        Bookmakers = new List<BookmakerModel>
                        {
                            new BookmakerModel
                            {
                                Title = "Live Odds",
                                Markets = new List<MarketModel>
                                {
                                    new MarketModel
                                    {
                                        Key = "h2h",
                                        LastUpdate = DateTime.UtcNow,
                                        Outcomes = new List<OutcomeModel>
                                        {
                                            new OutcomeModel { Name = home, Price = Math.Round((decimal)(1.5 + random.NextDouble()), 2) },
                                            new OutcomeModel { Name = away, Price = Math.Round((decimal)(1.5 + random.NextDouble()), 2) }
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }
            else if (sportKey == "basketball_nba")
            {
                var nbaTeams = new[]
                {
                    ("Los Angeles Lakers", "Boston Celtics"),
                    ("Golden State Warriors", "Miami Heat"),
                    ("Chicago Bulls", "New York Knicks"),
                    ("Phoenix Suns", "Dallas Mavericks")
                };

                foreach (var (home, away) in nbaTeams.Take(2))
                {
                    var homeScore = random.Next(80, 130);
                    var awayScore = random.Next(80, 130);

                    liveEvents.Add(new EventApiModel
                    {
                        Id = $"sim_nba_{Guid.NewGuid().ToString("N")[..8]}",
                        SportKey = sportKey,
                        SportTitle = sportTitle,
                        CommenceTime = DateTime.UtcNow.AddMinutes(-random.Next(30, 120)),
                        HomeTeam = home,
                        AwayTeam = away,
                        Completed = false,
                        Scores = new ScoreModel
                        {
                            Score = $"{home} {homeScore} - {away} {awayScore}",
                            Name = "Live Score"
                        },
                        Bookmakers = new List<BookmakerModel>
                        {
                            new BookmakerModel
                            {
                                Title = "Live Odds",
                                Markets = new List<MarketModel>
                                {
                                    new MarketModel
                                    {
                                        Key = "h2h",
                                        LastUpdate = DateTime.UtcNow,
                                        Outcomes = new List<OutcomeModel>
                                        {
                                            new OutcomeModel { Name = home, Price = Math.Round((decimal)(1.7 + random.NextDouble() * 0.6), 2) },
                                            new OutcomeModel { Name = away, Price = Math.Round((decimal)(1.7 + random.NextDouble() * 0.6), 2) }
                                        }
                                    }
                                }
                            }
                        }
                    });
                }
            }

            return liveEvents;
        }

        private List<EventApiModel> CreateDemoEvents()
        {
            return new List<EventApiModel>
            {
                new EventApiModel
                {
                    Id = "demo_live_1",
                    SportKey = "baseball_mlb",
                    SportTitle = "MLB",
                    CommenceTime = DateTime.UtcNow.AddMinutes(-30),
                    HomeTeam = "New York Yankees",
                    AwayTeam = "Minnesota Twins",
                    Completed = false,
                    Scores = new ScoreModel
                    {
                        Score = "Yankees 4 - Twins 2",
                        Name = "Live Score"
                    },
                    Bookmakers = new List<BookmakerModel>
                    {
                        new BookmakerModel
                        {
                            Title = "Demo Bookmaker",
                            Markets = new List<MarketModel>
                            {
                                new MarketModel
                                {
                                    Key = "h2h",
                                    LastUpdate = DateTime.UtcNow,
                                    Outcomes = new List<OutcomeModel>
                                    {
                                        new OutcomeModel { Name = "New York Yankees", Price = 1.75m },
                                        new OutcomeModel { Name = "Minnesota Twins", Price = 2.10m }
                                    }
                                }
                            }
                        }
                    }
                },
                new EventApiModel
                {
                    Id = "demo_live_2",
                    SportKey = "basketball_nba",
                    SportTitle = "NBA",
                    CommenceTime = DateTime.UtcNow.AddMinutes(-45),
                    HomeTeam = "Los Angeles Lakers",
                    AwayTeam = "Boston Celtics",
                    Completed = false,
                    Scores = new ScoreModel
                    {
                        Score = "Lakers 98 - Celtics 94",
                        Name = "Live Score"
                    },
                    Bookmakers = new List<BookmakerModel>
                    {
                        new BookmakerModel
                        {
                            Title = "Demo Bookmaker",
                            Markets = new List<MarketModel>
                            {
                                new MarketModel
                                {
                                    Key = "h2h",
                                    LastUpdate = DateTime.UtcNow,
                                    Outcomes = new List<OutcomeModel>
                                    {
                                        new OutcomeModel { Name = "Los Angeles Lakers", Price = 1.85m },
                                        new OutcomeModel { Name = "Boston Celtics", Price = 1.95m }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }




        [HttpGet]
        public async Task<IActionResult> LiveEvents()
        {
            var liveEventsDict = new Dictionary<string, List<Models.API.EventApiModel>>();
            var sports = await _oddsApiService.GetSportsAsync();

            foreach (var sport in sports.Where(s => s.Active))
            {
                var liveEvents = await _oddsApiService.GetLiveEventsAsync(sport.Key);
                if (liveEvents.Any())
                {
                    liveEventsDict[sport.Title] = liveEvents;
                }
            }

            return View(liveEventsDict);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> CreateBetFromApi(string sportKey, string eventId, string teamName, decimal odds)
        {
            var apiEvent = await _oddsApiService.GetEventAsync(sportKey, eventId);
            if (apiEvent == null)
            {
                return NotFound();
            }

            var user = await _userService.GetCurrentUserAsync();

            var model = new CreateBetFromApiViewModel
            {
                ApiEventId = eventId,
                SportKey = sportKey,
                EventName = $"{apiEvent.HomeTeam} vs {apiEvent.AwayTeam}",
                EventDate = apiEvent.CommenceTime,
                TeamName = teamName,
                Odds = odds,
                UserBalance = user?.CreditBalance ?? 0
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBetFromApi(CreateBetFromApiViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var user = await _userService.GetCurrentUserAsync();
                model.UserBalance = user?.CreditBalance ?? 0;
                return View(model);
            }

            // Primero sincronizar el evento con la base de datos local
            await _oddsApiService.SyncEventsWithDatabaseAsync(model.SportKey);

            // Buscar el evento local
            var localEvent = await _eventService.GetEventByExternalIdAsync(model.ApiEventId);
            if (localEvent == null)
            {
                AddErrorMessage("No se pudo procesar la apuesta. Intente nuevamente.");
                return View(model);
            }

            // Crear la apuesta usando el sistema existente
            var betModel = new CreateBetViewModel
            {
                EventId = localEvent.EventId,
                TeamId = localEvent.Teams.First(t => t.TeamName == model.TeamName).TeamId,
                Stake = model.Stake,
                Odds = model.Odds
            };

            var result = await _bettingService.PlaceBetAsync(betModel);

            if (result.Success)
            {
                AddSuccessMessage("¡Apuesta realizada con éxito!");
                return RedirectToAction("Details", "Betting", new { id = result.BetId });
            }

            AddModelErrors(result.ErrorMessage ?? "Error al procesar la apuesta");
            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SyncSports()
        {
            var success = await _oddsApiService.SyncSportsWithDatabaseAsync();

            if (success)
            {
                return JsonSuccess("Deportes sincronizados exitosamente");
            }

            return JsonError("Error al sincronizar deportes");
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> SyncEvents(string sportKey)
        {
            if (string.IsNullOrEmpty(sportKey))
            {
                return JsonError("Debe especificar un deporte");
            }

            var success = await _oddsApiService.SyncEventsWithDatabaseAsync(sportKey);

            if (success)
            {
                return JsonSuccess("Eventos sincronizados exitosamente");
            }

            return JsonError("Error al sincronizar eventos");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOdds(int eventId, string apiEventId)
        {
            var success = await _oddsApiService.UpdateOddsForEventAsync(eventId, apiEventId);

            if (success)
            {
                return JsonSuccess("Cuotas actualizadas exitosamente");
            }

            return JsonError("Error al actualizar cuotas");
        }

        [HttpGet]
        public async Task<IActionResult> ApiStatus()
        {
            var usage = await _oddsApiService.GetApiUsageAsync();
            var isAvailable = await _oddsApiService.IsApiAvailableAsync();

            return PartialView("_ApiStatus", new ApiStatusViewModel
            {
                IsAvailable = isAvailable,
                RequestsUsed = usage.RequestsUsed,
                RequestsRemaining = usage.RequestsRemaining,
                UsagePercentage = usage.RequestsUsed > 0
                    ? (decimal)usage.RequestsUsed / (usage.RequestsUsed + usage.RequestsRemaining) * 100
                    : 0
            });
        }

        [HttpGet]
        public async Task<IActionResult> SearchEvents(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            {
                return PartialView("_EventSearchResults", new List<Models.API.OddsApiModel>());
            }

            var allEvents = new List<Models.API.OddsApiModel>();
            var sports = await _oddsApiService.GetSportsAsync();

            // Buscar en los deportes más populares
            var popularSports = new[] { "soccer_epl", "basketball_nba", "baseball_mlb", "americanfootball_nfl" };

            foreach (var sportKey in popularSports)
            {
                // GetOddsAsync devuelve List<OddsApiModel>, no List<EventApiModel>
                var odds = await _oddsApiService.GetOddsAsync(sportKey, "us", "h2h");
                var filtered = odds.Where(e =>
                    e.HomeTeam.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    e.AwayTeam.Contains(query, StringComparison.OrdinalIgnoreCase))
                    .Take(5);

                allEvents.AddRange(filtered);
            }

            return PartialView("_EventSearchResults", allEvents.Take(10).ToList());
        }
    }


}