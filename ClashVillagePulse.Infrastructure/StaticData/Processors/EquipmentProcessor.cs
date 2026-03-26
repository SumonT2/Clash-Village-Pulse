using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Mapping;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class EquipmentProcessor : StaticDataTargetProcessorBase
{
    private readonly EquipmentCsvMapper _mapper;

    public EquipmentProcessor(EquipmentCsvMapper mapper)
    {
        _mapper = mapper;
    }

    public override string TargetKey => "equipment";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "logic/character_items.csv", cancellationToken);
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
            "Parsed equipment CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} equipment rows.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<EquipmentCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Equipment)
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

        var heroesByName = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Hero)
            .ToDictionaryAsync(
                x => NormalizeName(x.Name),
                x => x.ItemDataId,
                cancellationToken);

        var grouped = rows
            .Where(x => x.GlobalId.HasValue)
            .GroupBy(x => x.GlobalId!.Value)
            .ToList();

        foreach (var group in grouped)
        {
            var firstRow = group.First();

            var staticItem = new StaticItem
            {
                Id = Guid.NewGuid(),
                ItemDataId = group.Key,
                ItemType = ItemType.Equipment,
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
                    UpgradeTimeSeconds = null
                };

                foreach (var cost in ParseUpgradeCosts(row.UpgradeResources, row.UpgradeCosts))
                {
                    level.UpgradeCosts.Add(new StaticItemLevelUpgradeCost
                    {
                        Id = Guid.NewGuid(),
                        ResourceType = cost.ResourceType,
                        Amount = cost.Amount
                    });
                }

                if (row.RequiredBlacksmithLevel.HasValue && row.RequiredBlacksmithLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = RequirementType.Blacksmith,
                        RequiredLevel = row.RequiredBlacksmithLevel.Value
                    });
                }

                if (!string.IsNullOrWhiteSpace(row.AllowedCharacters) &&
                    row.RequiredCharacterLevel.HasValue &&
                    row.RequiredCharacterLevel.Value > 0)
                {
                    var heroNameKey = NormalizeName(row.AllowedCharacters);

                    if (heroesByName.TryGetValue(heroNameKey, out var heroDataId))
                    {
                        level.Requirements.Add(new StaticItemRequirement
                        {
                            Id = Guid.NewGuid(),
                            RequirementType = RequirementType.BuildingLevel,
                            RequiredItemDataId = heroDataId,
                            RequiredLevel = row.RequiredCharacterLevel.Value
                        });
                    }
                }

                staticItem.Levels.Add(level);
            }

            db.StaticItems.Add(staticItem);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<ParsedUpgradeCost> ParseUpgradeCosts(
        string? resourcesValue,
        string? costsValue)
    {
        var result = new List<ParsedUpgradeCost>();

        if (string.IsNullOrWhiteSpace(resourcesValue) || string.IsNullOrWhiteSpace(costsValue))
            return result;

        var resources = resourcesValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var costs = costsValue
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var count = Math.Min(resources.Length, costs.Length);

        for (var i = 0; i < count; i++)
        {
            if (!long.TryParse(costs[i], out var amount))
                continue;

            var resourceType = UpgradeResourceMapper.Map(resources[i]);
            if (resourceType == UpgradeResourceType.Unknown)
                continue;

            result.Add(new ParsedUpgradeCost
            {
                ResourceType = resourceType,
                Amount = amount
            });
        }

        return result;
    }

    private static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim().ToLowerInvariant();
    }

    private sealed class ParsedUpgradeCost
    {
        public UpgradeResourceType ResourceType { get; set; }
        public long Amount { get; set; }
    }
}