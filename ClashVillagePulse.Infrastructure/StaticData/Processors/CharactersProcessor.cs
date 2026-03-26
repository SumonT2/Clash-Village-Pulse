using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Mapping;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class CharactersProcessor : StaticDataTargetProcessorBase
{
    private readonly CharactersCsvMapper _mapper;

    public CharactersProcessor(CharactersCsvMapper mapper)
    {
        _mapper = mapper;
    }

    public override string TargetKey => "characters";

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
                return _mapper.Parse(stream)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Name) &&
                        x.VisualLevel > 0 &&
                        x.GlobalId.HasValue &&
                        !IsGuardianCharacter(x))
                    .ToList();
            },
            "Parsed characters CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} character rows.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<CharacterCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.StaticItems
            .Where(x => x.ItemType == ItemType.Troop || x.ItemType == ItemType.SiegeMachine)
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
                Section = VillageTypeResolver.ResolveSection(x.VillageType),
                ItemType = ResolveItemType(x)
            })
            .ToList();

        foreach (var group in grouped)
        {
            var firstRow = group.First();

            var staticItem = new StaticItem
            {
                Id = Guid.NewGuid(),
                ItemDataId = group.Key.GlobalId,
                ItemType = group.Key.ItemType,
                Name = firstRow.Name.Trim(),
                Section = group.Key.Section,
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

                if (row.LaboratoryLevel.HasValue && row.LaboratoryLevel.Value > 0)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = RequirementType.Laboratory,
                        RequiredLevel = row.LaboratoryLevel.Value
                    });
                }

                var productionRequirement = ResolveProductionBuildingRequirement(row.ProductionBuilding);
                if (productionRequirement is not null)
                {
                    level.Requirements.Add(new StaticItemRequirement
                    {
                        Id = Guid.NewGuid(),
                        RequirementType = productionRequirement.Value,
                        RequiredLevel = 1
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

        if (row.Name.StartsWith("Guardian ", StringComparison.OrdinalIgnoreCase))
            return true;

        return row.Name.Equals("Longshot", StringComparison.OrdinalIgnoreCase)
            || row.Name.Equals("Smasher", StringComparison.OrdinalIgnoreCase);
    }

    private static ItemType ResolveItemType(CharacterCsvRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.ProductionBuilding) &&
            row.ProductionBuilding.Contains("Workshop", StringComparison.OrdinalIgnoreCase))
        {
            return ItemType.SiegeMachine;
        }

        return ItemType.Troop;
    }

    private static RequirementType? ResolveProductionBuildingRequirement(string? productionBuilding)
    {
        if (string.IsNullOrWhiteSpace(productionBuilding))
            return null;

        if (productionBuilding.Contains("Dark", StringComparison.OrdinalIgnoreCase) &&
            productionBuilding.Contains("Barracks", StringComparison.OrdinalIgnoreCase))
        {
            return RequirementType.DarkBarracks;
        }

        if (productionBuilding.Contains("Workshop", StringComparison.OrdinalIgnoreCase))
        {
            return RequirementType.Workshop;
        }

        if (productionBuilding.Contains("Barracks", StringComparison.OrdinalIgnoreCase))
        {
            return RequirementType.Barracks;
        }

        return null;
    }

    private static int? ToSeconds(int? hours, int? minutes)
    {
        if (!hours.HasValue && !minutes.HasValue)
            return null;

        return (hours ?? 0) * 3600 + (minutes ?? 0) * 60;
    }
}