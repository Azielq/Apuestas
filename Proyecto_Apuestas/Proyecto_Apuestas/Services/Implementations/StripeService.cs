// Services/Implementations/StripeService.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Proyecto_Apuestas.Services.Interfaces;
using Stripe;
using Stripe.Checkout;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Proyecto_Apuestas.Services.Implementations
{
    public class StripeService : IStripeService
    {
        private readonly string _secretKey;
        private readonly ILogger<StripeService> _logger;

        public StripeService(IConfiguration cfg, ILogger<StripeService> logger)
        {
            _logger = logger;
            _secretKey = cfg["Payment:Stripe:SecretKey"]
                ?? throw new ArgumentNullException("Stripe SecretKey missing");
            if (!_secretKey.StartsWith("sk_test_") && !_secretKey.StartsWith("sk_live_"))
                throw new ArgumentException("Stripe SecretKey format is invalid");
        }

        public async Task<string> CreateCheckoutSessionAsync(
            string priceId,
            string originBaseUrl,
            string userId,
            string packageId)
        {
            if (string.IsNullOrWhiteSpace(priceId) || !priceId.StartsWith("price_"))
                throw new ArgumentException("PriceId invalido.");
            if (string.IsNullOrWhiteSpace(originBaseUrl))
                throw new ArgumentException("Origin invalido.");

            StripeConfiguration.ApiKey = _secretKey;

            var options = new SessionCreateOptions
            {
                UiMode = "embedded",
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions { Price = priceId, Quantity = 1 }
                },
                ReturnUrl = $"{originBaseUrl}/StripeCheckout/Success?session_id={{CHECKOUT_SESSION_ID}}",

                // metadata para acreditacion server-side
                ClientReferenceId = userId,
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["packageId"] = packageId
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            if (string.IsNullOrWhiteSpace(session.ClientSecret))
                throw new Exception("Stripe no devolvio clientSecret.");

            _logger.LogInformation("Embedded Checkout Session creada: {SessionId}", session.Id);
            return session.ClientSecret!;
        }

        public async Task<Refund?> RefundPaymentAsync(string paymentIntentId, decimal? amount = null)
        {
            StripeConfiguration.ApiKey = _secretKey;
            var svc = new RefundService();
            var opts = new RefundCreateOptions { PaymentIntent = paymentIntentId };
            if (amount.HasValue) opts.Amount = (long)(amount.Value * 100);
            try { return await svc.CreateAsync(opts); }
            catch (StripeException ex) { _logger.LogError(ex, "Error creando refund"); return null; }
        }
        // Services/Implementations/StripeService.cs
        public async Task<string> CreateCheckoutSessionInlinePriceAsync(
            long unitAmount, string currency, string productName,
            string originBaseUrl, string userId, string packageId)
        {
            StripeConfiguration.ApiKey = _secretKey;

            var options = new SessionCreateOptions
            {
                UiMode = "embedded",
                Mode = "payment",
                LineItems = new List<SessionLineItemOptions>
        {
            new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = currency,
                    UnitAmount = unitAmount, // en centavos
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = productName
                    }
                },
                Quantity = 1
            }
        },
                ReturnUrl = $"{originBaseUrl}/StripeCheckout/Success?session_id={{CHECKOUT_SESSION_ID}}",
                ClientReferenceId = userId,
                Metadata = new Dictionary<string, string>
                {
                    ["userId"] = userId,
                    ["packageId"] = packageId
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            if (string.IsNullOrWhiteSpace(session.ClientSecret))
                throw new Exception("Stripe no devolvio clientSecret.");

            _logger.LogInformation("Embedded Checkout Session (inline price) creada: {SessionId}", session.Id);
            return session.ClientSecret!;
        }

    }
}
