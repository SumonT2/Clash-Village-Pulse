using CsvHelper;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class PetsCsvMapper
{
    private const int PetGlobalIdPrefix = 73;

    public List<PetCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<PetCsvRow>();

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
        string? currentVillageType = null;

        var petIndex = -1;

        while (csv.Read())
        {
            var name = GetString(csv, "Name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                currentName = name;
                petIndex++;
                currentGlobalId = BuildSyntheticGlobalId(petIndex);

                // reset per-item state so values do not leak
                currentTid = null;
                currentRequiredTownHallLevel = null;
                currentVillageType = null;
            }

            if (string.IsNullOrWhiteSpace(currentName))
                continue;

            var tid = GetString(csv, "TID");
            if (!string.IsNullOrWhiteSpace(tid))
                currentTid = tid;

            var requiredTownHallLevel = GetInt(csv, "RequiredTownHallLevel");
            if (requiredTownHallLevel.HasValue)
                currentRequiredTownHallLevel = requiredTownHallLevel;

            var villageType = GetString(csv, "VillageType");
            if (!string.IsNullOrWhiteSpace(villageType))
                currentVillageType = villageType;

            var level =
                GetInt(csv, "Level")
                ?? GetInt(csv, "TroopLevel")
                ?? GetInt(csv, "VisualLevel")
                ?? 0;

            if (level <= 0)
                continue;

            rows.Add(new PetCsvRow
            {
                Name = currentName!,
                GlobalId = currentGlobalId,
                Level = level,
                TID = currentTid,
                RequiredTownHallLevel = currentRequiredTownHallLevel,
                UpgradeResource = GetString(csv, "UpgradeResource"),
                UpgradeCost = GetLong(csv, "UpgradeCost"),
                UpgradeTimeH = GetInt(csv, "UpgradeTimeH"),
                VillageType = currentVillageType
            });
        }

        return rows;
    }

    private static int BuildSyntheticGlobalId(int itemIndex)
        => (PetGlobalIdPrefix * 1_000_000) + itemIndex;

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