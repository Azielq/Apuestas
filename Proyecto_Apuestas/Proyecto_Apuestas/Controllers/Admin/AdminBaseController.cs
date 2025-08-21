using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Proyecto_Apuestas.Controllers.Admin
{
    public abstract class AdminBaseController : Controller
    {
        protected readonly ILogger<AdminBaseController> _logger;

        protected AdminBaseController(ILogger<AdminBaseController> logger)
        {
            _logger = logger;
        }

        protected void AddSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        protected void AddErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }
    }
}