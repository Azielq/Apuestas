using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services.Interfaces;

namespace Proyecto_Apuestas.ViewComponents
{
    public class UserBalanceViewComponent : ViewComponent
    {
        private readonly IUserService _userService;

        public UserBalanceViewComponent(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Content("₡0");
            }

            try
            {
                var user = await _userService.GetCurrentUserAsync();
                if (user == null)
                {
                    return Content("₡0");
                }

                // Use the same formatting as in the profile
                return Content(user.CreditBalance.ToString("C").Replace("$", "₡"));
            }
            catch
            {
                return Content("₡0");
            }
        }
    }
}