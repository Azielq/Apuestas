using Proyecto_Apuestas.Configuration;
using Proyecto_Apuestas.Helpers;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Proyecto_Apuestas.Services
{
    public interface IConfigurationDiagnosticsService
    {
        Task<ConfigurationDiagnostics> RunDiagnosticsAsync();
        Task<string> GenerateConfigurationReportAsync();
        Task<bool> ValidateJsonFilesAsync();
    }

    public class ConfigurationDiagnosticsService : IConfigurationDiagnosticsService
    {
        private readonly IConfiguration _configuration;
        private readonly IOptions<EmailSettings> _emailSettings;
        private readonly IOptions<PaymentSettings> _paymentSettings;
        private readonly IOptions<ApplicationSettings> _applicationSettings;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ConfigurationDiagnosticsService> _logger;

        public ConfigurationDiagnosticsService(
            IConfiguration configuration,
            IOptions<EmailSettings> emailSettings,
            IOptions<PaymentSettings> paymentSettings,
            IOptions<ApplicationSettings> applicationSettings,
            IWebHostEnvironment environment,
            ILogger<ConfigurationDiagnosticsService> logger)
        {
            _configuration = configuration;
            _emailSettings = emailSettings;
            _paymentSettings = paymentSettings;
            _applicationSettings = applicationSettings;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ConfigurationDiagnostics> RunDiagnosticsAsync()
        {
            var diagnostics = new ConfigurationDiagnostics
            {
                Environment = _environment.EnvironmentName,
                Timestamp = DateTime.UtcNow,
                IsValid = true,
                Issues = new List<string>(),
                Warnings = new List<string>(),
                Recommendations = new List<string>()
            };

            try
            {
                // Test configuration loading
                var emailSettings = _emailSettings.Value;
                var paymentSettings = _paymentSettings.Value;
                var appSettings = _applicationSettings.Value;

                // Validate critical sections
                await ValidateEmailConfiguration(diagnostics, emailSettings);
                await ValidatePaymentConfiguration(diagnostics, paymentSettings);
                await ValidateApplicationConfiguration(diagnostics, appSettings);
                await ValidateDatabaseConfiguration(diagnostics);
                await ValidateSecurityConfiguration(diagnostics);

                // Environment-specific validations
                if (_environment.IsProduction())
                {
                    await ValidateProductionConfiguration(diagnostics);
                }
                else if (_environment.IsDevelopment())
                {
                    await ValidateDevelopmentConfiguration(diagnostics);
                }

                diagnostics.IsValid = diagnostics.Issues.Count == 0;
            }
            catch (Exception ex)
            {
                diagnostics.IsValid = false;
                diagnostics.Issues.Add($"Critical error during configuration validation: {ex.Message}");
                _logger.LogError(ex, "Configuration diagnostics failed");
            }

            return diagnostics;
        }

        public async Task<string> GenerateConfigurationReportAsync()
        {
            var diagnostics = await RunDiagnosticsAsync();
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== BET506 CONFIGURATION DIAGNOSTICS REPORT ===");
            report.AppendLine($"Generated: {diagnostics.Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            report.AppendLine($"Environment: {diagnostics.Environment}");
            report.AppendLine($"Overall Status: {(diagnostics.IsValid ? "? VALID" : "? INVALID")}");
            report.AppendLine();

            if (diagnostics.Issues.Any())
            {
                report.AppendLine("?? CRITICAL ISSUES:");
                foreach (var issue in diagnostics.Issues)
                {
                    report.AppendLine($"  • {issue}");
                }
                report.AppendLine();
            }

            if (diagnostics.Warnings.Any())
            {
                report.AppendLine("?? WARNINGS:");
                foreach (var warning in diagnostics.Warnings)
                {
                    report.AppendLine($"  • {warning}");
                }
                report.AppendLine();
            }

            if (diagnostics.Recommendations.Any())
            {
                report.AppendLine("?? RECOMMENDATIONS:");
                foreach (var recommendation in diagnostics.Recommendations)
                {
                    report.AppendLine($"  • {recommendation}");
                }
                report.AppendLine();
            }

            // Configuration summary (non-sensitive data only)
            if (_environment.IsDevelopment())
            {
                report.AppendLine("?? CONFIGURATION SUMMARY:");
                report.AppendLine($"  Database: {(_configuration.GetConnectionString("DefaultConnection") != null ? "Configured" : "Missing")}");
                report.AppendLine($"  SendGrid: {(!string.IsNullOrEmpty(_emailSettings.Value.SendGrid.ApiKey) ? "Configured" : "Missing")}");
                report.AppendLine($"  Stripe: {(!string.IsNullOrEmpty(_paymentSettings.Value.Stripe.PublicKey) ? "Configured" : "Missing")}");
                report.AppendLine($"  JWT Secret: {(!string.IsNullOrEmpty(ConfigurationHelper.SecuritySettings.JwtSecret) ? "Configured" : "Missing")}");
            }

            return report.ToString();
        }

        public async Task<bool> ValidateJsonFilesAsync()
        {
            var files = new[]
            {
                "appsettings.json",
                $"appsettings.{_environment.EnvironmentName}.json"
            };

            foreach (var file in files)
            {
                try
                {
                    var path = Path.Combine(_environment.ContentRootPath, file);
                    if (File.Exists(path))
                    {
                        var content = await File.ReadAllTextAsync(path);
                        JsonDocument.Parse(content); // This will throw if invalid JSON
                        _logger.LogInformation("JSON file {File} is valid", file);
                    }
                    else
                    {
                        _logger.LogWarning("JSON file {File} not found", file);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError("Invalid JSON in file {File}: {Error}", file, ex.Message);
                    return false;
                }
            }

            return true;
        }

        private async Task ValidateEmailConfiguration(ConfigurationDiagnostics diagnostics, EmailSettings settings)
        {
            if (string.IsNullOrEmpty(settings.SendGrid.ApiKey))
                diagnostics.Issues.Add("SendGrid API key is not configured");
            else if (settings.SendGrid.ApiKey.Contains("development") && _environment.IsProduction())
                diagnostics.Issues.Add("Development SendGrid API key is being used in production");

            if (string.IsNullOrEmpty(settings.FromEmail))
                diagnostics.Issues.Add("From email address is not configured");

            if (string.IsNullOrEmpty(settings.BaseUrl))
                diagnostics.Warnings.Add("Base URL is not configured - email links may not work");

            await Task.CompletedTask;
        }

        private async Task ValidatePaymentConfiguration(ConfigurationDiagnostics diagnostics, PaymentSettings settings)
        {
            if (string.IsNullOrEmpty(settings.Stripe.PublicKey))
                diagnostics.Issues.Add("Stripe public key is not configured");

            if (string.IsNullOrEmpty(settings.Stripe.SecretKey))
                diagnostics.Issues.Add("Stripe secret key is not configured");

            if (settings.Stripe.PublicKey.Contains("test") && _environment.IsProduction())
                diagnostics.Issues.Add("Test Stripe keys are being used in production");

            await Task.CompletedTask;
        }

        private async Task ValidateApplicationConfiguration(ConfigurationDiagnostics diagnostics, ApplicationSettings settings)
        {
            if (settings.Betting.MinimumBet <= 0)
                diagnostics.Issues.Add("Minimum bet amount must be greater than 0");

            if (settings.Betting.MaximumBet <= settings.Betting.MinimumBet)
                diagnostics.Issues.Add("Maximum bet must be greater than minimum bet");

            if (settings.Betting.DailyBetLimit <= settings.Betting.MaximumBet)
                diagnostics.Warnings.Add("Daily bet limit should be significantly higher than maximum single bet");

            await Task.CompletedTask;
        }

        private async Task ValidateDatabaseConfiguration(ConfigurationDiagnostics diagnostics)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                diagnostics.Issues.Add("Database connection string is not configured");
            }
            else if (connectionString.Contains("localhost") && _environment.IsProduction())
            {
                diagnostics.Warnings.Add("Using localhost database connection in production");
            }

            await Task.CompletedTask;
        }

        private async Task ValidateSecurityConfiguration(ConfigurationDiagnostics diagnostics)
        {
            var jwtSecret = ConfigurationHelper.SecuritySettings.JwtSecret;
            if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Contains("Default"))
                diagnostics.Issues.Add("JWT secret is not properly configured");

            var encryptionKey = ConfigurationHelper.SecuritySettings.EncryptionKey;
            if (string.IsNullOrEmpty(encryptionKey) || encryptionKey.Contains("Default"))
                diagnostics.Issues.Add("Encryption key is not properly configured");

            if (jwtSecret.Length < 32)
                diagnostics.Warnings.Add("JWT secret should be at least 32 characters long");

            await Task.CompletedTask;
        }

        private async Task ValidateProductionConfiguration(ConfigurationDiagnostics diagnostics)
        {
            if (_applicationSettings.Value.EnableDetailedErrors)
                diagnostics.Issues.Add("Detailed errors should be disabled in production");

            if (_applicationSettings.Value.EnableSwagger)
                diagnostics.Warnings.Add("Swagger should be disabled in production");

            if (ConfigurationHelper.SecuritySettings.MaxLoginAttempts > 5)
                diagnostics.Warnings.Add("Consider reducing max login attempts in production");

            await Task.CompletedTask;
        }

        private async Task ValidateDevelopmentConfiguration(ConfigurationDiagnostics diagnostics)
        {
            if (!_applicationSettings.Value.EnableDetailedErrors)
                diagnostics.Recommendations.Add("Enable detailed errors for better debugging");

            if (!_applicationSettings.Value.EnableSwagger)
                diagnostics.Recommendations.Add("Enable Swagger for API documentation");

            await Task.CompletedTask;
        }
    }

    public class ConfigurationDiagnostics
    {
        public string Environment { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsValid { get; set; }
        public List<string> Issues { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}