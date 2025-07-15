using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators.Admin
{
    public class UserManagementFilterValidator : AbstractValidator<UserManagementViewModel>
    {
        public UserManagementFilterValidator()
        {
            RuleFor(x => x.PageSize)
                .InclusiveBetween(10, 100).WithMessage("El tamaño de página debe estar entre 10 y 100");

            RuleFor(x => x.SearchTerm)
                .MaximumLength(50).WithMessage("El término de búsqueda es demasiado largo")
                .Matches("^[a-zA-Z0-9@.\\s]*$").WithMessage("El término de búsqueda contiene caracteres inválidos")
                .When(x => !string.IsNullOrEmpty(x.SearchTerm));
        }
    }

    public class ReportViewModelValidator : AbstractValidator<ReportViewModel>
    {
        private readonly List<string> _allowedReportTypes = new()
        {
            "UserActivity", "Revenue", "BettingStats", "PaymentSummary"
        };

        public ReportViewModelValidator()
        {
            RuleFor(x => x.ReportType)
                .NotEmpty().WithMessage("El tipo de reporte es requerido")
                .Must(BeValidReportType).WithMessage("Tipo de reporte inválido");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("La fecha de inicio es requerida")
                .LessThanOrEqualTo(x => x.EndDate).WithMessage("La fecha de inicio debe ser anterior a la fecha fin");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("La fecha fin es requerida")
                .LessThanOrEqualTo(DateTime.Now).WithMessage("La fecha fin no puede ser futura");

            RuleFor(x => x)
                .Must(x => (x.EndDate - x.StartDate).Days <= 365)
                .WithMessage("El rango de fechas no puede exceder un año");
        }

        private bool BeValidReportType(string reportType)
        {
            return _allowedReportTypes.Contains(reportType);
        }
    }
}