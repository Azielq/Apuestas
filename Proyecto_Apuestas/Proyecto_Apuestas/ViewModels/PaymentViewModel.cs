namespace Proyecto_Apuestas.ViewModels
{
    public class PaymentMethodViewModel
    {
        public int PaymentMethodId { get; set; }
        public string ProviderName { get; set; }
        public string AccountReference { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsDefault { get; set; }
    }

    public class AddPaymentMethodViewModel
    {
        public string ProviderName { get; set; }
        public string AccountReference { get; set; }
        public bool SetAsDefault { get; set; }
    }

    public class DepositViewModel
    {
        public decimal Amount { get; set; }
        public int PaymentMethodId { get; set; }
        public List<PaymentMethodViewModel>? AvailableMethods { get; set; }
        public decimal CurrentBalance { get; set; }
    }

    public class WithdrawViewModel
    {
        public decimal Amount { get; set; }
        public int PaymentMethodId { get; set; }
        public List<PaymentMethodViewModel>? AvailableMethods { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal MinimumWithdrawal { get; set; }
    }

    public class TransactionHistoryViewModel
    {
        public List<TransactionViewModel> Transactions { get; set; }
        public decimal TotalDeposits { get; set; }
        public decimal TotalWithdrawals { get; set; }
        public decimal CurrentBalance { get; set; }

        // Paginación
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }

        // Filtros
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? TransactionType { get; set; }
        public string? Status { get; set; }
    }

    public class TransactionViewModel
    {
        public int TransactionId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; }
        public string TransactionTypeDisplay { get; set; }
        public string Status { get; set; }
        public string StatusDisplay { get; set; }
        public string PaymentMethodName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<int>? RelatedBetIds { get; set; }
    }
}