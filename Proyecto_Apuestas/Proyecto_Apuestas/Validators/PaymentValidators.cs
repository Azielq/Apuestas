using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.ViewModels;
using Proyecto_Apuestas.Services.Interfaces;

namespace Proyecto_Apuestas.Validators
{
    public class AddPaymentMethodViewModelValidator : AbstractValidator<AddPaymentMethodViewModel>
    {
        private readonly List<string> _allowedProviders = new() { "VISA", "MasterCard", "PayPal", "Sinpe", "Transferencia" };

        public AddPaymentMethodViewModelValidator()
        {
            RuleFor(x => x.ProviderName)
                .NotEmpty().WithMessage("El proveedor es requerido")
                .Must(BeAllowedProvider).WithMessage("Proveedor de pago no válido");

            RuleFor(x => x.AccountReference)
                .NotEmpty().WithMessage("La referencia de cuenta es requerida")
                .MaximumLength(100).WithMessage("La referencia no puede exceder 100 caracteres");

            // VISA y MasterCard: número de tarjeta (crédito o débito)
            When(x => x.ProviderName == "VISA" || x.ProviderName == "MasterCard", () => {
                RuleFor(x => x.AccountReference)
                    .CreditCard().WithMessage("Número de tarjeta inválido");
            });

            // PayPal: email
            When(x => x.ProviderName == "PayPal", () => {
                RuleFor(x => x.AccountReference)
                    .EmailAddress().WithMessage("Email inválido para PayPal");
            });

            // Sinpe: número de teléfono costarricense (8 dígitos)
            When(x => x.ProviderName == "Sinpe", () => {
                RuleFor(x => x.AccountReference)
                    .Matches(@"^\d{8}$").WithMessage("El número Sinpe debe tener 8 dígitos");
            });

        }

        private bool BeAllowedProvider(string provider)
        {
            return _allowedProviders.Contains(provider);
        }
    }

    public class DepositViewModelValidator : AbstractValidator<DepositViewModel>
    {
        private readonly apuestasDbContext _context;
        private readonly IUserService _userService;

        public DepositViewModelValidator(apuestasDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;

            RuleFor(x => x.Amount)
                .NotEmpty().WithMessage("El monto es requerido")
                .GreaterThanOrEqualTo(10).WithMessage("El monto mínimo de depósito es 10")
                .LessThanOrEqualTo(10000).WithMessage("El monto máximo de depósito es 10,000")
                .PrecisionScale(10, 2, true).WithMessage("El monto solo puede tener 2 decimales");

            RuleFor(x => x.PaymentMethodId)
                .NotEmpty().WithMessage("Debe seleccionar un método de pago")
                .MustAsync(BeValidPaymentMethod).WithMessage("Método de pago inválido");

            RuleFor(x => x)
                .MustAsync(NotExceedMonthlyDepositLimit)
                .WithMessage("Ha excedido el límite mensual de depósitos");
        }

        private async Task<bool> BeValidPaymentMethod(int paymentMethodId, CancellationToken cancellationToken)
        {
            var userId = _userService.GetCurrentUserId();
            return await _context.PaymentMethods
                .AnyAsync(pm => pm.PaymentMethodId == paymentMethodId &&
                               pm.UserId == userId &&
                               pm.IsActive == true, cancellationToken);
        }

        private async Task<bool> NotExceedMonthlyDepositLimit(DepositViewModel model, CancellationToken cancellationToken)
        {
            var userId = _userService.GetCurrentUserId();
            var firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            var monthlyTotal = await _context.PaymentTransactions
                .Where(pt => pt.UserId == userId &&
                            pt.TransactionType == "DEPOSIT" &&
                            pt.CreatedAt >= firstDayOfMonth &&
                            pt.Status == "COMPLETED")
                .SumAsync(pt => pt.Amount, cancellationToken);

            return (monthlyTotal + model.Amount) <= 50000; // Límite mensual de 50,000
        }
    }

    public class WithdrawViewModelValidator : AbstractValidator<WithdrawViewModel>
    {
        private readonly apuestasDbContext _context;
        private readonly IUserService _userService;

        public WithdrawViewModelValidator(apuestasDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;

            RuleFor(x => x.Amount)
                .NotEmpty().WithMessage("El monto es requerido")
                .GreaterThanOrEqualTo(x => x.MinimumWithdrawal)
                    .WithMessage(x => $"El monto mínimo de retiro es {x.MinimumWithdrawal}")
                .LessThanOrEqualTo(10000).WithMessage("El monto máximo de retiro es 10,000")
                .LessThanOrEqualTo(x => x.CurrentBalance)
                    .WithMessage("El monto excede su saldo disponible")
                .PrecisionScale(10, 2, true).WithMessage("El monto solo puede tener 2 decimales");

            RuleFor(x => x.PaymentMethodId)
                .NotEmpty().WithMessage("Debe seleccionar un método de pago")
                .MustAsync(BeValidPaymentMethod).WithMessage("Método de pago inválido");

            RuleFor(x => x)
                .MustAsync(NoActiveBets)
                .WithMessage("No puede retirar mientras tenga apuestas activas")
                .MustAsync(AccountVerified)
                .WithMessage("Su cuenta debe estar verificada para realizar retiros");
        }

        private async Task<bool> BeValidPaymentMethod(int paymentMethodId, CancellationToken cancellationToken)
        {
            var userId = _userService.GetCurrentUserId();
            return await _context.PaymentMethods
                .AnyAsync(pm => pm.PaymentMethodId == paymentMethodId &&
                               pm.UserId == userId &&
                               pm.IsActive == true, cancellationToken);
        }

        private async Task<bool> NoActiveBets(WithdrawViewModel model, CancellationToken cancellationToken)
        {
            var userId = _userService.GetCurrentUserId();
            return !await _context.Bets
                .AnyAsync(b => b.Users.Any(u => u.UserId == userId) &&
                              b.BetStatus == "P", cancellationToken);
        }

        private async Task<bool> AccountVerified(WithdrawViewModel model, CancellationToken cancellationToken)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _context.UserAccounts
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            // Asumiendo que se tiene algún campo o lógica para verificación
            return user != null && user.IsActive == true;
        }
    }
}