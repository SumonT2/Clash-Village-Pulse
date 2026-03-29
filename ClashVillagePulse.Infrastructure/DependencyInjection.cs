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
        services.AddHttpClient<FankitImageCrawler>();
        services.AddScoped<CsvParser>();

        services.AddScoped<IStaticDataTargetProcessor, BuildingsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, LocalizationProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, TownHallLevelsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, CharactersProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, HeroesProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, SpellsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, PetsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, EquipmentProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, TrapsProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, GuardiansProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, HelpersProcessor>();
        services.AddScoped<IStaticDataTargetProcessor, FankitImagesProcessor>();
        services.AddScoped<TrapsCsvMapper>();
        services.AddScoped<BuildingsCsvMapper>();
        services.AddScoped<StaticDataDecompressor>();
        services.AddScoped<StaticDataRunTracker>();
        services.AddScoped<StaticDataProcessorRegistry>();
        services.AddScoped<SpellsCsvMapper>();
        services.AddScoped<CharactersCsvMapper>();
        services.AddScoped<LocalizationTextsCsvMapper>();
        services.AddScoped<HeroesCsvMapper>();
        services.AddScoped<PetsCsvMapper>();
        services.AddScoped<EquipmentCsvMapper>();
        services.AddScoped<VillagerApprenticesCsvMapper>();
        services.AddHttpClient<IClashApiService, ClashApiService>();
        services.AddScoped<IClashProfileSyncService, ClashProfileSyncService>();
        services.AddScoped<IPriorityService, PriorityService>();

        return services;
    }
}
