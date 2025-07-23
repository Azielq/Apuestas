using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class TeamViewModelValidator : AbstractValidator<TeamViewModel>
    {
        public TeamViewModelValidator()
        {
            RuleFor(x => x.TeamName)
                .NotEmpty().WithMessage("El nombre del equipo es requerido")
                .MaximumLength(45).WithMessage("El nombre no puede exceder 45 caracteres");

            RuleFor(x => x.TeamWinPercent)
                .InclusiveBetween(0, 100).WithMessage("El porcentaje de victorias debe estar entre 0 y 100");

            RuleFor(x => x.TeamDrawPercent)
                .InclusiveBetween(0, 100).WithMessage("El porcentaje de empates debe estar entre 0 y 100");

            RuleFor(x => x)
                .Must(x => x.TeamWinPercent + x.TeamDrawPercent <= 100)
                .WithMessage("La suma de porcentajes de victoria y empate no puede exceder 100%")
                .WithName("Porcentajes");
        }
    }
}