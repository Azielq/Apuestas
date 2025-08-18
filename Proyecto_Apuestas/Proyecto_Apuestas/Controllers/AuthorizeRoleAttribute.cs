using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using System.Linq;
using System.Security.Claims;

namespace Proyecto_Apuestas.Attributes
{
    public class AuthorizeRoleAttribute : TypeFilterAttribute
    {
        public AuthorizeRoleAttribute(params string[] roles)
            : base(typeof(AuthorizeRoleFilter))
        {
            Arguments = new object[] { roles };
        }

        private class AuthorizeRoleFilter : IAuthorizationFilter
        {
            private readonly string[] _roles;
            private readonly ITempDataDictionaryFactory _tempDataFactory;

            public AuthorizeRoleFilter(string[] roles, ITempDataDictionaryFactory tempDataFactory)
            {
                _roles = roles;
                _tempDataFactory = tempDataFactory;
            }

            public void OnAuthorization(AuthorizationFilterContext context)
            {
                var user = context.HttpContext.User;
                var tempData = _tempDataFactory.GetTempData(context.HttpContext);

                if (!user.Identity.IsAuthenticated)
                {
                    tempData["ShowLoginModal"] = true;
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }

                var userRoleClaim = user.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userRoleClaim) || !_roles.Contains(userRoleClaim))
                {
                    tempData["AlertMessage"] = "Rol no válido para acceder a esta página.";
                    context.Result = new RedirectToActionResult("Index", "Home", null);
                    return;
                }
            }
        }
    }
}
