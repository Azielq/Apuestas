using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class TransactionHistoryViewModelValidator : AbstractValidator<TransactionHistoryViewModel>
    {
        public TransactionHistoryViewModelValidator()
        {
            RuleFor(x => x.PageSize)
                .InclusiveBetween(10, 100).WithMessage("El tamaño de página debe estar entre 10 y 100");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("La fecha de inicio debe ser anterior a la fecha fin")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);

            RuleFor(x => x.EndDate)
                .LessThanOrEqualTo(DateTime.Now)
                .WithMessage("La fecha fin no puede ser futura")
                .When(x => x.EndDate.HasValue);

            RuleFor(x => x.TransactionType)
                .Must(BeValidTransactionType).WithMessage("Tipo de transacción inválido")
                .When(x => !string.IsNullOrEmpty(x.TransactionType));

            RuleFor(x => x.Status)
                .Must(BeValidStatus).WithMessage("Estado inválido")
                .When(x => !string.IsNullOrEmpty(x.Status));
        }

        private bool BeValidTransactionType(string type)
        {
            var validTypes = new[] { "DEPOSIT", "WITHDRAWAL", "BET", "PAYOUT" };
            return validTypes.Contains(type);
        }

        private bool BeValidStatus(string status)
        {
            var validStatuses = new[] { "PENDING", "COMPLETED", "FAILED", "CANCELLED" };
            return validStatuses.Contains(status);
        }
    }
}
