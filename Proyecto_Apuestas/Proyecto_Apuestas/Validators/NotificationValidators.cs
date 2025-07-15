using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class NotificationListViewModelValidator : AbstractValidator<NotificationListViewModel>
    {
        public NotificationListViewModelValidator()
        {
            RuleFor(x => x.PageNumber)
                .GreaterThan(0).WithMessage("El número de página debe ser mayor a 0");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(10, 50).WithMessage("El tamaño de página debe estar entre 10 y 50");
        }
    }
}