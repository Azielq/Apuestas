using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.ViewModels;
using System.Security.Claims;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IUserService
    {
        // Autenticación y usuario actual
        int GetCurrentUserId();
        string GetCurrentUserEmail();
        Task<UserAccount?> GetCurrentUserAsync();
        Task<bool> IsUserInRoleAsync(int userId, string roleName);
        Task<bool> IsCurrentUserInRoleAsync(string roleName);
        ClaimsPrincipal GetCurrentUser();

        // Gestión de usuarios
        Task<UserAccount?> GetUserByIdAsync(int userId);
        Task<UserAccount?> GetUserByEmailAsync(string email);
        Task<UserProfileViewModel?> GetUserProfileAsync(int userId);
        Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileViewModel model);
        Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
        Task<bool> UpdateUserBalanceAsync(int userId, decimal amount, string transactionType);
        Task<bool> LockUserAccountAsync(int userId, DateTime until, string reason);
        Task<bool> UnlockUserAccountAsync(int userId);
        Task<List<UserAccount>> GetActiveUsersAsync();
        Task<bool> RecordLoginAttemptAsync(string email, bool isSuccessful);
        Task<int> GetFailedLoginAttemptsAsync(string email, TimeSpan period);
    }
}
