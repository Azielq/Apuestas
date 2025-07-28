using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using System.Security.Claims;

namespace Proyecto_Apuestas.Controllers
{
    public class AccountController : BaseController
    {
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IPasswordHasher<UserAccount> _passwordHasher;
        private readonly apuestasDbContext _context;

        public AccountController(
            IUserService userService,
            INotificationService notificationService,
            IEmailService emailService,
            IPasswordHasher<UserAccount> passwordHasher,
            apuestasDbContext context,
            ILogger<AccountController> logger) : base(logger)
        {
            _userService = userService;
            _notificationService = notificationService;
            _emailService = emailService;
            _passwordHasher = passwordHasher;
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([FromBody] LoginViewModel model)
        {
            // Validar manualmente el modelo
            if (model == null || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { success = false, message = "Por favor ingresa email y contraseña." });
            }

            // Verificar intentos fallidos
            var failedAttempts = await _userService.GetFailedLoginAttemptsAsync(model.Email, TimeSpan.FromHours(1));
            if (failedAttempts >= 5)
            {
                return Unauthorized(new { success = false, message = "Demasiados intentos fallidos. Por favor intenta más tarde." });
            }

            var user = await _userService.GetUserByEmailAsync(model.Email);
            if (user == null)
            {
                await _userService.RecordLoginAttemptAsync(model.Email, false);
                return Unauthorized(new { success = false, message = "Email o contraseña incorrectos." });
            }

            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.Now)
            {
                return Unauthorized(new { success = false, message = $"Tu cuenta está bloqueada hasta {user.LockedUntil.Value:dd/MM/yyyy HH:mm}." });
            }

            var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
            if (result == PasswordVerificationResult.Failed)
            {
                await _userService.RecordLoginAttemptAsync(model.Email, false);
                return Unauthorized(new { success = false, message = "Email o contraseña incorrectos." });
            }

            // Login exitoso
            await _userService.RecordLoginAttemptAsync(model.Email, true);
            await SignInAsync(user, model.RememberMe);

            // Retornar éxito como alertaa
            return Ok(new { success = true, message = "Inicio de sesión exitoso." });
        }


        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Obtiene rol por defecto
                var defaultRole = await _context.Roles.FindAsync(2);//lo cambie a 2 porque el rol de usuario es el segundo en la tabla Roles, no hay algun roll default en la tabla Roles
                if (defaultRole == null)
                {
                    AddModelErrors("Error en la configuración del sistema");
                    return View(model);
                }

                var user = new UserAccount
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PasswordHash = _passwordHasher.HashPassword(null, model.Password),
                    FirstName = model.FirstName,
                    PrimerApellido = model.PrimerApellido,
                    SegundoApellido = model.SegundoApellido,
                    PhoneNumber = model.PhoneNumber,
                    Country = model.Country,
                    BirthDate = model.BirthDate,
                    RoleId = defaultRole.RoleId,
                    CreditBalance = 0,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.UserAccounts.Add(user);
                await _context.SaveChangesAsync();

                // Envía notificaciones
                await _notificationService.SendWelcomeNotificationAsync(user.UserId);
                await _emailService.SendWelcomeEmailAsync(user.Email, user.UserName);

                await transaction.CommitAsync();

                // Auto login
                await SignInAsync(user, false);

                AddSuccessMessage("¡Registro exitoso! Bienvenido a Proyecto Apuestas.");
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during user registration");
                AddModelErrors("Ocurrió un error durante el registro. Por favor intenta nuevamente.");
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var userId = _userService.GetCurrentUserId();
            var profile = await _userService.GetUserProfileAsync(userId);

            if (profile == null)
            {
                return NotFound();
            }

            return View(profile);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            var user = await _userService.GetCurrentUserAsync();
            if (user == null)
            {
                return NotFound();
            }

            var model = new UpdateProfileViewModel
            {
                FirstName = user.FirstName,
                PrimerApellido = user.PrimerApellido,
                SegundoApellido = user.SegundoApellido,
                PhoneNumber = user.PhoneNumber,
                Country = user.Country
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userService.GetCurrentUserId();
            var success = await _userService.UpdateUserProfileAsync(userId, model);

            if (success)
            {
                AddSuccessMessage("Perfil actualizado exitosamente");
                return RedirectToAction(nameof(Profile));
            }

            AddErrorMessage("Error al actualizar el perfil");
            return View(model);
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userService.GetCurrentUserId();
            var success = await _userService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);

            if (success)
            {
                AddSuccessMessage("Contraseña cambiada exitosamente");
                return RedirectToAction(nameof(Profile));
            }

            AddModelErrors("La contraseña actual es incorrecta");
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                AddModelErrors("El email es requerido");
                return View();
            }

            var user = await _userService.GetUserByEmailAsync(email);
            if (user != null)
            {
                // NOTE: Esto nos genera el token de restablecimiento
                var resetToken = Guid.NewGuid().ToString();

                // Aquí se guardaría el token en la base de datos
                // y se enviaría el email
                await _emailService.SendPasswordResetEmailAsync(email, resetToken);
            }

            // Siempre muestra mensaje de éxito por seguridad
            AddSuccessMessage("Si el email existe en nuestro sistema, recibirás instrucciones para restablecer tu contraseña.");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        // Métodos privados
        private async Task SignInAsync(UserAccount user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.RoleName)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(isPersistent ? 30 : 1)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }
    }
}