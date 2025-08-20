namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IStripeService
    {
        Task<string> CreateCheckoutSessionAsync(string priceId, string originBaseUrl, string userId, string packageId);
        Task<string> CreateCheckoutSessionInlinePriceAsync(long unitAmount, string currency, string productName, string originBaseUrl, string userId, string packageId); // NUEVO
        Task<Stripe.Refund?> RefundPaymentAsync(string paymentIntentId, decimal? amount = null);
    }

}
