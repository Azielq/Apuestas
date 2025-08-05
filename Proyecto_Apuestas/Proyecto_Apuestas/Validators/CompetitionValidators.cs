using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class CompetitionViewModelValidator : AbstractValidator<CompetitionViewModel>
    {
        public CompetitionViewModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre de la competición es requerido")
                .MaximumLength(100).WithMessage("El nombre no puede exceder 100 caracteres");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .WithMessage("La fecha de inicio debe ser anterior a la fecha fin")
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue);
        }
    }
}