using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface INotificationService
    {
        Task<bool> SendNotificationAsync(int userId, string message, string? notificationType = null, string? actionUrl = null);
        Task<bool> SendBulkNotificationAsync(List<int> userIds, string message, string? notificationType = null);
        Task<NotificationListViewModel> GetUserNotificationsAsync(int userId, int page = 1, int pageSize = 20, bool unreadOnly = false);
        Task<int> GetUnreadCountAsync(int userId);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<bool> DeleteNotificationAsync(int notificationId, int userId);
        Task<bool> DeleteAllReadNotificationsAsync(int userId);
        Task SendWelcomeNotificationAsync(int userId);
        Task SendBetWonNotificationAsync(int userId, int betId, decimal amount);
        Task SendBetLostNotificationAsync(int userId, int betId);
        Task SendDepositSuccessNotificationAsync(int userId, decimal amount);
        Task SendWithdrawalProcessedNotificationAsync(int userId, decimal amount);
        Task SendEventReminderNotificationAsync(int userId, int eventId, DateTime eventDate);
        Task SendPromotionalNotificationAsync(int userId, string promoCode, decimal bonusAmount);
    }
}
