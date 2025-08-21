using Proyecto_Apuestas.Models;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Services.Interfaces
{
    public interface IPaymentService
    {
        Task<PaymentResult> ProcessDepositAsync(DepositViewModel model);
        Task<PaymentResult> ProcessWithdrawalAsync(WithdrawViewModel model);
        Task<bool> AddPaymentMethodAsync(int userId, AddPaymentMethodViewModel model);
        Task<bool> RemovePaymentMethodAsync(int userId, int paymentMethodId);
        Task<bool> SetDefaultPaymentMethodAsync(int userId, int paymentMethodId);
        Task<List<PaymentMethodViewModel>> GetUserPaymentMethodsAsync(int userId);
        Task<PaymentMethodViewModel?> GetPaymentMethodAsync(int paymentMethodId);
        Task<TransactionHistoryViewModel> GetTransactionHistoryAsync(int userId, TransactionFilter? filter = null);
        Task<PaymentTransaction?> GetTransactionAsync(int transactionId);
        Task<bool> CreateTransactionAsync(PaymentTransactionRequest request);
        Task<bool> UpdateTransactionStatusAsync(int transactionId, string status);
        Task<Dictionary<string, decimal>> GetPaymentStatisticsAsync(int userId);
        Task<bool> ValidatePaymentMethodAsync(string provider, string accountReference);
        Task<decimal> GetMinimumWithdrawalAmount(int userId);
        Task<decimal> GetMaximumDepositAmount(int userId);
    }

    public class PaymentResult
    {
        public bool Success { get; set; }
        public int? TransactionId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TransactionReference { get; set; }
    }

    public class PaymentTransactionRequest
    {
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
        public string? Description { get; set; }
        public int? PaymentMethodId { get; set; }
        public int? RelatedBetId { get; set; }
        public List<int>? RelatedBetIds { get; set; }
    }

    public class TransactionFilter
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? TransactionType { get; set; }
        public string? Status { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
