using FluentValidation;
using Proyecto_Apuestas.Validators;
using Proyecto_Apuestas.Services;

namespace Proyecto_Apuestas.Configuration
{
    public static class ValidationConfiguration
    {
        public static IServiceCollection AddValidators(this IServiceCollection services)
        {
            services.AddValidatorsFromAssemblyContaining<LoginViewModelValidator>();
            return services;
        }
    }
}