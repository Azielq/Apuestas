using FluentValidation;
using Proyecto_Apuestas.ViewModels;
namespace Proyecto_Apuestas.Validators
{
    public class UpdateProfileViewModelValidator : AbstractValidator<UpdateProfileViewModel>
    {
        public UpdateProfileViewModelValidator()
        {
            RuleFor(x => x.FirstName)
                .MaximumLength(80).WithMessage("El nombre no puede exceder 80 caracteres")
                .Matches("^[a-zA-ZáéíóúÁÉÍÓÚñÑ\\s]+$").WithMessage("El nombre solo puede contener letras")
                .When(x => !string.IsNullOrEmpty(x.FirstName));

            RuleFor(x => x.PrimerApellido)
                .MaximumLength(45).WithMessage("El primer apellido no puede exceder 45 caracteres")
                .Matches("^[a-zA-ZáéíóúÁÉÍÓÚñÑ\\s]+$").WithMessage("El apellido solo puede contener letras")
                .When(x => !string.IsNullOrEmpty(x.PrimerApellido));

            RuleFor(x => x.SegundoApellido)
                .MaximumLength(45).WithMessage("El segundo apellido no puede exceder 45 caracteres")
                .Matches("^[a-zA-ZáéíóúÁÉÍÓÚñÑ\\s]+$").WithMessage("El apellido solo puede contener letras")
                .When(x => !string.IsNullOrEmpty(x.SegundoApellido));

            RuleFor(x => x.PhoneNumber)
                .Matches(@"^\+?[1-9]\d{1,14}$").WithMessage("Número de teléfono inválido")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            RuleFor(x => x.Country)
                .MaximumLength(45).WithMessage("El país no puede exceder 45 caracteres")
                .When(x => !string.IsNullOrEmpty(x.Country));
        }
    }
}
