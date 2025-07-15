using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Proyecto_Apuestas.Services.Interfaces;

namespace Proyecto_Apuestas.Controllers
{
    [Authorize]
    public class NotificationController : BaseController
    {
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public NotificationController(
            INotificationService notificationService,
            IUserService userService,
            ILogger<NotificationController> logger) : base(logger)
        {
            _notificationService = notificationService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, bool unreadOnly = false)
        {
            var userId = _userService.GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, page, 20, unreadOnly);

            ViewBag.UnreadOnly = unreadOnly;
            return View(notifications);
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _notificationService.MarkAsReadAsync(id, userId);

            if (success)
            {
                return JsonSuccess();
            }

            return JsonError("No se pudo marcar la notificación como leída");
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _notificationService.MarkAllAsReadAsync(userId);

            if (success)
            {
                return JsonSuccess(message: "Todas las notificaciones marcadas como leídas");
            }

            return JsonError("Error al marcar las notificaciones");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _notificationService.DeleteNotificationAsync(id, userId);

            if (success)
            {
                return JsonSuccess();
            }

            return JsonError("No se pudo eliminar la notificación");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAllRead()
        {
            var userId = _userService.GetCurrentUserId();
            var success = await _notificationService.DeleteAllReadNotificationsAsync(userId);

            if (success)
            {
                AddSuccessMessage("Notificaciones leídas eliminadas");
                return RedirectToAction(nameof(Index));
            }

            AddErrorMessage("Error al eliminar las notificaciones");
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = _userService.GetCurrentUserId();
            var count = await _notificationService.GetUnreadCountAsync(userId);

            return JsonSuccess(new { count });
        }

        [HttpGet]
        public async Task<IActionResult> GetRecent()
        {
            var userId = _userService.GetCurrentUserId();
            var notifications = await _notificationService.GetUserNotificationsAsync(userId, 1, 5, true);

            return PartialView("_RecentNotifications", notifications);
        }
    }
}