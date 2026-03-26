using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Mapping;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class TrapsProcessor : StaticDataTargetProcessorBase
{
    private readonly TrapsCsvMapper _mapper;

    public TrapsProcessor(TrapsCsvMapper mapper)
    {
        _mapper = mapper;
    }

    public override string TargetKey => "traps";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "logic/traps.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        var rows = await TrackParseAsync(
            context,
            async () =>
            {
                using var stream = new MemoryStream(decompressedBytes);
                return _mapper.Parse(stream)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        x.GlobalId.HasValue &&
                        x.Level > 0)
                    .ToList();
            },
            "Parsed traps CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} trap rows.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<TrapCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Trap)
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
                ItemType = ItemType.Trap,
                Name = firstRow.Name.Trim(),
                Section = group.Key.Section,
                IsUpgradeable = true
            };

            var distinctLevels = group
                .OrderBy(x => x.Level)
                .GroupBy(x => x.Level)
                .Select(g => g.First())
                .ToList();

            foreach (var row in distinctLevels)
            {
                var level = new StaticItemLevel
                {
                    Id = Guid.NewGuid(),
                    StaticItemId = staticItem.Id,
                    Level = row.Level,
                    UpgradeTimeSeconds = ToSeconds(
                        row.BuildTimeD,
                        row.BuildTimeH,
                        row.BuildTimeM,
                        row.BuildTimeS)
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

                if (row.TownHallLevel.HasValue && row.TownHallLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = VillageTypeResolver.ResolveTownHallRequirement(row.VillageType),
                        RequiredLevel = row.TownHallLevel.Value
                    });
                }

                staticItem.Levels.Add(level);
            }

            db.StaticItems.Add(staticItem);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static int? ToSeconds(int? d, int? h, int? m, int? s)
    {
        if (!d.HasValue && !h.HasValue && !m.HasValue && !s.HasValue)
            return null;

        return (d ?? 0) * 86400 + (h ?? 0) * 3600 + (m ?? 0) * 60 + (s ?? 0);
    }
}