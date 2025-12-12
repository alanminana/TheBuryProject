using TheBuryProject.Services;
using TheBuryProject.Services.Interfaces;
using TheBuryProject.Services.Validators;

namespace TheBuryProject.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCoreServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }

        public static IServiceCollection AddVentaServices(this IServiceCollection services)
        {
            services.AddScoped<IFinancialCalculationService, FinancialCalculationService>();
            services.AddScoped<IPrequalificationService, PrequalificationService>();
            services.AddScoped<IVentaValidator, VentaValidator>();
            services.AddScoped<VentaNumberGenerator>();

            return services;
        }
    }
}