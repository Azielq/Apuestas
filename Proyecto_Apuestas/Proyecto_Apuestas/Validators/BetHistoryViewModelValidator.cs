using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class BetHistoryViewModelValidator : AbstractValidator<BetHistoryViewModel>
    {
        public BetHistoryViewModelValidator()
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

            RuleFor(x => x.Status)
                .Must(BeValidStatus).WithMessage("Estado inválido")
                .When(x => !string.IsNullOrEmpty(x.Status));
        }

        private bool BeValidStatus(string status)
        {
            var validStatuses = new[] { "P", "W", "L", "C" }; // Pending, Won, Lost, Cancelled
            return validStatuses.Contains(status);
        }
    }
}