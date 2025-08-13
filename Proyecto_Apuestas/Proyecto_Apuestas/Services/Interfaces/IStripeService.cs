using Stripe;
using System.Threading.Tasks;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IStripeService
    {
        Task<string> CreatePaymentIntentAsync(decimal amount, string currency = "usd");
        Task<PaymentIntent?> RetrievePaymentIntentAsync(string intentId);
        Task<Refund?> RefundPaymentAsync(string paymentIntentId, decimal? amount = null);
    }
}
