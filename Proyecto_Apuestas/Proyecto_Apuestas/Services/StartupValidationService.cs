using Proyecto_Apuestas.Configuration;
using Proyecto_Apuestas.Helpers;
using Microsoft.Extensions.Options;

namespace Proyecto_Apuestas.Services
{
    public interface IStartupValidationService
    {
        Task<bool> ValidateAllConfigurationsAsync();
        Task<List<string>> GetValidationErrorsAsync();
    }

    public class StartupValidationService : IStartupValidationService
    {
        private readonly IOptions<EmailSettings> _emailSettings;
        private readonly IOptions<PaymentSettings> _paymentSettings;
        private readonly IOptions<ApplicationSettings> _applicationSettings;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StartupValidationService> _logger;

        public StartupValidationService(
            IOptions<EmailSettings> emailSettings,
            IOptions<PaymentSettings> paymentSettings,
            IOptions<ApplicationSettings> applicationSettings,
            IConfiguration configuration,
            ILogger<StartupValidationService> logger)
        {
            _emailSettings = emailSettings;
            _paymentSettings = paymentSettings;
            _applicationSettings = applicationSettings;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> ValidateAllConfigurationsAsync()
        {
            var errors = await GetValidationErrorsAsync();
            
            if (errors.Count > 0)
            {
                _logger.LogError("Configuration validation failed with {ErrorCount} errors: {Errors}", 
                    errors.Count, string.Join(", ", errors));
                return false;
            }

            _logger.LogInformation("All configuration validations passed successfully");
            return true;
        }

        public async Task<List<string>> GetValidationErrorsAsync()
        {
            var errors = new List<string>();

            // Validate database connection
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                errors.Add("Database connection string is missing");
            }

            // Validate email settings
            var emailSettings = _emailSettings.Value;
            if (string.IsNullOrWhiteSpace(emailSettings.SendGrid.ApiKey))
            {
                errors.Add("SendGrid API key is missing");
            }
            if (string.IsNullOrWhiteSpace(emailSettings.FromEmail))
            {
                errors.Add("Email FromEmail is missing");
            }

            // Validate payment settings
            var paymentSettings = _paymentSettings.Value;
            if (string.IsNullOrWhiteSpace(paymentSettings.Stripe.PublicKey))
            {
                errors.Add("Stripe public key is missing");
            }
            if (string.IsNullOrWhiteSpace(paymentSettings.Stripe.SecretKey))
            {
                errors.Add("Stripe secret key is missing");
            }

            // Validate application settings
            var appSettings = _applicationSettings.Value;
            if (appSettings.Betting.MinimumBet <= 0)
            {
                errors.Add("Minimum bet amount must be greater than 0");
            }
            if (appSettings.Betting.MaximumBet <= appSettings.Betting.MinimumBet)
            {
                errors.Add("Maximum bet amount must be greater than minimum bet amount");
            }

            // Validate security settings
            var jwtSecret = ConfigurationHelper.SecuritySettings.JwtSecret;
            if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret == "DefaultJwtSecret123456789")
            {
                errors.Add("JWT secret is missing or using default value (security risk)");
            }

            var encryptionKey = ConfigurationHelper.SecuritySettings.EncryptionKey;
            if (string.IsNullOrWhiteSpace(encryptionKey) || encryptionKey == "DefaultKey123456")
            {
                errors.Add("Encryption key is missing or using default value (security risk)");
            }

            return await Task.FromResult(errors);
        }
    }
}