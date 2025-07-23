using FluentValidation;
using Proyecto_Apuestas.ViewModels;

namespace Proyecto_Apuestas.Validators
{
    public class PaginationViewModelValidator : AbstractValidator<PaginationViewModel>
    {
        public PaginationViewModelValidator()
        {
            RuleFor(x => x.CurrentPage)
                .GreaterThan(0).WithMessage("La página actual debe ser mayor a 0")
                .LessThanOrEqualTo(x => x.TotalPages)
                .WithMessage("La página actual no puede exceder el total de páginas")
                .When(x => x.TotalPages > 0);

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100).WithMessage("El tamaño de página debe estar entre 1 y 100");

            RuleFor(x => x.TotalItems)
                .GreaterThanOrEqualTo(0).WithMessage("El total de items no puede ser negativo");
        }
    }
}
