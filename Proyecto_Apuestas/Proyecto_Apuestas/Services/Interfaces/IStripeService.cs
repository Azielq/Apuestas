using Stripe;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IStripeService
    {
        // Usa un Price creado en Stripe
        Task<string> CreateCheckoutSessionAsync(string priceId, string originBaseUrl, string userId, string packageId);

        // Crea el precio "inline" (unitAmount en minor units). currency: "crc"
        Task<string> CreateCheckoutSessionInlinePriceAsync(
            long unitAmount, string currency, string productName,
            string originBaseUrl, string userId, string packageId);

        Task<Refund?> RefundPaymentAsync(string paymentIntentId, decimal? amount = null);
    }
}
