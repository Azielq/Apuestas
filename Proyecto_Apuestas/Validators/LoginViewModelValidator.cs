using Proyecto_Apuestas.Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.ViewModels;
using Proyecto_Apuestas.Helpers;

namespace Proyecto_Apuestas.Validators
{
    public class LoginViewModelValidator : AbstractValidator<LoginViewModel>
    {
        public LoginViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El formato del email no es v�lido")
                .MaximumLength(45).WithMessage("El email no puede exceder 45 caracteres");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contrase�a es requerida")
                .MinimumLength(6).WithMessage("La contrase�a debe tener al menos 6 caracteres");
        }
    }

    public class RegisterViewModelValidator : AbstractValidator<RegisterViewModel>
    {
        private readonly apuestasDbContext _context;

        public RegisterViewModelValidator(apuestasDbContext context)
        {
            _context = context;

            RuleFor(x => x.UserName)
                .NotEmpty().WithMessage("El nombre de usuario es requerido")
                .Length(3, 45).WithMessage("El nombre de usuario debe tener entre 3 y 45 caracteres")
                .Matches("^[a-zA-Z0-9_]+$").WithMessage("El nombre de usuario solo puede contener letras, n�meros y gui�n bajo")
                .MustAsync(BeUniqueUserName).WithMessage("Este nombre de usuario ya est� en uso");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El formato del email no es v�lido")
                .MaximumLength(45).WithMessage("El email no puede exceder 45 caracteres")
                .MustAsync(BeUniqueEmail).WithMessage("Este email ya est� registrado");

            // ? CONTRASE�A MENOS ESTRICTA - Solo longitud m�nima por ahora
            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("La contrase�a es requerida")
                .MinimumLength(8).WithMessage("La contrase�a debe tener al menos 8 caracteres");
                // ? COMENTADO TEMPORALMENTE - Validaciones muy estrictas
                //.Matches("[A-Z]").WithMessage("La contrase�a debe contener al menos una may�scula")
                //.Matches("[a-z]").WithMessage("La contrase�a debe contener al menos una min�scula")
                //.Matches("[0-9]").WithMessage("La contrase�a debe contener al menos un n�mero")
                //.Matches("[^a-zA-Z0-9]").WithMessage("La contrase�a debe contener al menos un car�cter especial");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.Password).WithMessage("Las contrase�as no coinciden");

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage("El nombre es requerido")
                .MaximumLength(80).WithMessage("El nombre no puede exceder 80 caracteres")
                .Matches("^[a-zA-Z������������\\s]+$").WithMessage("El nombre solo puede contener letras");

            RuleFor(x => x.PrimerApellido)
                .NotEmpty().WithMessage("El primer apellido es requerido")
                .MaximumLength(45).WithMessage("El primer apellido no puede exceder 45 caracteres")
                .Matches("^[a-zA-Z������������\\s]+$").WithMessage("El apellido solo puede contener letras");

            RuleFor(x => x.SegundoApellido)
                .MaximumLength(45).WithMessage("El segundo apellido no puede exceder 45 caracteres")
                .Matches("^[a-zA-Z������������\\s]+$").WithMessage("El apellido solo puede contener letras")
                .When(x => !string.IsNullOrEmpty(x.SegundoApellido));

            // ? TEL�FONO OPCIONAL - Validaci�n solo si no est� vac�o
            RuleFor(x => x.PhoneNumber)
                .MustBeValidCostaRicanPhone()
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber?.Trim()));

            // ? FECHA DE NACIMIENTO OPCIONAL
            RuleFor(x => x.BirthDate)
                .Must(BeAtLeast18YearsOld).WithMessage("Debes tener al menos 18 a�os para registrarte")
                .When(x => x.BirthDate.HasValue);

            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("Debes seleccionar un pa�s")
                .MaximumLength(45).WithMessage("El pa�s no puede exceder 45 caracteres")
                .Must(BeValidCountry).WithMessage("Pa�s no v�lido");

            // ? T�RMINOS - Validaci�n m�s flexible
            RuleFor(x => x.AcceptTerms)
                .Must(x => x == true).WithMessage("Debes aceptar los t�rminos y condiciones")
                .When(x => x.AcceptTerms.HasValue);
        }

        private async Task<bool> BeUniqueUserName(string userName, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(userName)) return true; // Ya se valida en NotEmpty
            return !await _context.UserAccounts
                .AnyAsync(x => x.UserName == userName, cancellationToken);
        }

        private async Task<bool> BeUniqueEmail(string email, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(email)) return true; // Ya se valida en NotEmpty
            return !await _context.UserAccounts
                .AnyAsync(x => x.Email == email, cancellationToken);
        }

        private bool BeAtLeast18YearsOld(DateOnly? birthDate)
        {
            if (!birthDate.HasValue) return true;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - birthDate.Value.Year;
            if (birthDate.Value > today.AddYears(-age)) age--;

            return age >= 18;
        }

        private bool BeValidCountry(string country)
        {
            if (string.IsNullOrEmpty(country)) return false;
            var validCountries = new[] { "Costa Rica", "Guatemala", "Honduras", "El Salvador", "Nicaragua", "Panam�" };
            return validCountries.Contains(country);
        }
    }

    public class ChangePasswordViewModelValidator : AbstractValidator<ChangePasswordViewModel>
    {
        public ChangePasswordViewModelValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage("La contrase�a actual es requerida");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contrase�a es requerida")
                .MinimumLength(8).WithMessage("La contrase�a debe tener al menos 8 caracteres")
                .Matches("[A-Z]").WithMessage("La contrase�a debe contener al menos una may�scula")
                .Matches("[a-z]").WithMessage("La contrase�a debe contener al menos una min�scula")
                .Matches("[0-9]").WithMessage("La contrase�a debe contener al menos un n�mero")
                .Matches("[^a-zA-Z0-9]").WithMessage("La contrase�a debe contener al menos un car�cter especial")
                .NotEqual(x => x.CurrentPassword).WithMessage("La nueva contrase�a debe ser diferente a la actual");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage("Las contrase�as no coinciden");
        }
    }

    public class ForgotPasswordViewModelValidator : AbstractValidator<ForgotPasswordViewModel>
    {
        public ForgotPasswordViewModelValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El formato del email no es v�lido")
                .MaximumLength(100).WithMessage("El email no puede exceder 100 caracteres");
        }
    }

    public class ResetPasswordViewModelValidator : AbstractValidator<ResetPasswordViewModel>
    {
        public ResetPasswordViewModelValidator()
        {
            RuleFor(x => x.Token)
                .NotEmpty().WithMessage("Token de validaci�n requerido");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("El email es requerido")
                .EmailAddress().WithMessage("El formato del email no es v�lido");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("La nueva contrase�a es requerida")
                .MinimumLength(8).WithMessage("La contrase�a debe tener al menos 8 caracteres")
                .Matches("[A-Z]").WithMessage("La contrase�a debe contener al menos una may�scula")
                .Matches("[a-z]").WithMessage("La contrase�a debe contener al menos una min�scula")
                .Matches("[0-9]").WithMessage("La contrase�a debe contener al menos un n�mero")
                .Matches("[^a-zA-Z0-9]").WithMessage("La contrase�a debe contener al menos un car�cter especial");

            RuleFor(x => x.ConfirmPassword)
                .Equal(x => x.NewPassword).WithMessage("Las contrase�as no coinciden");
        }
    }
}