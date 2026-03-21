using CsvHelper;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class CharactersCsvMapper
{
    public List<CharacterCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<CharacterCsvRow>();

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
        int? currentLaboratoryLevel = null;
        int? currentUnlockByTH = null;
        string? currentProductionBuilding = null;
        string? currentVillageType = null;

        while (csv.Read())
        {
            var name = GetString(csv, "Name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                currentName = name;

                // reset per-item state
                currentGlobalId = null;
                currentTid = null;
                currentLaboratoryLevel = null;
                currentUnlockByTH = null;
                currentProductionBuilding = null;
                currentVillageType = null;
            }

            var globalId = GetInt(csv, "GlobalID");
            if (globalId.HasValue)
                currentGlobalId = globalId;

            var tid = GetString(csv, "TID");
            if (!string.IsNullOrWhiteSpace(tid))
                currentTid = tid;

            var laboratoryLevel = GetInt(csv, "LaboratoryLevel");
            if (laboratoryLevel.HasValue)
                currentLaboratoryLevel = laboratoryLevel;

            var unlockByTH = GetInt(csv, "UnlockByTH");
            if (unlockByTH.HasValue)
                currentUnlockByTH = unlockByTH;

            var productionBuilding = GetString(csv, "ProductionBuilding");
            if (!string.IsNullOrWhiteSpace(productionBuilding))
                currentProductionBuilding = productionBuilding;

            var villageType = GetString(csv, "VillageType");
            if (!string.IsNullOrWhiteSpace(villageType))
                currentVillageType = villageType;

            var visualLevel = GetInt(csv, "VisualLevel") ?? 0;

            if (string.IsNullOrWhiteSpace(currentName))
                continue;

            if (visualLevel <= 0)
                continue;

            rows.Add(new CharacterCsvRow
            {
                Name = currentName!,
                GlobalId = currentGlobalId,
                VisualLevel = visualLevel,
                TID = currentTid,
                LaboratoryLevel = currentLaboratoryLevel,
                UnlockByTH = currentUnlockByTH,
                ProductionBuilding = currentProductionBuilding,
                UpgradeResource = GetString(csv, "UpgradeResource"),
                UpgradeCost = GetLong(csv, "UpgradeCost"),
                UpgradeTimeH = GetInt(csv, "UpgradeTimeH"),
                UpgradeTimeM = GetInt(csv, "UpgradeTimeM"),
                VillageType = currentVillageType
            });
        }

        return rows;
    }

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