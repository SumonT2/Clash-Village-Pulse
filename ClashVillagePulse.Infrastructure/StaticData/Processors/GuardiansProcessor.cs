using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Mapping;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class GuardiansProcessor : StaticDataTargetProcessorBase
{
    private readonly CharactersCsvMapper _charactersMapper;

    private static readonly Dictionary<string, int> GuardianVillageIds =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Longshot"] = 107000000,
            ["Smasher"] = 107000001,
            ["Guardian Eagle"] = 107000002,
            ["Guardian Giga Inferno"] = 107000003,
            ["Guardian Assassins"] = 107000004,
            ["Guardian Reviver"] = 107000005
        };

    private static readonly Dictionary<string, string> GuardianDisplayNames =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Longshot"] = "Longshot",
            ["Smasher"] = "Smasher",
            ["Guardian Eagle"] = "Guardian Eagle",
            ["Guardian Giga Inferno"] = "Guardian Giga Inferno",
            ["Guardian Assassins"] = "Guardian Assassins",
            ["Guardian Reviver"] = "Guardian Reviver"
        };

    public GuardiansProcessor(CharactersCsvMapper charactersMapper)
    {
        _charactersMapper = charactersMapper;
    }

    public override string TargetKey => "guardians";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "logic/characters.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        var rows = await TrackParseAsync(
            context,
            async () =>
            {
                using var stream = new MemoryStream(decompressedBytes);
                return _charactersMapper.Parse(stream)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        x.VisualLevel > 0 &&
                        x.GlobalId.HasValue &&
                        IsGuardianCharacter(x))
                    .ToList();
            },
            "Parsed guardian rows from characters CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} guardian rows from characters CSV.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<CharacterCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Guardian)
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
            .GroupBy(x => NormalizeGuardianName(x.Name))
            .Where(g => GuardianVillageIds.ContainsKey(g.Key))
            .ToList();

        foreach (var group in grouped)
        {
            var normalizedName = group.Key;
            var firstRow = group.First();

            var itemDataId = GuardianVillageIds[normalizedName];
            var displayName = GuardianDisplayNames.TryGetValue(normalizedName, out var prettyName)
                ? prettyName
                : normalizedName;

            var staticItem = new StaticItem
            {
                Id = Guid.NewGuid(),
                ItemDataId = itemDataId,
                ItemType = ItemType.Guardian,
                Name = displayName,
                Section = VillageSection.HomeVillage,
                IsUpgradeable = true
            };

            var distinctLevels = group
                .OrderBy(x => x.VisualLevel)
                .GroupBy(x => x.VisualLevel)
                .Select(g => g.First())
                .ToList();

            foreach (var row in distinctLevels)
            {
                var level = new StaticItemLevel
                {
                    Id = Guid.NewGuid(),
                    StaticItemId = staticItem.Id,
                    Level = row.VisualLevel,
                    UpgradeTimeSeconds = ToSeconds(row.UpgradeTimeH, row.UpgradeTimeM)
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

                if (row.UnlockByTH.HasValue && row.UnlockByTH.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = VillageTypeResolver.ResolveTownHallRequirement(row.VillageType),
                        RequiredLevel = row.UnlockByTH.Value
                    });
                }

                staticItem.Levels.Add(level);
            }

            db.StaticItems.Add(staticItem);
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static bool IsGuardianCharacter(CharacterCsvRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.TID) &&
            row.TID.Contains("GUARDIAN", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var normalized = NormalizeGuardianName(row.Name);

        return GuardianVillageIds.ContainsKey(normalized);
    }

    private static string NormalizeGuardianName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return string.Empty;

        var name = rawName.Trim();

        return name switch
        {
            "Guardian_Eagle" => "Guardian Eagle",
            "Guardian_Giga_Inferno" => "Guardian Giga Inferno",
            "Guardian_Assassins" => "Guardian Assassins",
            "Guardian_Reviver" => "Guardian Reviver",
            _ => name
        };
    }

    private static int? ToSeconds(int? hours, int? minutes)
    {
        if (!hours.HasValue && !minutes.HasValue)
            return null;

        return (hours ?? 0) * 3600 + (minutes ?? 0) * 60;
    }
}