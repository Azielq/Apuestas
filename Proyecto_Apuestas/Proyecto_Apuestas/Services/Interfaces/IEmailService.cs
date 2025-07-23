namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task<bool> SendTemplatedEmailAsync(string to, string templateName, Dictionary<string, string> replacements);
        Task<bool> SendWelcomeEmailAsync(string to, string userName);
        Task<bool> SendPasswordResetEmailAsync(string to, string resetToken);
        Task<bool> SendBetConfirmationEmailAsync(string to, int betId, decimal amount);
        Task<bool> SendWithdrawalConfirmationEmailAsync(string to, decimal amount, string reference);
        Task<bool> SendAccountLockedEmailAsync(string to, DateTime lockedUntil, string reason);
        Task<bool> SendPromotionalEmailAsync(List<string> recipients, string subject, string promoContent);
    }
}
