namespace Proyecto_Apuestas.ViewModels
{
    public class UserProfileViewModel
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
        public DateOnly? BirthDate { get; set; }
        public decimal CreditBalance { get; set; }
        public DateTime? LastBet { get; set; }
        public DateTime CreatedAt { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        // Estadísticas
        public int TotalBets { get; set; }
        public int WonBets { get; set; }
        public int LostBets { get; set; }
        public decimal TotalWinnings { get; set; }
        public decimal TotalLosses { get; set; }
        public decimal WinRate { get; set; }
        
        // Propiedades calculadas
        public string FullName => $"{FirstName} {PrimerApellido} {SegundoApellido}".Trim();
        public int PendingBets => TotalBets - WonBets - LostBets;
        public decimal NetBalance => TotalWinnings - TotalLosses;
    }

    public class UpdateProfileViewModel
    {
        public string? FirstName { get; set; }
        public string? PrimerApellido { get; set; }
        public string? SegundoApellido { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Country { get; set; }
    }

    public class ChangePasswordViewModel
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class UserDashboardViewModel
    {
        public UserProfileViewModel UserProfile { get; set; } = new();
        public List<RecentBetViewModel> RecentBets { get; set; } = new();
        public List<NotificationViewModel> RecentNotifications { get; set; } = new();
        public decimal AvailableBalance { get; set; }
        public int UnreadNotificationsCount { get; set; }
    }

    public class RecentBetViewModel
    {
        public int BetId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public decimal Stake { get; set; }
        public decimal Odds { get; set; }
        public decimal PotentialPayout { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public bool IsLive { get; set; }
    }
}