using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using System.Diagnostics;

namespace Proyecto_Apuestas.Controllers
{
    public class HomeController : BaseController
    {
        private readonly IEventService _eventService;
        private readonly IBettingService _bettingService;
        private readonly IUserService _userService;

        public HomeController(
            IEventService eventService,
            IBettingService bettingService,
            IUserService userService,
            ILogger<HomeController> logger) : base(logger)
        {
            _eventService = eventService;
            _bettingService = bettingService;
            _userService = userService;
        }

        public async Task<IActionResult> Index()
        {
            var upcomingEvents = await _eventService.GetUpcomingEventsByCategoryAsync();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userService.GetCurrentUserId();
                var activeBets = await _bettingService.GetActiveBetsByUserAsync(userId);
                ViewBag.ActiveBetsCount = activeBets.Count;

                var user = await _userService.GetCurrentUserAsync();
                ViewBag.UserBalance = user?.CreditBalance ?? 0;
            }

            return View(upcomingEvents);
        }
        public async Task<IActionResult> LandingPage()
        {
            var upcomingEvents = await _eventService.GetUpcomingEventsByCategoryAsync();

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userService.GetCurrentUserId();
                var activeBets = await _bettingService.GetActiveBetsByUserAsync(userId);
                ViewBag.ActiveBetsCount = activeBets.Count;

                var user = await _userService.GetCurrentUserAsync();
                ViewBag.UserBalance = user?.CreditBalance ?? 0;
            }

            return View(upcomingEvents);
        }
        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Contact(string name, string email, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                AddErrorMessage("Todos los campos son requeridos");
                return View();
            }

            // Aquí enviarías el email
            AddSuccessMessage("Tu mensaje ha sido enviado. Te contactaremos pronto.");
            return RedirectToAction(nameof(Index));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}