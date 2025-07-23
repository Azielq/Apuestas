namespace Proyecto_Apuestas.ViewModels
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string Message { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string TimeAgo { get; set; }
        public string NotificationType { get; set; }
        public string? ActionUrl { get; set; }
    }

    public class NotificationListViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; }
        public int UnreadCount { get; set; }
        public bool HasMore { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}