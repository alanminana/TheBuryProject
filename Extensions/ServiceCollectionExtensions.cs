using TheBuryProject.Services;
using TheBuryProject.Services.Validators;

namespace TheBuryProject.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddVentaServices(this IServiceCollection services)
        {
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddScoped<IFinancialCalculationService, FinancialCalculationService>();
            services.AddScoped<IVentaValidator, VentaValidator>();
            services.AddScoped<VentaNumberGenerator>();

            return services;
        }
    }
}