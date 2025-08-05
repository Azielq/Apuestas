using Microsoft.Extensions.Options;
using Proyecto_Apuestas.Configuration;

namespace Proyecto_Apuestas.Helpers
{
    public static class ConfigurationHelper
    {
        private static IConfiguration _configuration;
        private static IServiceProvider? _serviceProvider;

        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Obtiene valor de configuración con valor por defecto
        /// </summary>
        public static T GetValue<T>(string key, T defaultValue = default)
        {
            try
            {
                return _configuration.GetValue<T>(key) ?? defaultValue;
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// Obtiene sección de configuración
        /// </summary>
        public static IConfigurationSection GetSection(string key)
        {
            return _configuration.GetSection(key);
        }

        /// <summary>
        /// Obtiene string de conexión
        /// </summary>
        public static string GetConnectionString(string name = "DefaultConnection")
        {
            return _configuration.GetConnectionString(name) ?? string.Empty;
        }

        /// <summary>
        /// Verifica si una key existe
        /// </summary>
        public static bool Exists(string key)
        {
            return _configuration[key] != null;
        }

        // Modern configuration access using IOptions pattern
        public static Configuration.EmailSettings EmailSettingsTyped
        {
            get
            {
                if (_serviceProvider == null)
                    throw new InvalidOperationException("ConfigurationHelper not initialized");

                return _serviceProvider.GetRequiredService<IOptions<Configuration.EmailSettings>>().Value;
            }
        }

        public static Configuration.PaymentSettings PaymentSettingsTyped
        {
            get
            {
                if (_serviceProvider == null)
                    throw new InvalidOperationException("ConfigurationHelper not initialized");

                return _serviceProvider.GetRequiredService<IOptions<Configuration.PaymentSettings>>().Value;
            }
        }

        public static Configuration.ApplicationSettings ApplicationSettingsTyped
        {
            get
            {
                if (_serviceProvider == null)
                    throw new InvalidOperationException("ConfigurationHelper not initialized");

                return _serviceProvider.GetRequiredService<IOptions<Configuration.ApplicationSettings>>().Value;
            }
        }

        // Convenience properties for new typed settings
        public static Configuration.BettingSettings BettingSettingsTyped => ApplicationSettingsTyped.Betting;
        public static Configuration.StripeSettings StripeSettingsTyped => PaymentSettingsTyped.Stripe;
        public static Configuration.SendGridSettings SendGridSettingsTyped => EmailSettingsTyped.SendGrid;

        /// <summary>
        /// Obtiene configuración de límites de apuesta
        /// </summary>
        public static class BettingLimits
        {
            public static decimal MinimumBet => GetValue("Betting:MinimumBet", 100m);
            public static decimal MaximumBet => GetValue("Betting:MaximumBet", 1000000m);
            public static decimal DailyLimit => GetValue("Betting:DailyLimit", 100000m);
            public static decimal WeeklyLimit => GetValue("Betting:WeeklyLimit", 500000m);
            public static decimal MonthlyLimit => GetValue("Betting:MonthlyLimit", 2000000m);
            public static int MaxBetsPerSlip => GetValue("Betting:MaxBetsPerSlip", 10);
            public static int MaxBetsPerDay => GetValue("Betting:MaxBetsPerDay", 50);
            public static decimal MaxPayoutPerBet => GetValue("Betting:MaxPayoutPerBet", 10000000m);

            // Límites por rol
            public static Dictionary<string, decimal> DailyLimitsByRole => new()
            {
                ["Regular"] = GetValue("Betting:Limits:Regular:Daily", 10000m),
                ["Premium"] = GetValue("Betting:Limits:Premium:Daily", 50000m),
                ["VIP"] = GetValue("Betting:Limits:VIP:Daily", 100000m)
            };

            public static Dictionary<string, decimal> MaxBetByRole => new()
            {
                ["Regular"] = GetValue("Betting:Limits:Regular:MaxBet", 5000m),
                ["Premium"] = GetValue("Betting:Limits:Premium:MaxBet", 20000m),
                ["VIP"] = GetValue("Betting:Limits:VIP:MaxBet", 50000m)
            };
        }

        /// <summary>
        /// Obtiene configuración de pagos
        /// </summary>
        public static class PaymentSettings
        {
            public static decimal MinimumDeposit => GetValue("Payment:MinimumDeposit", 1000m);
            public static decimal MaximumDeposit => GetValue("Payment:MaximumDeposit", 1000000m);
            public static decimal MinimumWithdrawal => GetValue("Payment:MinimumWithdrawal", 5000m);
            public static decimal MaximumWithdrawal => GetValue("Payment:MaximumWithdrawal", 500000m);
            public static decimal TransactionFee => GetValue("Payment:TransactionFee", 0m);
            public static decimal WithdrawalFeePercentage => GetValue("Payment:WithdrawalFeePercentage", 0m);
            public static int WithdrawalProcessingDays => GetValue("Payment:WithdrawalProcessingDays", 3);
            public static bool AllowInstantWithdrawals => GetValue("Payment:AllowInstantWithdrawals", false);

            // Límites mensuales por rol
            public static Dictionary<string, decimal> MonthlyDepositLimitByRole => new()
            {
                ["Regular"] = GetValue("Payment:Limits:Regular:MonthlyDeposit", 50000m),
                ["Premium"] = GetValue("Payment:Limits:Premium:MonthlyDeposit", 200000m),
                ["VIP"] = GetValue("Payment:Limits:VIP:MonthlyDeposit", 1000000m)
            };

            // Métodos de pago permitidos
            public static List<string> AllowedPaymentMethods =>
                GetSection("Payment:AllowedMethods").Get<List<string>>() ??
                new List<string> { "VISA", "MasterCard", "PayPal", "Skrill", "Transferencia" };

            // Comisiones por método
            public static Dictionary<string, decimal> PaymentMethodFees => new()
            {
                ["VISA"] = GetValue("Payment:Fees:VISA", 0m),
                ["MasterCard"] = GetValue("Payment:Fees:MasterCard", 0m),
                ["PayPal"] = GetValue("Payment:Fees:PayPal", 2.5m),
                ["Skrill"] = GetValue("Payment:Fees:Skrill", 1.5m),
                ["Transferencia"] = GetValue("Payment:Fees:Transferencia", 0m)
            };
        }

        /// <summary>
        /// Obtiene configuración de seguridad
        /// </summary>
        public static class SecuritySettings
        {
            public static int MaxLoginAttempts => GetValue("Security:MaxLoginAttempts", 5);
            public static int LockoutMinutes => GetValue("Security:LockoutMinutes", 30);
            public static int SessionTimeoutMinutes => GetValue("Security:SessionTimeoutMinutes", 30);
            public static bool RequireTwoFactor => GetValue("Security:RequireTwoFactor", false);
            public static bool RequireEmailConfirmation => GetValue("Security:RequireEmailConfirmation", true);
            public static int PasswordMinLength => GetValue("Security:PasswordMinLength", 8);
            public static bool RequireDigit => GetValue("Security:RequireDigit", true);
            public static bool RequireUppercase => GetValue("Security:RequireUppercase", true);
            public static bool RequireLowercase => GetValue("Security:RequireLowercase", true);
            public static bool RequireSpecialCharacter => GetValue("Security:RequireSpecialCharacter", true);
            public static int PasswordExpirationDays => GetValue("Security:PasswordExpirationDays", 90);
            public static string EncryptionKey => GetValue("Security:EncryptionKey", "DefaultKey123456");
            public static string JwtSecret => GetValue("Security:JwtSecret", "DefaultJwtSecret123456789");
            public static int JwtExpirationMinutes => GetValue("Security:JwtExpirationMinutes", 60);
        }

        /// <summary>
        /// Obtiene URLs de APIs externas
        /// </summary>
        public static class ExternalApis
        {
            public static string OddsApiUrl => GetValue("ExternalApis:OddsApi:Url", "");
            public static string OddsApiKey => GetValue("ExternalApis:OddsApi:ApiKey", "");
            public static int OddsApiTimeout => GetValue("ExternalApis:OddsApi:TimeoutSeconds", 30);

            public static string PaymentGatewayUrl => GetValue("ExternalApis:PaymentGateway:Url", "");
            public static string PaymentGatewayApiKey => GetValue("ExternalApis:PaymentGateway:ApiKey", "");
            public static string PaymentGatewaySecret => GetValue("ExternalApis:PaymentGateway:Secret", "");

            public static string SportsDataApiUrl => GetValue("ExternalApis:SportsData:Url", "");
            public static string SportsDataApiKey => GetValue("ExternalApis:SportsData:ApiKey", "");

            public static string EmailServiceUrl => GetValue("ExternalApis:EmailService:Url", "");
            public static string SendGridApiKey => GetValue("ExternalApis:SendGrid:ApiKey", "");

            public static string SmsServiceUrl => GetValue("ExternalApis:SmsService:Url", "");
            public static string TwilioAccountSid => GetValue("ExternalApis:Twilio:AccountSid", "");
            public static string TwilioAuthToken => GetValue("ExternalApis:Twilio:AuthToken", "");
        }

        /// <summary>
        /// Configuración de AWS
        /// </summary>
        public static class AwsSettings
        {
            public static string Region => GetValue("AWS:Region", "us-east-1");
            public static string AccessKey => GetValue("AWS:AccessKey", "");
            public static string SecretKey => GetValue("AWS:SecretKey", "");
            public static string S3BucketName => GetValue("AWS:S3:BucketName", "proyecto-apuestas");
            public static string S3BaseUrl => GetValue("AWS:S3:BaseUrl", "https://s3.amazonaws.com");
            public static string CloudFrontUrl => GetValue("AWS:CloudFront:Url", "");
            public static bool UseCloudFront => GetValue("AWS:CloudFront:Enabled", false);
        }

        /// <summary>
        /// Configuración de notificaciones
        /// </summary>
        public static class NotificationSettings
        {
            public static bool EnableEmailNotifications => GetValue("Notifications:Email:Enabled", true);
            public static bool EnableSmsNotifications => GetValue("Notifications:Sms:Enabled", false);
            public static bool EnablePushNotifications => GetValue("Notifications:Push:Enabled", true);
            public static int NotificationRetentionDays => GetValue("Notifications:RetentionDays", 30);

            public static List<string> CriticalNotificationTypes =>
                GetSection("Notifications:CriticalTypes").Get<List<string>>() ??
                new List<string> { "AccountLocked", "LargeWithdrawal", "SuspiciousActivity" };
        }

        /// <summary>
        /// Configuración del sistema
        /// </summary>
        public static class SystemSettings
        {
            public static string Environment => GetValue("Environment", "Production");
            public static bool IsProduction => Environment.Equals("Production", StringComparison.OrdinalIgnoreCase);
            public static bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);
            public static bool EnableSwagger => GetValue("System:EnableSwagger", IsDevelopment);
            public static bool EnableDetailedErrors => GetValue("System:EnableDetailedErrors", IsDevelopment);
            public static string DefaultCulture => GetValue("System:DefaultCulture", "es-CR");
            public static string DefaultTimezone => GetValue("System:DefaultTimezone", "Central America Standard Time");
            public static bool MaintenanceMode => GetValue("System:MaintenanceMode", false);
            public static string MaintenanceMessage => GetValue("System:MaintenanceMessage", "El sistema está en mantenimiento");
        }

        /// <summary>
        /// Configuración de caché
        /// </summary>
        public static class CacheSettings
        {
            public static bool EnableCaching => GetValue("Cache:Enabled", true);
            public static string CacheProvider => GetValue("Cache:Provider", "Memory"); // Memory, Redis
            public static string RedisConnectionString => GetValue("Cache:Redis:ConnectionString", "");
            public static int DefaultExpirationMinutes => GetValue("Cache:DefaultExpirationMinutes", 60);

            public static Dictionary<string, int> CacheExpirations => new()
            {
                ["UserProfile"] = GetValue("Cache:Expirations:UserProfile", 30),
                ["EventList"] = GetValue("Cache:Expirations:EventList", 5),
                ["Odds"] = GetValue("Cache:Expirations:Odds", 1),
                ["Statistics"] = GetValue("Cache:Expirations:Statistics", 60),
                ["Reports"] = GetValue("Cache:Expirations:Reports", 120)
            };
        }

        /// <summary>
        /// Configuración de logs
        /// </summary>
        public static class LoggingSettings
        {
            public static string LogLevel => GetValue("Logging:LogLevel:Default", "Information");
            public static bool LogToFile => GetValue("Logging:File:Enabled", true);
            public static string LogFilePath => GetValue("Logging:File:Path", "logs/app.log");
            public static bool LogToCloudWatch => GetValue("Logging:CloudWatch:Enabled", false);
            public static string CloudWatchLogGroup => GetValue("Logging:CloudWatch:LogGroup", "/aws/webapp/proyecto-apuestas");
            public static bool LogToElasticsearch => GetValue("Logging:Elasticsearch:Enabled", false);
            public static string ElasticsearchUrl => GetValue("Logging:Elasticsearch:Url", "");
        }

        /// <summary>
        /// Configuración de características (Feature Flags)
        /// </summary>
        public static class Features
        {
            public static bool EnableLiveBetting => GetValue("Features:LiveBetting", true);
            public static bool EnableCashOut => GetValue("Features:CashOut", false);
            public static bool EnableVirtualSports => GetValue("Features:VirtualSports", false);
            public static bool EnableCasino => GetValue("Features:Casino", false);
            public static bool EnablePromotions => GetValue("Features:Promotions", true);
            public static bool EnableReferralProgram => GetValue("Features:ReferralProgram", true);
            public static bool EnableMobileApp => GetValue("Features:MobileApp", false);
            public static bool EnableApiV2 => GetValue("Features:ApiV2", false);
        }

        /// <summary>
        /// Configuración de promociones y bonos
        /// </summary>
        public static class PromotionSettings
        {
            public static bool EnableWelcomeBonus => GetValue("Promotions:WelcomeBonus:Enabled", true);
            public static decimal WelcomeBonusPercentage => GetValue("Promotions:WelcomeBonus:Percentage", 100m);
            public static decimal WelcomeBonusMaxAmount => GetValue("Promotions:WelcomeBonus:MaxAmount", 50000m);
            public static int WelcomeBonusWageringRequirement => GetValue("Promotions:WelcomeBonus:WageringRequirement", 5);

            public static bool EnableReferralBonus => GetValue("Promotions:ReferralBonus:Enabled", true);
            public static decimal ReferralBonusAmount => GetValue("Promotions:ReferralBonus:Amount", 5000m);
            public static decimal ReferrerBonusAmount => GetValue("Promotions:ReferralBonus:ReferrerAmount", 10000m);

            public static bool EnableCashback => GetValue("Promotions:Cashback:Enabled", true);
            public static decimal CashbackPercentage => GetValue("Promotions:Cashback:Percentage", 10m);
            public static decimal CashbackMaxAmount => GetValue("Promotions:Cashback:MaxAmount", 100000m);

            public static List<string> RestrictedCountriesForBonuses =>
                GetSection("Promotions:RestrictedCountries").Get<List<string>>() ?? new List<string>();
        }

        /// <summary>
        /// Configuración de deportes y competiciones
        /// </summary>
        public static class SportsSettings
        {
            public static List<string> EnabledSports =>
                GetSection("Sports:Enabled").Get<List<string>>() ??
                new List<string> { "Football", "Basketball", "Tennis", "Baseball", "Boxing" };

            public static int EventsPerPage => GetValue("Sports:EventsPerPage", 20);
            public static int MaxOddsHistory => GetValue("Sports:MaxOddsHistory", 100);
            public static int LiveEventRefreshSeconds => GetValue("Sports:LiveEventRefreshSeconds", 10);

            public static Dictionary<string, int> SportEventDuration => new()
            {
                ["Football"] = GetValue("Sports:Duration:Football", 105),
                ["Basketball"] = GetValue("Sports:Duration:Basketball", 150),
                ["Tennis"] = GetValue("Sports:Duration:Tennis", 180),
                ["Baseball"] = GetValue("Sports:Duration:Baseball", 180),
                ["Boxing"] = GetValue("Sports:Duration:Boxing", 60)
            };
        }

        /// <summary>
        /// Configuración de emails (legacy - use EmailSettingsTyped instead)
        /// </summary>
        public static class EmailSettings
        {
            public static string FromEmail => GetValue("Email:From:Address", "noreply@proyectoapuestas.com");
            public static string FromName => GetValue("Email:From:Name", "Proyecto Apuestas");
            public static string SupportEmail => GetValue("Email:Support", "soporte@proyectoapuestas.com");
            public static string AdminEmail => GetValue("Email:Admin", "admin@proyectoapuestas.com");
            public static bool EnableEmailQueue => GetValue("Email:EnableQueue", true);
            public static int MaxRetryAttempts => GetValue("Email:MaxRetryAttempts", 3);
        }

        /// <summary>
        /// Configuración de SEO
        /// </summary>
        public static class SeoSettings
        {
            public static string SiteName => GetValue("SEO:SiteName", "Proyecto Apuestas");
            public static string DefaultTitle => GetValue("SEO:DefaultTitle", "Proyecto Apuestas - Apuestas Deportivas en Costa Rica");
            public static string DefaultDescription => GetValue("SEO:DefaultDescription", "La mejor plataforma de apuestas deportivas en Costa Rica");
            public static string DefaultKeywords => GetValue("SEO:DefaultKeywords", "apuestas, deportes, costa rica, futbol, casino");
            public static string DefaultImage => GetValue("SEO:DefaultImage", "/images/og-image.jpg");
            public static string TwitterHandle => GetValue("SEO:TwitterHandle", "@proyectoapuestas");
            public static string FacebookAppId => GetValue("SEO:FacebookAppId", "");
        }

        /// <summary>
        /// Obtiene toda la configuración como diccionario (útil para debugging)
        /// </summary>
        public static Dictionary<string, string> GetAllSettings()
        {
            var settings = new Dictionary<string, string>();

            // Solo en desarrollo
            if (SystemSettings.IsDevelopment)
            {
                foreach (var kvp in _configuration.AsEnumerable())
                {
                    if (!string.IsNullOrEmpty(kvp.Value))
                    {
                        // Ofuscar valores sensibles
                        var key = kvp.Key.ToLower();
                        if (key.Contains("key") || key.Contains("secret") || key.Contains("password") ||
                            key.Contains("token") || key.Contains("connectionstring"))
                        {
                            settings[kvp.Key] = "***HIDDEN***";
                        }
                        else
                        {
                            settings[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }

            return settings;
        }

        /// <summary>
        /// Valida que todas las configuraciones críticas estén presentes
        /// </summary>
        public static bool ValidateCriticalSettings()
        {
            var criticalSettings = new[]
            {
                "ConnectionStrings:DefaultConnection",
                "Security:JwtSecret",
                "Security:EncryptionKey",
                "Email:From:Address",
                "AWS:S3:BucketName"
            };

            var missingSettings = criticalSettings.Where(setting => string.IsNullOrEmpty(_configuration[setting])).ToList();

            if (missingSettings.Any())
            {
                throw new InvalidOperationException($"Missing critical settings: {string.Join(", ", missingSettings)}");
            }

            return true;
        }
    }
}