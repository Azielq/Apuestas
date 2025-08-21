using Microsoft.Extensions.Diagnostics.HealthChecks;
using HealthChecks.UI.Core;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Implementations;

namespace Proyecto_Apuestas.Configuration
{
    /// <summary>
    /// Configuration for comprehensive health checks
    /// </summary>
    public static class HealthChecksConfiguration
    {
        /// <summary>
        /// Configures health checks for the application
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Application configuration</param>
        /// <returns>Health checks builder</returns>
        public static IServiceCollection AddEnhancedHealthChecks(this IServiceCollection services, IConfiguration configuration)
        {
            var healthChecksBuilder = services.AddHealthChecks();

            // Database health check
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                healthChecksBuilder.AddMySql(
                    connectionString,
                    name: "mysql-database",
                    tags: new[] { "database", "mysql", "critical" },
                    timeout: TimeSpan.FromSeconds(10));

                // Entity Framework context health check
                healthChecksBuilder.AddDbContextCheck<apuestasDbContext>(
                    name: "ef-database-context",
                    tags: new[] { "database", "ef", "critical" });
            }

            // External API health checks
            var oddsApiUrl = configuration["OddsApi:BaseUrl"];
            if (!string.IsNullOrEmpty(oddsApiUrl))
            {
                healthChecksBuilder.AddUrlGroup(
                    new Uri($"{oddsApiUrl}/sports"),
                    name: "odds-api",
                    tags: new[] { "external", "api", "odds" },
                    timeout: TimeSpan.FromSeconds(15));
            }

            // SendGrid email service health check
            var sendGridApiKey = configuration["EmailSettings:SendGrid:ApiKey"];
            if (!string.IsNullOrEmpty(sendGridApiKey))
            {
                healthChecksBuilder.AddCheck<SendGridHealthCheck>(
                    name: "sendgrid-email",
                    tags: new[] { "external", "email", "sendgrid" });
            }

            // Stripe payment service health check
            var stripeApiKey = configuration["PaymentSettings:Stripe:SecretKey"];
            if (!string.IsNullOrEmpty(stripeApiKey))
            {
                healthChecksBuilder.AddCheck<StripeHealthCheck>(
                    name: "stripe-payment",
                    tags: new[] { "external", "payment", "stripe" });
            }

            // Memory usage health check
            healthChecksBuilder.AddCheck<MemoryHealthCheck>(
                name: "memory-usage",
                tags: new[] { "system", "memory" });

            // Disk space health check
            healthChecksBuilder.AddCheck<DiskSpaceHealthCheck>(
                name: "disk-space",
                tags: new[] { "system", "storage" });

            // Application configuration health check
            healthChecksBuilder.AddCheck<ConfigurationHealthCheck>(
                name: "application-config",
                tags: new[] { "configuration", "critical" });

            // Add health checks UI
            services.AddHealthChecksUI(setup =>
            {
                setup.SetEvaluationTimeInSeconds(30); // Check every 30 seconds
                setup.MaximumHistoryEntriesPerEndpoint(50);
                setup.SetApiMaxActiveRequests(2);
                
                setup.AddHealthCheckEndpoint("Proyecto Apuestas", "/health");
                setup.AddHealthCheckEndpoint("Database Only", "/health/database");
                setup.AddHealthCheckEndpoint("External APIs", "/health/external");
                setup.AddHealthCheckEndpoint("System Resources", "/health/system");
            })
            .AddInMemoryStorage();

            // Register custom health check services
            services.AddScoped<SendGridHealthCheck>();
            services.AddScoped<StripeHealthCheck>();
            services.AddScoped<MemoryHealthCheck>();
            services.AddScoped<DiskSpaceHealthCheck>();
            services.AddScoped<ConfigurationHealthCheck>();

            return services;
        }
    }

    /// <summary>
    /// SendGrid email service health check
    /// </summary>
    public class SendGridHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendGridHealthCheck> _logger;

        public SendGridHealthCheck(IConfiguration configuration, ILogger<SendGridHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var apiKey = _configuration["EmailSettings:SendGrid:ApiKey"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    return HealthCheckResult.Unhealthy("SendGrid API key not configured");
                }

                // Basic validation - check if API key format is correct
                if (!apiKey.StartsWith("SG."))
                {
                    return HealthCheckResult.Unhealthy("Invalid SendGrid API key format");
                }

                return HealthCheckResult.Healthy("SendGrid configuration is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendGrid health check failed");
                return HealthCheckResult.Unhealthy($"SendGrid health check failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Stripe payment service health check
    /// </summary>
    public class StripeHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeHealthCheck> _logger;

        public StripeHealthCheck(IConfiguration configuration, ILogger<StripeHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var secretKey = _configuration["PaymentSettings:Stripe:SecretKey"];
                var publicKey = _configuration["PaymentSettings:Stripe:PublicKey"];

                if (string.IsNullOrEmpty(secretKey) || string.IsNullOrEmpty(publicKey))
                {
                    return HealthCheckResult.Unhealthy("Stripe API keys not configured");
                }

                // Basic validation - check if API keys format is correct
                if (!secretKey.StartsWith("sk_") || !publicKey.StartsWith("pk_"))
                {
                    return HealthCheckResult.Unhealthy("Invalid Stripe API key format");
                }

                return HealthCheckResult.Healthy("Stripe configuration is valid");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe health check failed");
                return HealthCheckResult.Unhealthy($"Stripe health check failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Memory usage health check
    /// </summary>
    public class MemoryHealthCheck : IHealthCheck
    {
        private readonly ILogger<MemoryHealthCheck> _logger;

        public MemoryHealthCheck(ILogger<MemoryHealthCheck> logger)
        {
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var allocatedBytes = GC.GetTotalMemory(false);
                var allocatedMB = allocatedBytes / 1024 / 1024;

                var workingSet = Environment.WorkingSet;
                var workingSetMB = workingSet / 1024 / 1024;

                var data = new Dictionary<string, object>
                {
                    ["allocated_memory_mb"] = allocatedMB,
                    ["working_set_mb"] = workingSetMB,
                    ["gen0_collections"] = GC.CollectionCount(0),
                    ["gen1_collections"] = GC.CollectionCount(1),
                    ["gen2_collections"] = GC.CollectionCount(2)
                };

                // Alert if memory usage is high
                if (allocatedMB > 500) // 500MB threshold
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"High memory usage: {allocatedMB}MB allocated, {workingSetMB}MB working set", 
                        data: data));
                }

                if (allocatedMB > 1000) // 1GB threshold
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        $"Critical memory usage: {allocatedMB}MB allocated, {workingSetMB}MB working set", 
                        data: data));
                }

                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Memory usage normal: {allocatedMB}MB allocated, {workingSetMB}MB working set", 
                    data: data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Memory health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy($"Memory health check failed: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Disk space health check
    /// </summary>
    public class DiskSpaceHealthCheck : IHealthCheck
    {
        private readonly ILogger<DiskSpaceHealthCheck> _logger;

        public DiskSpaceHealthCheck(ILogger<DiskSpaceHealthCheck> logger)
        {
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var drives = DriveInfo.GetDrives()
                    .Where(d => d.IsReady && d.DriveType == DriveType.Fixed)
                    .ToList();

                var driveData = new Dictionary<string, object>();
                bool hasLowSpace = false;
                bool hasCriticalSpace = false;

                foreach (var drive in drives)
                {
                    var freeSpaceGB = drive.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                    var totalSpaceGB = drive.TotalSize / (1024.0 * 1024.0 * 1024.0);
                    var usedSpacePercent = ((totalSpaceGB - freeSpaceGB) / totalSpaceGB) * 100;

                    driveData[$"drive_{drive.Name.Replace("\\", "").Replace(":", "")}_free_gb"] = Math.Round(freeSpaceGB, 2);
                    driveData[$"drive_{drive.Name.Replace("\\", "").Replace(":", "")}_total_gb"] = Math.Round(totalSpaceGB, 2);
                    driveData[$"drive_{drive.Name.Replace("\\", "").Replace(":", "")}_used_percent"] = Math.Round(usedSpacePercent, 2);

                    if (usedSpacePercent > 90)
                        hasCriticalSpace = true;
                    else if (usedSpacePercent > 80)
                        hasLowSpace = true;
                }

                if (hasCriticalSpace)
                {
                    return Task.FromResult(HealthCheckResult.Unhealthy(
                        "Critical disk space usage detected (>90%)", 
                        data: driveData));
                }

                if (hasLowSpace)
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        "Low disk space detected (>80%)", 
                        data: driveData));
                }

                return Task.FromResult(HealthCheckResult.Healthy(
                    "Disk space usage is normal", 
                    data: driveData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Disk space health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy($"Disk space health check failed: {ex.Message}"));
            }
        }
    }

    /// <summary>
    /// Application configuration health check
    /// </summary>
    public class ConfigurationHealthCheck : IHealthCheck
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConfigurationHealthCheck> _logger;

        public ConfigurationHealthCheck(IConfiguration configuration, ILogger<ConfigurationHealthCheck> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var issues = new List<string>();
                var data = new Dictionary<string, object>();

                // Check critical configuration values
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrEmpty(connectionString))
                {
                    issues.Add("Database connection string missing");
                }
                data["has_database_connection"] = !string.IsNullOrEmpty(connectionString);

                var oddsApiKey = _configuration["OddsApi:ApiKey"];
                if (string.IsNullOrEmpty(oddsApiKey))
                {
                    issues.Add("Odds API key missing");
                }
                data["has_odds_api_key"] = !string.IsNullOrEmpty(oddsApiKey);

                var sendGridKey = _configuration["EmailSettings:SendGrid:ApiKey"];
                data["has_sendgrid_key"] = !string.IsNullOrEmpty(sendGridKey);

                var stripeKeys = new
                {
                    Secret = _configuration["PaymentSettings:Stripe:SecretKey"],
                    Public = _configuration["PaymentSettings:Stripe:PublicKey"]
                };
                data["has_stripe_keys"] = !string.IsNullOrEmpty(stripeKeys.Secret) && !string.IsNullOrEmpty(stripeKeys.Public);

                if (issues.Any())
                {
                    return Task.FromResult(HealthCheckResult.Degraded(
                        $"Configuration issues detected: {string.Join(", ", issues)}", 
                        data: data));
                }

                return Task.FromResult(HealthCheckResult.Healthy(
                    "All critical configuration values are present", 
                    data: data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Configuration health check failed");
                return Task.FromResult(HealthCheckResult.Unhealthy($"Configuration health check failed: {ex.Message}"));
            }
        }
    }
}