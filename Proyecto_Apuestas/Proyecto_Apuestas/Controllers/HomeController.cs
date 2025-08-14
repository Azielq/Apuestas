using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Models.API;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels.API;
using System.Diagnostics;

namespace Proyecto_Apuestas.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IOddsApiService _oddsApiService;

        public HomeController(ILogger<HomeController> logger, IOddsApiService oddsApiService)
        {
            _logger = logger;
            _oddsApiService = oddsApiService;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> LandingPage()
        {
            var sports = await _oddsApiService.GetSportsAsync();

            // Incluir baseball y más deportes populares
            var activeSports = sports
                .Where(s => s.Active &&
                            (s.Key.Contains("soccer") ||
                             s.Key.Contains("basketball") ||
                             s.Key.Contains("tennis") ||
                             s.Key.Contains("baseball") ||
                             s.Key.Contains("americanfootball") ||
                             s.Key == "baseball_mlb" ||
                             s.Key == "basketball_nba" ||
                             s.Key == "americanfootball_nfl"))
                .ToList();

            var dashboardData = new OddsApiDashboardViewModel
            {
                ActiveSports = activeSports,
                ApiUsage = await _oddsApiService.GetApiUsageAsync(),
                IsApiAvailable = await _oddsApiService.IsApiAvailableAsync()
            };

            var sportEventsDict = new Dictionary<string, List<EventApiModel>>();
            var liveEventsDict = new Dictionary<string, List<EventApiModel>>();
            var demoEvents = new List<EventApiModel>();

            foreach (var sport in activeSports)
            {
                try
                {
                    // Obtener eventos próximos
                    var upcomingEvents = await _oddsApiService.GetOddsAsync(sport.Key, "us", "h2h");
                    var liveEvents = await _oddsApiService.GetLiveEventsAsync(sport.Key);
                    var liveOdds = await _oddsApiService.GetLiveOddsAsync(sport.Key);

                    // Procesar eventos en vivo
                    var currentLiveEvents = new List<EventApiModel>();

                    // Agregar eventos del método GetLiveEventsAsync 
                    if (liveEvents != null && liveEvents.Any())
                    {
                        currentLiveEvents.AddRange(liveEvents
                            .Where(e => !e.Completed)
                            .Take(5));
                    }

                    // Agregar eventos del método GetLiveOddsAsync
                    if (liveOdds != null && liveOdds.Any())
                    {
                        var liveEventsFromOdds = liveOdds
                            .Where(o => o.CommenceTime <= DateTime.UtcNow.AddHours(1) &&
                                       o.CommenceTime >= DateTime.UtcNow.AddHours(-4))
                            .Select(o => new EventApiModel
                            {
                                Id = o.Id,
                                SportKey = o.SportKey,
                                SportTitle = o.SportTitle,
                                CommenceTime = o.CommenceTime,
                                HomeTeam = o.HomeTeam,
                                AwayTeam = o.AwayTeam,
                                Completed = false,
                                Bookmakers = o.Bookmakers ?? new List<BookmakerModel>(),
                                Scores = null 
                            })
                            .Take(5)
                            .ToList();

                        currentLiveEvents.AddRange(liveEventsFromOdds);
                    }

                    // Si no hay eventos en vivo reales, simular algunos para el test de stripe
                    if (!currentLiveEvents.Any() && (sport.Key == "baseball_mlb" || sport.Key == "basketball_nba"))
                    {
                        currentLiveEvents = CreateSimulatedLiveEvents(sport.Key, sport.Title);
                    }

                    if (currentLiveEvents.Any())
                    {
                        liveEventsDict[sport.Key] = currentLiveEvents
                            .GroupBy(e => e.Id)
                            .Select(g => g.First())
                            .OrderBy(e => e.CommenceTime)
                            .Take(5)
                            .ToList();
                    }

                    // Procesar eventos próximos
                    if (upcomingEvents != null && upcomingEvents.Any())
                    {
                        var now = DateTime.UtcNow;
                        var next24Hours = now.AddHours(24);

                        var futureEvents = upcomingEvents
                            .Where(o => o.CommenceTime > now && o.CommenceTime <= next24Hours)
                            .Select(o => new EventApiModel
                            {
                                Id = o.Id,
                                SportKey = o.SportKey,
                                SportTitle = o.SportTitle,
                                CommenceTime = o.CommenceTime,
                                HomeTeam = o.HomeTeam,
                                AwayTeam = o.AwayTeam,
                                Completed = false,
                                Bookmakers = o.Bookmakers ?? new List<BookmakerModel>(),
                                Scores = null
                            })
                            .OrderBy(e => e.CommenceTime)
                            .Take(5)
                            .ToList();

                        if (futureEvents.Any())
                        {
                            sportEventsDict[sport.Key] = futureEvents;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error obteniendo eventos para {sport.Key}: {ex.Message}");

                    if (sport.Key == "baseball_mlb" || sport.Key == "basketball_nba")
                    {
                        liveEventsDict[sport.Key] = CreateSimulatedLiveEvents(sport.Key, sport.Title);
                    }
                }
            }

            // Crear demo solo si no hay eventos reales
            if (!sportEventsDict.Any() && !liveEventsDict.Any())
            {
                demoEvents = CreateDemoEvents();
            }

            ViewBag.SportEvents = sportEventsDict;
            ViewBag.LiveEvents = liveEventsDict;
            ViewBag.DemoEvents = demoEvents;

            return View(dashboardData);
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
                },
                new EventApiModel
                {
                    Id = "demo_upcoming_1",
                    SportKey = "soccer_epl",
                    SportTitle = "Premier League",
                    CommenceTime = DateTime.UtcNow.AddHours(2),
                    HomeTeam = "Manchester United",
                    AwayTeam = "Liverpool",
                    Completed = false,
                    Scores = null,
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
                                        new OutcomeModel { Name = "Manchester United", Price = 2.20m },
                                        new OutcomeModel { Name = "Liverpool", Price = 1.80m },
                                        new OutcomeModel { Name = "Draw", Price = 3.40m }
                                    }
                                }
                            }
                        }
                    }
                },
                new EventApiModel
                {
                    Id = "demo_upcoming_2",
                    SportKey = "americanfootball_nfl",
                    SportTitle = "NFL",
                    CommenceTime = DateTime.UtcNow.AddHours(4),
                    HomeTeam = "Green Bay Packers",
                    AwayTeam = "Chicago Bears",
                    Completed = false,
                    Scores = null,
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
                                        new OutcomeModel { Name = "Green Bay Packers", Price = 1.65m },
                                        new OutcomeModel { Name = "Chicago Bears", Price = 2.30m }
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

    }
}
