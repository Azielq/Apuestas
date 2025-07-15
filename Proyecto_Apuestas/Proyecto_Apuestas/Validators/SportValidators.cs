using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class SportListViewModelValidator : AbstractValidator<SportListViewModel>
    {
        public SportListViewModelValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("El nombre del deporte es requerido")
                .MaximumLength(45).WithMessage("El nombre no puede exceder 45 caracteres");
        }
    }
}