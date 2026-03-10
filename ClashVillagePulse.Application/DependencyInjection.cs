using Microsoft.Extensions.DependencyInjection;

namespace ClashVillagePulse.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register application services here later
        // Example:
        // services.AddScoped<IVillageService, VillageService>();
        

        return services;
    }
}