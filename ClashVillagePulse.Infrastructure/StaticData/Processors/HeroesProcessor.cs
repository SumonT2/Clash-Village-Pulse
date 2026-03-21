using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Mapping;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class HeroesProcessor : StaticDataTargetProcessorBase
{
    private readonly HeroesCsvMapper _mapper;

    public HeroesProcessor(HeroesCsvMapper mapper)
    {
        _mapper = mapper;
    }

    public override string TargetKey => "heroes";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "logic/heroes.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        var rows = await TrackParseAsync(
            context,
            async () =>
            {
                using var stream = new MemoryStream(decompressedBytes);
                return _mapper.Parse(stream)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        x.VisualLevel > 0 &&
                        x.GlobalId.HasValue)
                    .ToList();
            },
            "Parsed heroes CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} hero rows.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<HeroCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Hero)
            .Include(x => x.Levels)
                .ThenInclude(x => x.UpgradeCosts)
            .Include(x => x.Levels)
                .ThenInclude(x => x.Requirements)
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            db.StaticItems.RemoveRange(existing);
            await db.SaveChangesAsync(cancellationToken);
        }

        var grouped = rows
            .Where(x => x.GlobalId.HasValue)
            .GroupBy(x => new
            {
                GlobalId = x.GlobalId!.Value,
                Section = VillageTypeResolver.ResolveSection(x.VillageType)
            })
            .ToList();

        foreach (var group in grouped)
        {
            var firstRow = group.First();

            var staticItem = new StaticItem
            {
                Id = Guid.NewGuid(),
                ItemDataId = group.Key.GlobalId,
                ItemType = ItemType.Hero,
                Name = firstRow.Name.Trim(),
                Section = group.Key.Section,
                IsUpgradeable = true
            };

            foreach (var row in group.OrderBy(x => x.VisualLevel))
            {
                var level = new StaticItemLevel
                {
                    Id = Guid.NewGuid(),
                    StaticItemId = staticItem.Id,
                    Level = row.VisualLevel,
                    UpgradeTimeSeconds = ToSeconds(row.UpgradeTimeH)
                };

                if (row.UpgradeCost.HasValue && !string.IsNullOrWhiteSpace(row.UpgradeResource))
                {
                    level.UpgradeCosts.Add(new StaticItemLevelUpgradeCost
                    {
                        Id = Guid.NewGuid(),
                        ResourceType = UpgradeResourceMapper.Map(row.UpgradeResource),
                        Amount = row.UpgradeCost.Value
                    });
                }

                if (row.RequiredTownHallLevel.HasValue && row.RequiredTownHallLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = VillageTypeResolver.ResolveTownHallRequirement(row.VillageType),
                        RequiredLevel = row.RequiredTownHallLevel.Value
                    });
                }

                if (row.RequiredHeroTavernLevel.HasValue && row.RequiredHeroTavernLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = RequirementType.HeroHall,
                        RequiredLevel = row.RequiredHeroTavernLevel.Value
                    });
                }

                staticItem.Levels.Add(level);
            }

            db.StaticItems.Add(staticItem);
        }

        await db.SaveChangesAsync(cancellationToken);
    }



    private static int? ToSeconds(int? hours)
    {
        if (!hours.HasValue)
            return null;

        return hours.Value * 3600;
    }
}