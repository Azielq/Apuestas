using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class EventFilterValidator : AbstractValidator<UpcomingEventsViewModel>
    {
        public EventFilterValidator()
        {
            RuleFor(x => x.SearchTerm)
                .MaximumLength(100).WithMessage("El término de búsqueda es demasiado largo")
                .Matches("^[a-zA-Z0-9\\s-]*$").WithMessage("El término de búsqueda contiene caracteres inválidos")
                .When(x => !string.IsNullOrEmpty(x.SearchTerm));
        }
    }
}