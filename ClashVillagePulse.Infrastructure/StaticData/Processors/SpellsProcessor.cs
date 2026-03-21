using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Mapping;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class SpellsProcessor : StaticDataTargetProcessorBase
{
    private readonly SpellsCsvMapper _mapper;

    public SpellsProcessor(SpellsCsvMapper mapper)
    {
        _mapper = mapper;
    }

    public override string TargetKey => "spells";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "logic/spells.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        var rows = await TrackParseAsync(
            context,
            async () =>
            {
                using var stream = new MemoryStream(decompressedBytes);
                return _mapper.Parse(stream)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        x.Level > 0 &&
                        x.GlobalId.HasValue)
                    .ToList();
            },
            "Parsed spells CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} spell rows.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<SpellCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Spell)
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
                ItemType = ItemType.Spell,
                Name = firstRow.Name.Trim(),
                Section = group.Key.Section,
                IsUpgradeable = true
            };

            foreach (var row in group.OrderBy(x => x.Level))
            {
                var level = new StaticItemLevel
                {
                    Id = Guid.NewGuid(),
                    StaticItemId = staticItem.Id,
                    Level = row.Level,
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

                if (row.SpellForgeLevel.HasValue && row.SpellForgeLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = RequirementType.SpellFactory,
                        RequiredLevel = row.SpellForgeLevel.Value
                    });
                }

                if (row.LaboratoryLevel.HasValue && row.LaboratoryLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = RequirementType.Laboratory,
                        RequiredLevel = row.LaboratoryLevel.Value
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