using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;
using AutoMapper;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly apuestasDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IMapper _mapper;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            apuestasDbContext context,
            IEmailService emailService,
            IMapper mapper,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<bool> SendNotificationAsync(int userId, string message, string? notificationType = null, string? actionUrl = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();

                // NOTE: Esto nos Envía notificación en tiempo real si está disponible
                // await _hubContext.Clients.User(userId.ToString()).SendAsync("NewNotification", notification);

                // Envia email si es crítico
                if (notificationType == "CRITICAL")
                {
                    var user = await _context.UserAccounts.FindAsync(userId);
                    if (user != null)
                    {
                        await _emailService.SendEmailAsync(user.Email, "Notificación Importante", message);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SendBulkNotificationAsync(List<int> userIds, string message, string? notificationType = null)
        {
            try
            {
                var notifications = userIds.Select(userId => new Notification
                {
                    UserId = userId,
                    Message = message,
                    IsRead = false,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                }).ToList();

                await _context.Notifications.AddRangeAsync(notifications);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending bulk notifications");
                return false;
            }
        }

        public async Task<NotificationListViewModel> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20, bool unreadOnly = false)
        {
            var query = _context.Notifications
                .Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            var totalNotifications = await query.CountAsync();

            var notifications = await query
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var notificationViewModels = notifications.Select(n => new NotificationViewModel
            {
                NotificationId = n.NotificationId,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt,
                TimeAgo = GetTimeAgo(n.CreatedAt),
                NotificationType = DetermineNotificationType(n.Message),
                ActionUrl = ExtractActionUrl(n.Message)
            }).ToList();

            return new NotificationListViewModel
            {
                Notifications = notificationViewModels,
                UnreadCount = await GetUnreadCountAsync(userId),
                HasMore = totalNotifications > page * pageSize,
                PageNumber = page,
                PageSize = pageSize
            };
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null) return false;

                notification.IsRead = true;
                notification.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
                return false;
            }
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && !n.IsRead)
                    .ToListAsync();

                foreach (var notification in notifications)
                {
                    notification.IsRead = true;
                    notification.UpdatedAt = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
                return false;
            }
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId, int userId)
        {
            try
            {
                var notification = await _context.Notifications
                    .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

                if (notification == null) return false;

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification");
                return false;
            }
        }

        public async Task<bool> DeleteAllReadNotificationsAsync(int userId)
        {
            try
            {
                var notifications = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead)
                    .ToListAsync();

                _context.Notifications.RemoveRange(notifications);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting read notifications");
                return false;
            }
        }

        public async Task SendWelcomeNotificationAsync(int userId)
        {
            await SendNotificationAsync(userId,
                "¡Bienvenido a Proyecto Apuestas! Tu cuenta ha sido creada exitosamente. " +
                "Realiza tu primer depósito y recibe un bono del 100% hasta $100.",
                "WELCOME",
                "/payment/deposit");
        }

        public async Task SendBetWonNotificationAsync(int userId, int betId, decimal amount)
        {
            await SendNotificationAsync(userId,
                $"¡Felicidades! Has ganado ${amount:N2} en tu apuesta #{betId}. " +
                $"El monto ha sido acreditado a tu cuenta.",
                "BET_WON",
                $"/betting/details/{betId}");
        }

        public async Task SendBetLostNotificationAsync(int userId, int betId)
        {
            await SendNotificationAsync(userId,
                $"Tu apuesta #{betId} no fue ganadora. ¡Mejor suerte la próxima vez!",
                "BET_LOST",
                $"/betting/details/{betId}");
        }

        public async Task SendDepositSuccessNotificationAsync(int userId, decimal amount)
        {
            await SendNotificationAsync(userId,
                $"Depósito exitoso de ${amount:N2}. El saldo está disponible en tu cuenta.",
                "DEPOSIT",
                "/payment/history");
        }

        public async Task SendWithdrawalProcessedNotificationAsync(int userId, decimal amount)
        {
            await SendNotificationAsync(userId,
                $"Tu retiro de ${amount:N2} ha sido procesado. " +
                "Los fondos llegarán a tu cuenta en 1-3 días hábiles.",
                "WITHDRAWAL",
                "/payment/history");
        }

        public async Task SendEventReminderNotificationAsync(int userId, int eventId, DateTime eventDate)
        {
            var timeUntilEvent = eventDate - DateTime.Now;
            var message = timeUntilEvent.TotalHours switch
            {
                <= 1 => "¡El evento en el que apostaste comienza en menos de una hora!",
                <= 24 => $"El evento en el que apostaste comienza en {timeUntilEvent.Hours} horas.",
                _ => $"Recordatorio: El evento en el que apostaste es el {eventDate:dd/MM/yyyy HH:mm}."
            };

            await SendNotificationAsync(userId, message, "EVENT_REMINDER", $"/events/details/{eventId}");
        }

        public async Task SendPromotionalNotificationAsync(int userId, string promoCode, decimal bonusAmount)
        {
            await SendNotificationAsync(userId,
                $"¡Oferta especial! Usa el código {promoCode} en tu próximo depósito " +
                $"y recibe un bono de ${bonusAmount:N2}.",
                "PROMO",
                "/payment/deposit");
        }

        // Métodos auxiliares privados
        private string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            return timeSpan.TotalMinutes switch
            {
                < 1 => "Hace un momento",
                < 60 => $"Hace {(int)timeSpan.TotalMinutes} minutos",
                < 1440 => $"Hace {(int)timeSpan.TotalHours} horas",
                < 10080 => $"Hace {(int)timeSpan.TotalDays} días",
                _ => dateTime.ToString("dd/MM/yyyy")
            };
        }

        private string DetermineNotificationType(string message)
        {
            if (message.Contains("ganado") || message.Contains("Felicidades"))
                return "success";
            if (message.Contains("perdido") || message.Contains("no fue ganadora"))
                return "warning";
            if (message.Contains("Depósito") || message.Contains("Retiro"))
                return "info";
            if (message.Contains("cancelado") || message.Contains("error"))
                return "danger";

            return "info";
        }

        private string? ExtractActionUrl(string message)
        {
            // Implementa lógica para extraer URLs de acciones del mensaje
            // Por ahora retornamos null
            return null;
        }
    }
}
