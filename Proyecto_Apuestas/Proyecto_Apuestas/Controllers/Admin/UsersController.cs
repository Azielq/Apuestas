using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Proyecto_Apuestas.Controllers.Admin
{
    public class UsersController : AdminBaseController
    {
        private readonly apuestasDbContext _context;
        private readonly IUserService _userService;
        private readonly INotificationService _notificationService;

        public UsersController(
            apuestasDbContext context,
            IUserService userService,
            INotificationService notificationService,
            ILogger<UsersController> logger) : base(logger)
        {
            _context = context;
            _userService = userService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(UserManagementViewModel model)
        {
            var query = _context.UserAccounts
                .Include(u => u.Role)
                .Include(u => u.Bets)
                .AsQueryable();

            // NOTE: Esto nos aplica filtros
            if (!string.IsNullOrEmpty(model.SearchTerm))
            {
                query = query.Where(u => u.UserName.Contains(model.SearchTerm) ||
                                        u.Email.Contains(model.SearchTerm));
            }

            if (model.RoleId.HasValue)
            {
                query = query.Where(u => u.RoleId == model.RoleId.Value);
            }

            if (model.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == model.IsActive.Value);
            }

            //  NOTE: Esto nos aplica ordenamiento
            query = model.SortBy switch
            {
                "name" => query.OrderBy(u => u.UserName),
                "email" => query.OrderBy(u => u.Email),
                "balance" => query.OrderByDescending(u => u.CreditBalance),
                "lastbet" => query.OrderByDescending(u => u.LastBet),
                _ => query.OrderByDescending(u => u.CreatedAt)
            };

            var totalUsers = await query.CountAsync();
            var pageSize = model.PageSize > 0 ? model.PageSize : 20;
            var currentPage = model.CurrentPage > 0 ? model.CurrentPage : 1;

            var users = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserAdminViewModel
                {
                    UserId = u.UserId,
                    UserName = u.UserName,
                    Email = u.Email,
                    RoleName = u.Role.RoleName,
                    CreditBalance = u.CreditBalance,
                    IsActive = u.IsActive ?? false,
                    LockedUntil = u.LockedUntil,
                    LastBet = u.LastBet,
                    CreatedAt = u.CreatedAt,
                    TotalBets = u.Bets.Count,
                    TotalWagered = u.Bets.Sum(b => b.Stake)
                })
                .ToListAsync();

            model.Users = users;
            model.TotalUsers = totalUsers;
            model.ActiveUsers = await _context.UserAccounts.CountAsync(u => u.IsActive == true);
            model.LockedUsers = await _context.UserAccounts.CountAsync(u => u.LockedUntil > DateTime.Now);
            model.TotalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            ViewBag.Roles = await _context.Roles.ToDictionaryAsync(r => r.RoleId, r => r.RoleName);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var user = await _context.UserAccounts.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                user.IsActive = !user.IsActive;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                var status = user.IsActive == true ? "activada" : "desactivada";
                TempData["SuccessMessage"] = $"Cuenta de {user.UserName} {status} exitosamente";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cambiar el estado de la cuenta";
                // Log del error si tienes logging configurado
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LockAccount(int id, int hours, string reason)
        {
            try
            {
                var user = await _context.UserAccounts.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var until = DateTime.Now.AddHours(hours);
                var success = await _userService.LockUserAccountAsync(id, until, reason);

                if (success)
                {
                    await _notificationService.SendNotificationAsync(id,
                        $"Tu cuenta ha sido bloqueada temporalmente hasta {until:dd/MM/yyyy HH:mm}. Motivo: {reason}");

                    TempData["SuccessMessage"] = $"Cuenta de {user.UserName} bloqueada exitosamente por {hours} horas";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al bloquear la cuenta";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al bloquear la cuenta";
                // Log del error si tienes logging configurado
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockAccount(int id)
        {
            try
            {
                var user = await _context.UserAccounts.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var success = await _userService.UnlockUserAccountAsync(id);

                if (success)
                {
                    await _notificationService.SendNotificationAsync(id,
                        "Tu cuenta ha sido desbloqueada. Ya puedes acceder nuevamente.");

                    TempData["SuccessMessage"] = $"Cuenta de {user.UserName} desbloqueada exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al desbloquear la cuenta";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al desbloquear la cuenta";
                // Log del error si tienes logging configurado
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, int roleId)
        {
            try
            {
                var user = await _context.UserAccounts.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var newRole = await _context.Roles.FindAsync(roleId);
                if (newRole == null)
                {
                    TempData["ErrorMessage"] = "Rol no válido";
                    return RedirectToAction(nameof(Index));
                }

                var oldRoleName = user.Role?.RoleName ?? "Sin rol";
                user.RoleId = roleId;
                user.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Rol de {user.UserName} cambiado de '{oldRoleName}' a '{newRole.RoleName}' exitosamente";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al cambiar el rol";
                // Log del error si tienes logging configurado
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustBalance(int id, decimal amount, string type, string reason)
        {
            try
            {
                if (amount <= 0)
                {
                    TempData["ErrorMessage"] = "La cantidad debe ser mayor a 0";
                    return RedirectToAction(nameof(Index));
                }

                var user = await _context.UserAccounts.FindAsync(id);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Usuario no encontrado";
                    return RedirectToAction(nameof(Index));
                }

                var transactionType = type == "add" ? "DEPOSIT" : "WITHDRAWAL";
                var adjustAmount = type == "add" ? Math.Abs(amount) : -Math.Abs(amount);

                // Verificar que no quede en negativo al restar
                if (type == "subtract" && user.CreditBalance < amount)
                {
                    TempData["ErrorMessage"] = $"No se puede restar ${amount:N2}. Balance actual: ${user.CreditBalance:N2}";
                    return RedirectToAction(nameof(Index));
                }

                var success = await _userService.UpdateUserBalanceAsync(id, Math.Abs(amount), transactionType);

                if (success)
                {
                    await _notificationService.SendNotificationAsync(id,
                        $"Ajuste de saldo: {(type == "add" ? "+" : "-")}${Math.Abs(amount):N2}. Motivo: {reason}");

                    var action = type == "add" ? "agregado" : "restado";
                    TempData["SuccessMessage"] = $"${Math.Abs(amount):N2} {action} al balance de {user.UserName} exitosamente";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error al ajustar el saldo";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error al ajustar el saldo";
                // Log del error si tienes logging configurado
            }

            return RedirectToAction(nameof(Index));
        }

        // Método opcional para obtener detalles de usuario via AJAX
        [HttpGet]
        public async Task<IActionResult> GetUserDetails(int id)
        {
            try
            {
                var user = await _context.UserAccounts
                    .Include(u => u.Role)
                    .Include(u => u.Bets)
                    .Include(u => u.PaymentTransactions)
                    .FirstOrDefaultAsync(u => u.UserId == id);

                if (user == null)
                {
                    return NotFound();
                }

                var recentActivity = await GetUserRecentActivity(id);

                var userDetails = new
                {
                    user.UserId,
                    user.UserName,
                    user.Email,
                    RoleName = user.Role?.RoleName ?? "Sin rol",
                    user.CreditBalance,
                    user.IsActive,
                    user.LockedUntil,
                    user.LastBet,
                    user.CreatedAt,
                    TotalBets = user.Bets?.Count ?? 0,
                    TotalWagered = user.Bets?.Sum(b => b.Stake) ?? 0,
                    RecentActivity = recentActivity
                };

                return Json(userDetails);
            }
            catch (Exception ex)
            {
                return BadRequest("Error al obtener detalles del usuario");
            }
        }

        private async Task<List<object>> GetUserRecentActivity(int userId)
        {
            try
            {
                var activities = new List<object>();

                // Últimas apuestas
                var recentBets = await _context.Bets
                    .Where(b => b.Users.Any(u => u.UserId == userId))
                    .OrderByDescending(b => b.CreatedAt)
                    .Take(5)
                    .Select(b => new { Type = "Bet", Date = b.CreatedAt, Amount = b.Stake, Status = b.BetStatus })
                    .ToListAsync();

                // Últimas transacciones
                var recentTransactions = await _context.PaymentTransactions
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(5)
                    .Select(t => new { Type = t.TransactionType, Date = t.CreatedAt, t.Amount, t.Status })
                    .ToListAsync();

                activities.AddRange(recentBets);
                activities.AddRange(recentTransactions);

                return activities.OrderByDescending(a => ((dynamic)a).Date).Take(10).ToList();
            }
            catch (Exception ex)
            {
                // Log del error si tienes logging configurado
                return new List<object>();
            }
        }
    }
}