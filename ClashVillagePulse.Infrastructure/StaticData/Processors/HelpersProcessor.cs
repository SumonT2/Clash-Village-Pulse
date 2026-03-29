using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Mapping;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class HelpersProcessor : StaticDataTargetProcessorBase
{
    private readonly VillagerApprenticesCsvMapper _mapper;

    public HelpersProcessor(VillagerApprenticesCsvMapper mapper)
    {
        _mapper = mapper;
    }

    public override string TargetKey => "helpers";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "logic/villager_apprentices.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        var rows = await TrackParseAsync(
            context,
            async () =>
            {
                using var stream = new MemoryStream(decompressedBytes);
                return _mapper.Parse(stream)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        x.GlobalId > 0 &&
                        x.Level > 0)
                    .ToList();
            },
            "Parsed helpers CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} helper rows.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<VillagerApprenticeCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Helper)
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
            .GroupBy(x => x.GlobalId)
            .OrderBy(x => x.Key)
            .ToList();

        foreach (var group in grouped)
        {
            var firstRow = group.First();

            var staticItem = new StaticItem
            {
                Id = Guid.NewGuid(),
                ItemDataId = group.Key,
                ItemType = ItemType.Helper,
                Name = firstRow.Name.Trim(),
                Section = VillageSection.HomeVillage,
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
                    UpgradeTimeSeconds = null,
                    HelperType = row.HelperType,
                    BoostMultiplier = row.BoostMultiplier,
                    BoostTimeSeconds = row.BoostTimeSeconds
                };

                if (row.Cost.HasValue && !string.IsNullOrWhiteSpace(row.CostResource))
                {
                    level.UpgradeCosts.Add(new StaticItemLevelUpgradeCost
                    {
                        Id = Guid.NewGuid(),
                        ResourceType = UpgradeResourceMapper.Map(row.CostResource),
                        Amount = row.Cost.Value
                    });
                }

                if (row.RequiredTownHallLevel.HasValue && row.RequiredTownHallLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = RequirementType.TownHall,
                        RequiredLevel = row.RequiredTownHallLevel.Value
                    });
                }

                staticItem.Levels.Add(level);
            }

            db.StaticItems.Add(staticItem);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}