using CsvHelper;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class SpellsCsvMapper
{
    public List<SpellCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<SpellCsvRow>();

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
        int? currentSpellForgeLevel = null;
        int? currentLaboratoryLevel = null;
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
                currentSpellForgeLevel = null;
                currentLaboratoryLevel = null;
                currentVillageType = null;
            }

            var globalId = GetInt(csv, "GlobalID");
            if (globalId.HasValue)
                currentGlobalId = globalId;

            var tid = GetString(csv, "TID");
            if (!string.IsNullOrWhiteSpace(tid))
                currentTid = tid;

            var spellForgeLevel = GetInt(csv, "SpellForgeLevel");
            if (spellForgeLevel.HasValue)
                currentSpellForgeLevel = spellForgeLevel;

            var laboratoryLevel = GetInt(csv, "LaboratoryLevel");
            if (laboratoryLevel.HasValue)
                currentLaboratoryLevel = laboratoryLevel;

            var villageType = GetString(csv, "VillageType");
            if (!string.IsNullOrWhiteSpace(villageType))
                currentVillageType = villageType;

            var level = GetInt(csv, "Level") ?? 0;

            if (string.IsNullOrWhiteSpace(currentName))
                continue;

            if (level <= 0)
                continue;

            rows.Add(new SpellCsvRow
            {
                Name = currentName!,
                GlobalId = currentGlobalId,
                Level = level,
                TID = currentTid,
                SpellForgeLevel = currentSpellForgeLevel,
                LaboratoryLevel = currentLaboratoryLevel,
                UpgradeResource = GetString(csv, "UpgradeResource"),
                UpgradeCost = GetLong(csv, "UpgradeCost"),
                UpgradeTimeH = GetInt(csv, "UpgradeTimeH"),
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