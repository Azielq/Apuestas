using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Models.API;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels.API;
using System.Diagnostics;

namespace Proyecto_Apuestas.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IEventService _eventService;
        private readonly IBettingService _bettingService;
        private readonly IUserService _userService;
        private readonly IOddsApiService _oddsApiService;

        public HomeController(
            IEventService eventService,
            IBettingService bettingService,
            IUserService userService,
            IOddsApiService oddsApiService,
            ILogger<HomeController> logger) : base(logger)
        {
            _eventService = eventService;
            _bettingService = bettingService;
            _userService = userService;
            _oddsApiService = oddsApiService;
        }

        public async Task<IActionResult> LandingPage()
        {
            var sports = await _oddsApiService.GetSportsAsync();
            var activeSports = sports
                .Where(s => s.Active &&
                           (s.Key.Contains("soccer") || s.Key.Contains("basketball") || s.Key.Contains("tennis")))
                .ToList();
            var dashboardData = new OddsApiDashboardViewModel
            {
                ActiveSports = activeSports,
                ApiUsage = await _oddsApiService.GetApiUsageAsync(),
                IsApiAvailable = await _oddsApiService.IsApiAvailableAsync()
            };
            var sportEventsDict = new Dictionary<string, List<EventApiModel>>();

            foreach (var sport in activeSports)
            {
                var oddsEvents = await _oddsApiService.GetOddsAsync(sport.Key, "us", "h2h");

                var mappedEvents = oddsEvents.Select(o => new EventApiModel
                {
                    Id = o.Id,
                    SportKey = o.SportKey,
                    SportTitle = o.SportTitle,
                    CommenceTime = o.CommenceTime,
                    HomeTeam = o.HomeTeam,
                    AwayTeam = o.AwayTeam,
                    Completed = false,
                    Bookmakers = o.Bookmakers,
                    Scores = null 
                })
                .OrderBy(e => e.CommenceTime)
                .Take(5) // mostrar máximo 5 eventos por deporte
                .ToList();

                sportEventsDict[sport.Key] = mappedEvents;
            }

            ViewBag.SportEvents = sportEventsDict;

            return View(dashboardData);
        }



        public IActionResult Privacy() => View();

        public IActionResult About() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string name, string email, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                AddErrorMessage("Todos los campos son requeridos");
                return View();
            }

            AddSuccessMessage("Tu mensaje ha sido enviado. Te contactaremos pronto.");
            return RedirectToAction(nameof(LandingPage));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
