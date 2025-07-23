namespace Proyecto_Apuestas.Configuration
{
    public class EmailSettings
    {
        public const string SectionName = "Email";
        
        public string FromEmail { get; set; } = "noreply@bet506.com";
        public string FromName { get; set; } = "Bet506 - Apuestas Deportivas";
        public string SupportEmail { get; set; } = "support@bet506.com";
        public string BaseUrl { get; set; } = "https://bet506.com";
        public SendGridSettings SendGrid { get; set; } = new();
    }

    public class SendGridSettings
    {
        public string ApiKey { get; set; } = string.Empty;
    }

    public class PaymentSettings
    {
        public const string SectionName = "Payment";
        
        public StripeSettings Stripe { get; set; } = new();
    }

    public class StripeSettings
    {
        public string PublicKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string WebhookSecret { get; set; } = string.Empty;
    }

    public class ApplicationSettings
    {
        public const string SectionName = "Application";
        
        public string Name { get; set; } = "Bet506";
        public string Version { get; set; } = "1.0.0";
        public string Environment { get; set; } = "Development";
        public bool EnableDetailedErrors { get; set; } = false;
        public bool EnableSwagger { get; set; } = false;
        public BettingSettings Betting { get; set; } = new();
    }

    public class BettingSettings
    {
        public decimal MinimumBet { get; set; } = 1000; // ?1,000
        public decimal MaximumBet { get; set; } = 500000; // ?500,000
        public decimal DailyBetLimit { get; set; } = 2000000; // ?2,000,000
        public int MaximumBetsPerSlip { get; set; } = 10;
        public TimeSpan BetSlipExpiration { get; set; } = TimeSpan.FromMinutes(30);
        public bool AllowLiveBetting { get; set; } = true;
    }
}