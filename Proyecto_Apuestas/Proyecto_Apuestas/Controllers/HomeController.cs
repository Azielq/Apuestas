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
            // Esta es la p�gina principal real que se muestra en la ruta "/"
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

            // Aqu� enviar�as el email
            AddSuccessMessage("Tu mensaje ha sido enviado. Te contactaremos pronto.");
            return RedirectToAction(nameof(Index));
        }

        // Nuevas acciones para las rutas que faltan
        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult ResponsibleGaming()
        {
            return View();
        }

        public IActionResult Blog()
        {
            ViewBag.FeatureName = "Blog de Bet506";
            ViewBag.ExpectedDate = "Q4 2025";
            ViewBag.Description = "Nuestro blog traer� las �ltimas noticias deportivas, an�lisis de apuestas, consejos de expertos y mucho m�s.";
            ViewBag.Icon = "bi-newspaper";
            ViewBag.ShowNewsletterSignup = true;
            ViewBag.Features = new List<string>
            {
                "Noticias deportivas actualizadas",
                "An�lisis de expertos",
                "Consejos de apuestas",
                "Pron�sticos deportivos",
                "Entrevistas exclusivas",
                "Estad�sticas avanzadas"
            };
            
            return View("~/Views/Shared/_UnderDevelopment.cshtml");
        }

        public IActionResult Careers()
        {
            ViewBag.FeatureName = "Oportunidades de Carrera";
            ViewBag.ExpectedDate = "Q4 2025";
            ViewBag.Description = "�nete a nuestro equipo. Estamos construyendo una secci�n completa de empleos para crecer juntos.";
            ViewBag.Icon = "bi-briefcase";
            ViewBag.ShowNewsletterSignup = true;
            ViewBag.Features = new List<string>
            {
                "Posiciones en tecnolog�a",
                "Roles en atenci�n al cliente",
                "Oportunidades en marketing",
                "Trabajos remotos disponibles",
                "Beneficios competitivos",
                "Ambiente de trabajo inclusivo"
            };
            
            return View("~/Views/Shared/_UnderDevelopment.cshtml");
        }

        public IActionResult Promotions()
        {
            return View();
        }

        public IActionResult FAQ()
        {
            ViewBag.FeatureName = "Preguntas Frecuentes";
            ViewBag.ExpectedDate = "Q4 2025";
            ViewBag.Description = "Estamos compilando las preguntas m�s comunes para ofrecerte respuestas r�pidas y precisas.";
            ViewBag.Icon = "bi-question-circle";
            ViewBag.ShowNewsletterSignup = false;
            ViewBag.Features = new List<string>
            {
                "Gu�as de registro y verificaci�n",
                "Informaci�n sobre m�todos de pago",
                "Explicaci�n de tipos de apuestas",
                "Pol�ticas de retiros",
                "Soporte t�cnico com�n",
                "B�squeda inteligente de respuestas"
            };
            
            return View("~/Views/Shared/_UnderDevelopment.cshtml");
        }

        public IActionResult AppStore()
        {
            ViewBag.FeatureName = "App para iOS";
            ViewBag.ExpectedDate = "Q1 2026";
            ViewBag.Description = "Nuestra aplicaci�n m�vil para iOS est� en desarrollo. Pronto podr�s apostar desde tu iPhone o iPad.";
            ViewBag.Icon = "bi-apple";
            ViewBag.ShowNewsletterSignup = true;
            ViewBag.Features = new List<string>
            {
                "Interfaz nativa optimizada",
                "Notificaciones push en vivo",
                "Apuestas r�pidas con Touch ID",
                "Streaming de eventos",
                "Modo offline b�sico",
                "Integraci�n con Apple Pay"
            };
            
            return View("~/Views/Shared/_UnderDevelopment.cshtml");
        }

        public IActionResult PlayStore()
        {
            ViewBag.FeatureName = "App para Android";
            ViewBag.ExpectedDate = "Q4 2025";
            ViewBag.Description = "Estamos desarrollando la aplicaci�n para Android con todas las funcionalidades que necesitas.";
            ViewBag.Icon = "bi-google-play";
            ViewBag.ShowNewsletterSignup = true;
            ViewBag.Features = new List<string>
            {
                "Material Design 3",
                "Widgets para pantalla principal",
                "Notificaciones personalizables",
                "Modo oscuro autom�tico",
                "Soporte para tablets",
                "Integraci�n con Google Pay"
            };
            
            return View("~/Views/Shared/_UnderDevelopment.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}