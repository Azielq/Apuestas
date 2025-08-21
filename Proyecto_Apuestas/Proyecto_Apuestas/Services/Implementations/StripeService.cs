using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Proyecto_Apuestas.Services.Interfaces;
using Stripe;
using Stripe.Checkout;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class StripeService : IStripeService
    {
        private readonly string _secretKey;
        private readonly ILogger<StripeService> _logger;

        public StripeService(IConfiguration configuration, ILogger<StripeService> logger)
        {
            _secretKey = configuration["Payment:Stripe:SecretKey"] ?? string.Empty;
            _logger = logger;
            StripeConfiguration.ApiKey = _secretKey;
        }

        public async Task<string> CreateCheckoutSessionAsync(
            string priceId, string originBaseUrl, string userId, string packageId)
        {
            var options = new SessionCreateOptions
            {
                UiMode = "embedded",
                Mode = "payment",
                ReturnUrl = $"{originBaseUrl}/StripeCheckout/Success?session_id={{CHECKOUT_SESSION_ID}}",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Price = priceId,
                        Quantity = 1
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["packageId"] = packageId
                },
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = false },
                PaymentMethodTypes = new List<string> { "card" }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            if (string.IsNullOrWhiteSpace(session.ClientSecret))
                throw new Exception("Stripe no devolvió clientSecret.");

            _logger.LogInformation("Embedded Checkout Session creada: {SessionId}", session.Id);
            return session.ClientSecret!;
        }

        public async Task<string> CreateCheckoutSessionInlinePriceAsync(
            long unitAmount, string currency, string productName,
            string originBaseUrl, string userId, string packageId)
        {
            var options = new SessionCreateOptions
            {
                UiMode = "embedded",
                Mode = "payment",
                ReturnUrl = $"{originBaseUrl}/StripeCheckout/Success?session_id={{CHECKOUT_SESSION_ID}}",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = currency,            // "crc"
                            UnitAmount = unitAmount,        // céntimos
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = productName
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["packageId"] = packageId
                },
                AutomaticTax = new SessionAutomaticTaxOptions { Enabled = false },
                PaymentMethodTypes = new List<string> { "card" }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            if (string.IsNullOrWhiteSpace(session.ClientSecret))
                throw new Exception("Stripe no devolvió clientSecret.");

            _logger.LogInformation("Embedded Checkout (inline price) creada: {SessionId}", session.Id);
            return session.ClientSecret!;
        }

        public async Task<Refund?> RefundPaymentAsync(string paymentIntentId, decimal? amount = null)
        {
            var opts = new RefundCreateOptions
            {
                PaymentIntent = paymentIntentId
            };

            // Si se envía monto, convertir a "minor units" (centimos)
            if (amount.HasValue)
            {
                // CRC tiene 2 decimales
                var minor = (long)Math.Round(amount.Value * 100m, 0, MidpointRounding.AwayFromZero);
                opts.Amount = minor;
            }

            var service = new RefundService();
            var refund = await service.CreateAsync(opts);
            _logger.LogInformation("Refund creado: {RefundId} para PI {PI}", refund.Id, paymentIntentId);
            return refund;
        }
    }
}
