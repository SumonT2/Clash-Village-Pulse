using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Infrastructure.Database;
using ClashVillagePulse.Infrastructure.Services;
using ClashVillagePulse.Infrastructure.StaticData;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using ClashVillagePulse.Infrastructure.StaticData.Processors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClashVillagePulse.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IVillageUploadService, VillageUploadService>();
        services.AddScoped<IVillageQueryService, VillageQueryService>();
        services.AddScoped<IStaticDataGenerationService, StaticDataGenerationService>();
        services.AddHttpClient<StaticDataDownloader>();
        services.AddScoped<CsvParser>();

        services.AddScoped<IStaticDataTargetProcessor, BuildingsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, LocalizationProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, TownHallLevelsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, CharactersProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, HeroesProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, SpellsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, PetsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, EquipmentProcessor>();
        services.AddScoped<BuildingsCsvMapper>();
        services.AddScoped<StaticDataDecompressor>();
        services.AddScoped<StaticDataRunTracker>();
        services.AddScoped<StaticDataProcessorRegistry>();

        return services;
    }
}