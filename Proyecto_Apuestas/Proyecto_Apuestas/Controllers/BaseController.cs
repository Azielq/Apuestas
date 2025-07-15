using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Proyecto_Apuestas.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ILogger<BaseController> _logger;

        protected BaseController(ILogger<BaseController> logger)
        {
            _logger = logger;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            //  NOTE: Esto nos verifica si el usuario está autenticado para ciertas acciones
            if (User.Identity?.IsAuthenticated == true)
            {
                ViewBag.UserName = User.Identity.Name;
                ViewBag.IsAuthenticated = true;
            }
            else
            {
                ViewBag.IsAuthenticated = false;
            }

            base.OnActionExecuting(context);
        }

        protected IActionResult JsonSuccess(object? data = null, string? message = null)
        {
            return Json(new
            {
                success = true,
                message = message,
                data = data
            });
        }

        protected IActionResult JsonError(string message, object? errors = null)
        {
            return Json(new
            {
                success = false,
                message = message,
                errors = errors
            });
        }

        protected void AddSuccessMessage(string message)
        {
            TempData["SuccessMessage"] = message;
        }

        protected void AddErrorMessage(string message)
        {
            TempData["ErrorMessage"] = message;
        }

        protected void AddModelErrors(string errorMessage)
        {
            ModelState.AddModelError(string.Empty, errorMessage);
        }
    }
}