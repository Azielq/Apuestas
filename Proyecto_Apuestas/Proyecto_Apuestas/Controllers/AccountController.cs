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

        #region Authentication Actions

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
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }
                return View(model);
            }

            try
            {
                // Verifica intentos fallidos
                var failedAttempts = await _userService.GetFailedLoginAttemptsAsync(model.EmailOrUsername, TimeSpan.FromHours(1));
                if (failedAttempts >= 5)
                {
                    var rateLimitMessage = "Demasiados intentos fallidos. Por favor intenta más tarde.";
                    if (isAjax)
                    {
                        return Json(new { success = false, message = rateLimitMessage });
                    }
                    AddModelErrors(rateLimitMessage);
                    return View(model);
                }

                // Busca usuario por email o username
                var user = await _userService.GetUserByEmailOrUsernameAsync(model.EmailOrUsername);
                if (user == null)
                {
                    await _userService.RecordLoginAttemptAsync(model.EmailOrUsername, false);
                    var notFoundMessage = "Email/Usuario o contraseña incorrectos";
                    if (isAjax)
                    {
                        return Json(new { success = false, message = notFoundMessage });
                    }
                    AddModelErrors(notFoundMessage);
                    return View(model);
                }

                // Verifica si la cuenta está bloqueada
                if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.Now)
                {
                    var lockedMessage = $"Tu cuenta está bloqueada hasta {user.LockedUntil.Value:dd/MM/yyyy HH:mm}";
                    if (isAjax)
                    {
                        return Json(new { success = false, message = lockedMessage });
                    }
                    AddModelErrors(lockedMessage);
                    return View(model);
                }

                // 🔐 SISTEMA DE MIGRACIÓN DE CONTRASEÑAS
                bool passwordIsValid = false;
                bool needsMigration = false;

                // Log para debugging
                _logger.LogInformation("Login attempt for user {EmailOrUsername}. PasswordHash length: {HashLength}, First 10 chars: {HashStart}", 
                    model.EmailOrUsername, 
                    user.PasswordHash?.Length ?? 0, 
                    user.PasswordHash?.Length > 10 ? user.PasswordHash.Substring(0, 10) : user.PasswordHash);

                // Detecta si la contraseña está en texto plano
                bool isPlainText = IsPlainTextPassword(user.PasswordHash);
                _logger.LogInformation("IsPlainTextPassword result for user {EmailOrUsername}: {IsPlainText}", model.EmailOrUsername, isPlainText);

                if (isPlainText)
                {
                    // Verificación directa para contraseñas en texto plano
                    if (user.PasswordHash == model.Password)
                    {
                        passwordIsValid = true;
                        needsMigration = true;
                        _logger.LogInformation("Plain text password verified successfully for user {EmailOrUsername}, will be migrated to hash", model.EmailOrUsername);
                    }
                    else
                    {
                        _logger.LogInformation("Plain text password verification failed for user {EmailOrUsername}", model.EmailOrUsername);
                    }
                }
                else
                {
                    // Verificación normal con hash
                    _logger.LogInformation("Attempting hash verification for user {EmailOrUsername}", model.EmailOrUsername);
                    var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
                    passwordIsValid = result != PasswordVerificationResult.Failed;
                    _logger.LogInformation("Hash verification result for user {EmailOrUsername}: {Result}", model.EmailOrUsername, result);
                }

                if (!passwordIsValid)
                {
                    await _userService.RecordLoginAttemptAsync(model.EmailOrUsername, false);
                    var wrongPasswordMessage = "Email/Usuario o contraseña incorrectos";
                    if (isAjax)
                    {
                        return Json(new { success = false, message = wrongPasswordMessage });
                    }
                    AddModelErrors(wrongPasswordMessage);
                    return View(model);
                }

                // 🔄 MIGRA CONTRASEÑA DE TEXTO PLANO A HASH
                if (needsMigration)
                {
                    using var migrationTransaction = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        // Hashea la contraseña y actualizarla
                        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);
                        user.UpdatedAt = DateTime.Now;
                        
                        _context.UserAccounts.Update(user);
                        await _context.SaveChangesAsync();
                        await migrationTransaction.CommitAsync();
                        
                        _logger.LogInformation("Password successfully migrated from plain text to hash for user {EmailOrUsername}", model.EmailOrUsername);
                        
                        // Opcional: Envia notificación de seguridad
                        await _notificationService.SendNotificationAsync(
                            user.UserId, 
                            "Tu contraseña ha sido actualizada por seguridad durante el login.",
                            "security"
                        );
                    }
                    catch (Exception migrationEx)
                    {
                        await migrationTransaction.RollbackAsync();
                        _logger.LogError(migrationEx, "Error migrating password for user {EmailOrUsername}", model.EmailOrUsername);
                        
                        // Continua con el login aunque falle la migración
                        // La próxima vez se intentará migrar de nuevo
                    }
                }

                // Login exitoso
                await _userService.RecordLoginAttemptAsync(model.EmailOrUsername, true);
                await SignInAsync(user, model.RememberMe);

                var redirectUrl = !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) 
                    ? model.ReturnUrl 
                    : Url.Action("Index", "Home");

                if (isAjax)
                {
                    return Json(new { success = true, message = "Login exitoso", data = new { redirectUrl } });
                }
                return Redirect(redirectUrl ?? "/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {EmailOrUsername}", model.EmailOrUsername);
                var errorMessage = "Ocurrió un error durante el login. Por favor intenta nuevamente.";
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMessage });
                }
                AddModelErrors(errorMessage);
                return View(model);
            }
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
            var isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

            if (!ModelState.IsValid)
            {
                if (isAjax)
                {
                    return Json(new { success = false, message = "Datos inválidos" });
                }
                return View(model);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Verifica si el email ya existe
                var existingUser = await _userService.GetUserByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    var emailExistsMessage = "El email ya está registrado";
                    if (isAjax)
                    {
                        return Json(new { success = false, message = emailExistsMessage });
                    }
                    AddModelErrors(emailExistsMessage);
                    return View(model);
                }

                // Verifica si el username ya existe
                var existingUsername = await _userService.GetUserByUsernameAsync(model.UserName);
                if (existingUsername != null)
                {
                    var usernameExistsMessage = "El nombre de usuario ya está registrado";
                    if (isAjax)
                    {
                        return Json(new { success = false, message = usernameExistsMessage });
                    }
                    AddModelErrors(usernameExistsMessage);
                    return View(model);
                }

                // Obtiene rol por defecto para crearlo con el rol por defecto, en este caso (User)
                var defaultRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
                if (defaultRole == null)
                {
                    var configErrorMessage = "Error en la configuración del sistema";
                    if (isAjax)
                    {
                        return Json(new { success = false, message = configErrorMessage });
                    }
                    AddModelErrors(configErrorMessage);
                    return View(model);
                }

                // Crea usuario
                var user = new UserAccount
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    PasswordHash = _passwordHasher.HashPassword(null!, model.Password),
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

                // Envia notificaciones
                await _notificationService.SendWelcomeNotificationAsync(user.UserId);
                await _emailService.SendWelcomeEmailAsync(user.Email, user.UserName);

                await transaction.CommitAsync();

                // Auto login
                await SignInAsync(user, false);

                var redirectUrl = Url.Action("Index", "Home");
                var successMessage = "¡Registro exitoso! Bienvenido a Bet506.";

                if (isAjax)
                {
                    return Json(new { success = true, message = successMessage, data = new { redirectUrl } });
                }
                AddSuccessMessage(successMessage);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during user registration");
                var errorMessage = "Ocurrió un error durante el registro. Por favor intenta nuevamente.";
                if (isAjax)
                {
                    return Json(new { success = false, message = errorMessage });
                }
                AddModelErrors(errorMessage);
                return View(model);
            }
        }

        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Profile Management

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

        #endregion

        #region Password Management

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
                // Generar token de restablecimiento
                var resetToken = Guid.NewGuid().ToString();

                // Aquí se guardaría el token en la base de datos
                // y se enviaría el email
                await _emailService.SendPasswordResetEmailAsync(email, resetToken);
            }

            // Siempre muestra mensaje de éxito por seguridad
            AddSuccessMessage("Si el email existe en nuestro sistema, recibirás instrucciones para restablecer tu contraseña.");
            return RedirectToAction(nameof(Login));
        }

        #endregion

        #region Access Control

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        #endregion

        #region Debug and Testing Methods (Remove in Production)

        /// <summary>
        /// Método temporal para debugging de contraseñas - REMOVER EN PRODUCCIÓN
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DebugPassword(string emailOrUsername, string password)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            try
            {
                var user = await _userService.GetUserByEmailOrUsernameAsync(emailOrUsername);
                if (user == null)
                {
                    return Json(new { success = false, message = "Usuario no encontrado" });
                }

                var debugInfo = new
                {
                    UserFound = true,
                    PasswordHashLength = user.PasswordHash?.Length ?? 0,
                    PasswordHashStart = user.PasswordHash?.Length > 20 ? user.PasswordHash.Substring(0, 20) + "..." : user.PasswordHash,
                    IsPlainTextDetected = IsPlainTextPassword(user.PasswordHash),
                    PlainTextMatches = user.PasswordHash == password,
                    HashVerificationResult = user.PasswordHash != null ? _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, password).ToString() : "N/A",
                    HashContainsPlus = user.PasswordHash?.Contains('+') ?? false,
                    HashContainsSlash = user.PasswordHash?.Contains('/') ?? false,
                    HashContainsEquals = user.PasswordHash?.Contains('=') ?? false,
                    HashStartsWithAQ = user.PasswordHash?.StartsWith("AQ") ?? false,
                    InputPasswordLength = password?.Length ?? 0
                };

                return Json(new { success = true, debugInfo });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Detecta si una contraseña está almacenada en texto plano
        /// </summary>
        /// <param name="passwordHash">El hash/contraseña almacenada</param>
        /// <returns>True si es texto plano, False si es hash</returns>
        private bool IsPlainTextPassword(string passwordHash)
        {
            if (string.IsNullOrEmpty(passwordHash))
                return false;

            // Los hashes de ASP.NET Core Identity tienen características específicas:
            // - Longitud considerable (típicamente 84+ caracteres)
            // - Empiezan con identificadores específicos del algoritmo
            // - Contienen caracteres de Base64 como +, /, =
            
            // CRITERIO PRINCIPAL: Longitud
            // Los hashes de Identity son muy largos (84+ chars), contraseñas humanas son más cortas
            if (passwordHash.Length < 50)
            {
                // Si es muy corto, probablemente es texto plano
                return true;
            }
            
            // CRITERIO SECUNDARIO: Patrones específicos de ASP.NET Core Identity
            // Los hashes de Identity típicamente empiezan con "AQ" o tienen patrones específicos
            if (passwordHash.StartsWith("AQ") && passwordHash.Length > 80)
            {
                // Definitivamente es un hash de Identity
                return false;
            }
            
            // CRITERIO TERCIARIO: Caracteres típicos de Base64
            bool hasBase64Chars = passwordHash.Contains('+') || 
                                 passwordHash.Contains('/') || 
                                 passwordHash.Contains('=');
            
            // CRITERIO CUARTO: Solo caracteres alfanuméricos (típico de contraseñas humanas)
            bool isOnlyAlphanumeric = passwordHash.All(c => char.IsLetterOrDigit(c));
            
            // Si es solo alfanumérico Y corto, muy probablemente es texto plano
            if (isOnlyAlphanumeric && passwordHash.Length < 80)
            {
                return true;
            }
            
            // Si es largo Y tiene caracteres Base64, probablemente es hash
            if (passwordHash.Length >= 80 && hasBase64Chars)
            {
                return false;
            }
            
            // Si llegamos aquí y es muy largo, asumir que es hash
            if (passwordHash.Length >= 80)
            {
                return false;
            }
            
            // Por defecto, si es medianamente largo pero no tiene características de hash claras,
            // asumir texto plano para ser conservadores
            return true;
        }

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

        #endregion
    }
}