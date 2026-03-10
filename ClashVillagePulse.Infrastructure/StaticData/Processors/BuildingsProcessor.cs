using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Mapping;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class BuildingsProcessor : StaticDataTargetProcessorBase
{
    private readonly BuildingsCsvMapper _mapper;

    public BuildingsProcessor(BuildingsCsvMapper mapper)
    {
        _mapper = mapper;
    }

    public override string TargetKey => "buildings";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "logic/buildings.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        var rows = await TrackParseAsync(
            context,
            async () =>
            {
                using var stream = new MemoryStream(decompressedBytes);
                return _mapper.Parse(stream)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        x.BuildingLevel > 0)
                    .ToList();
            },
            "Parsed buildings CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} building rows.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<BuildingCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Building)
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
                Section = ResolveVillageSection(x.CapitalHallLevel)
            })
            .ToList();

        foreach (var group in grouped)
        {
            var firstRow = group.First();

            var staticItem = new StaticItem
            {
                Id = Guid.NewGuid(),
                ItemDataId = group.Key.GlobalId,
                ItemType = ItemType.Building,
                Name = firstRow.Name.Trim(),
                Section = group.Key.Section,
                IsUpgradeable = true
            };

            foreach (var row in group.OrderBy(x => x.BuildingLevel))
            {
                var level = new StaticItemLevel
                {
                    Id = Guid.NewGuid(),
                    StaticItemId = staticItem.Id,
                    Level = row.BuildingLevel,
                    UpgradeTimeSeconds = ToSeconds(row.BuildTimeD, row.BuildTimeH, row.BuildTimeM, row.BuildTimeS)
                };

                if (row.BuildCost.HasValue && !string.IsNullOrWhiteSpace(row.BuildResource))
                {
                    level.UpgradeCosts.Add(new StaticItemLevelUpgradeCost
                    {
                        Id = Guid.NewGuid(),
                        ResourceType = UpgradeResourceMapper.Map(row.BuildResource),
                        Amount = row.BuildCost.Value
                    });
                }

                if (row.AlternateCost.HasValue && !string.IsNullOrWhiteSpace(row.AlternateResource))
                {
                    level.UpgradeCosts.Add(new StaticItemLevelUpgradeCost
                    {
                        Id = Guid.NewGuid(),
                        ResourceType = UpgradeResourceMapper.Map(row.AlternateResource),
                        Amount = row.AlternateCost.Value
                    });
                }

                if (row.TownHallLevel.HasValue && row.TownHallLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = RequirementType.TownHall,
                        RequiredLevel = row.TownHallLevel.Value
                    });
                }

                staticItem.Levels.Add(level);
            }

            db.StaticItems.Add(staticItem);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static VillageSection ResolveVillageSection(int? capitalHallLevel)
    {
        return capitalHallLevel.HasValue && capitalHallLevel.Value > 0
            ? VillageSection.BuilderBase
            : VillageSection.HomeVillage;
    }

    private static int? ToSeconds(int? d, int? h, int? m, int? s)
    {
        if (!d.HasValue && !h.HasValue && !m.HasValue && !s.HasValue)
            return null;

        return (d ?? 0) * 86400 + (h ?? 0) * 3600 + (m ?? 0) * 60 + (s ?? 0);
    }
}