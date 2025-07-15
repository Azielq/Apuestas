using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Proyecto_Apuestas.Data;
using Proyecto_Apuestas.Services.Interfaces;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class CreateBetViewModelValidator : AbstractValidator<CreateBetViewModel>
    {
        private readonly apuestasDbContext _context;
        private readonly IUserService _userService;

        public CreateBetViewModelValidator(apuestasDbContext context, IUserService userService)
        {
            _context = context;
            _userService = userService;

            RuleFor(x => x.EventId)
                .NotEmpty().WithMessage("Debe seleccionar un evento")
                .MustAsync(BeValidEvent).WithMessage("El evento seleccionado no es válido")
                .MustAsync(EventNotStarted).WithMessage("No se puede apostar en eventos que ya han comenzado");

            RuleFor(x => x.TeamId)
                .NotEmpty().WithMessage("Debe seleccionar un equipo")
                .MustAsync(BeValidTeamForEvent).WithMessage("El equipo seleccionado no participa en este evento");

            RuleFor(x => x.Stake)
                .NotEmpty().WithMessage("El monto es requerido")
                .GreaterThan(0).WithMessage("El monto debe ser mayor a 0")
                .LessThanOrEqualTo(10000).WithMessage("El monto máximo por apuesta es 10,000")
                .PrecisionScale(10, 2, true).WithMessage("El monto solo puede tener 2 decimales")
                .MustAsync(UserHasEnoughBalance).WithMessage("Saldo insuficiente para realizar esta apuesta")
                .MustAsync(NotExceedDailyLimit).WithMessage("Ha excedido el límite diario de apuestas");

            RuleFor(x => x.Odds)
                .GreaterThan(1).WithMessage("Las cuotas deben ser mayores a 1")
                .LessThanOrEqualTo(1000).WithMessage("Las cuotas parecen incorrectas");
        }

        private async Task<bool> BeValidEvent(int eventId, CancellationToken cancellationToken)
        {
            return await _context.Events
                .AnyAsync(e => e.EventId == eventId, cancellationToken);
        }

        private async Task<bool> EventNotStarted(CreateBetViewModel model, int eventId, CancellationToken cancellationToken)
        {
            var eventEntity = await _context.Events
                .FirstOrDefaultAsync(e => e.EventId == eventId, cancellationToken);

            return eventEntity != null && eventEntity.Date > DateTime.Now.AddMinutes(5);
        }

        private async Task<bool> BeValidTeamForEvent(CreateBetViewModel model, int teamId, CancellationToken cancellationToken)
        {
            return await _context.EventHasTeams
                .AnyAsync(et => et.EventId == model.EventId && et.TeamId == teamId, cancellationToken);
        }

        private async Task<bool> UserHasEnoughBalance(CreateBetViewModel model, decimal stake, CancellationToken cancellationToken)
        {
            var userId = _userService.GetCurrentUserId();
            var user = await _context.UserAccounts
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            return user != null && user.CreditBalance >= stake;
        }

        private async Task<bool> NotExceedDailyLimit(CreateBetViewModel model, decimal stake, CancellationToken cancellationToken)
        {
            var userId = _userService.GetCurrentUserId();
            var today = DateTime.Today;

            var todayTotal = await _context.Bets
                .Where(b => b.Users.Any(u => u.UserId == userId) &&
                           b.CreatedAt >= today)
                .SumAsync(b => b.Stake, cancellationToken);

            return (todayTotal + stake) <= 5000; // Límite diario de 5000
        }
    }

    public class BetSlipViewModelValidator : AbstractValidator<BetSlipViewModel>
    {
        public BetSlipViewModelValidator()
        {
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("Debe agregar al menos una apuesta")
                .Must(items => items.Count <= 10).WithMessage("Máximo 10 apuestas por boleto");

            RuleFor(x => x.TotalStake)
                .LessThanOrEqualTo(x => x.UserBalance)
                .WithMessage("El monto total excede su saldo disponible");

            RuleForEach(x => x.Items).SetValidator(new BetSlipItemValidator());
        }
    }

    public class BetSlipItemValidator : AbstractValidator<BetSlipItemViewModel>
    {
        public BetSlipItemValidator()
        {
            RuleFor(x => x.Stake)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a 0")
                .LessThanOrEqualTo(10000).WithMessage("El monto máximo por apuesta es 10,000");

            RuleFor(x => x.Odds)
                .GreaterThan(1).WithMessage("Las cuotas deben ser mayores a 1");
        }
    }
}
