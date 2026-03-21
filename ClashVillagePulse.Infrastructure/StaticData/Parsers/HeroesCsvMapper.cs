using CsvHelper;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class HeroesCsvMapper
{
    private const int HeroGlobalIdPrefix = 28;

    public List<HeroCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<HeroCsvRow>();

        // Row 0: header
        if (!csv.Read())
            return rows;

        csv.ReadHeader();

        // Row 1: type-definition row -> skip
        if (!csv.Read())
            return rows;

        string? currentName = null;
        int? currentGlobalId = null;
        string? currentTid = null;
        int? currentRequiredTownHallLevel = null;
        int? currentRequiredHeroTavernLevel = null;
        string? currentVillageType = null;

        var heroIndex = -1;

        while (csv.Read())
        {
            var name = GetString(csv, "Name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                currentName = name;
                heroIndex++;
                currentGlobalId = BuildSyntheticGlobalId(heroIndex);

                currentTid = null;
                currentRequiredTownHallLevel = null;
                currentRequiredHeroTavernLevel = null;
                currentVillageType = null;
            }

            var tid = GetString(csv, "TID");
            if (!string.IsNullOrWhiteSpace(tid))
                currentTid = tid;

            var requiredTownHallLevel = GetInt(csv, "RequiredTownHallLevel");
            if (requiredTownHallLevel.HasValue)
                currentRequiredTownHallLevel = requiredTownHallLevel;

            var requiredHeroTavernLevel = GetInt(csv, "RequiredHeroTavernLevel");
            if (requiredHeroTavernLevel.HasValue)
                currentRequiredHeroTavernLevel = requiredHeroTavernLevel;

            var villageType = GetString(csv, "VillageType");
            if (!string.IsNullOrWhiteSpace(villageType))
                currentVillageType = villageType;

            var visualLevel = GetInt(csv, "VisualLevel") ?? 0;

            if (string.IsNullOrWhiteSpace(currentName))
                continue;

            if (visualLevel <= 0)
                continue;

            rows.Add(new HeroCsvRow
            {
                Name = currentName!,
                GlobalId = currentGlobalId,
                VisualLevel = visualLevel,
                TID = currentTid,
                RequiredTownHallLevel = currentRequiredTownHallLevel,
                RequiredHeroTavernLevel = currentRequiredHeroTavernLevel,
                UpgradeResource = GetString(csv, "UpgradeResource"),
                UpgradeCost = GetLong(csv, "UpgradeCost"),
                UpgradeTimeH = GetInt(csv, "UpgradeTimeH"),
                VillageType = currentVillageType
            });
        }

        return rows;
    }

    private static int BuildSyntheticGlobalId(int itemIndex)
        => (HeroGlobalIdPrefix * 1_000_000) + itemIndex;

    private static string? GetString(CsvReader csv, string column)
    {
        try
        {
            if (csv.TryGetField<string>(column, out var value))
                return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }
        catch
        {
        }

        return null;
    }

    private static int? GetInt(CsvReader csv, string column)
    {
        try
        {
            if (csv.TryGetField<int>(column, out var value))
                return value;
        }
        catch
        {
        }

        return null;
    }

    private static long? GetLong(CsvReader csv, string column)
    {
        try
        {
            if (csv.TryGetField<long>(column, out var value))
                return value;
        }
        catch
        {
        }

        return null;
    }
}