using Microsoft.Extensions.Configuration;
using Proyecto_Apuestas.Services.Interfaces;
using Stripe;
using System;
using System.Threading.Tasks;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class StripeService : IStripeService
    {
        private readonly IConfiguration _configuration;
        private readonly string _secretKey;
        private readonly string _publicKey;
        private readonly ILogger<StripeService> _logger;


        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            _secretKey = _configuration["Payment:Stripe:SecretKey"];
            _publicKey = _configuration["Payment:Stripe:PublicKey"];

            if (string.IsNullOrWhiteSpace(_secretKey))
            {
                _logger.LogError("Stripe SecretKey is missing or empty.");
                throw new ArgumentNullException("Stripe SecretKey is missing or empty in configuration");
            }

            if (!_secretKey.StartsWith("sk_test_") && !_secretKey.StartsWith("sk_live_"))
            {
                _logger.LogError("Stripe SecretKey format is invalid: {SecretKey}", _secretKey);
                throw new ArgumentException("Stripe SecretKey format is invalid");
            }

            if (string.IsNullOrWhiteSpace(_publicKey))
            {
                _logger.LogError("Stripe PublicKey is missing or empty.");
                throw new ArgumentNullException("Stripe PublicKey is missing or empty in configuration");
            }

            if (!_publicKey.StartsWith("pk_test_") && !_publicKey.StartsWith("pk_live_"))
            {
                _logger.LogError("Stripe SecretKey format is invalid: {SecretKey}", _secretKey);
                throw new ArgumentException("Stripe PublicKey format is invalid");
            }

        }

        public async Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd")
        {
            var client = new StripeClient(_secretKey); // <- crea cliente con la clave correcta

            var options = new PaymentIntentCreateOptions
            {
                Amount = (long)(amount * 100),
                Currency = currency,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                }
            };

            var service = new PaymentIntentService(client); // <- usa el cliente
            var intent = await service.CreateAsync(options);
            return intent.ClientSecret;
        }


        public async Task<PaymentIntent?> RetrievePaymentIntentAsync(string intentId)
        {
            var service = new PaymentIntentService();
            try
            {
                return await service.GetAsync(intentId);
            }
            catch (StripeException)
            {
                return null;
            }
        }

        public async Task<Refund?> RefundPaymentAsync(string paymentIntentId, decimal? amount = null)
        {
            var service = new RefundService();

            var options = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            };

            if (amount.HasValue)
            {
                options.Amount = (long)(amount.Value * 100);
            }

            try
            {
                return await service.CreateAsync(options);
            }
            catch (StripeException)
            {
                return null;
            }
        }
    }
}
