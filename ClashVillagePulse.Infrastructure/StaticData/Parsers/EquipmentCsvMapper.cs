using CsvHelper;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class EquipmentCsvMapper
{
    private const int EquipmentGlobalIdPrefix = 90;

    public List<EquipmentCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<EquipmentCsvRow>();

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
        string? currentAllowedCharacters = null;
        int? currentRequiredBlacksmithLevel = null;
        int? currentRequiredCharacterLevel = null;

        var equipmentIndex = -1;

        while (csv.Read())
        {
            var name = GetString(csv, "Name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                currentName = name;
                equipmentIndex++;
                currentGlobalId = BuildSyntheticGlobalId(equipmentIndex);

                // reset per-item state
                currentTid = null;
                currentAllowedCharacters = null;
                currentRequiredBlacksmithLevel = null;
                currentRequiredCharacterLevel = null;
            }

            if (string.IsNullOrWhiteSpace(currentName))
                continue;

            // Skip placeholder rows
            if (currentName.StartsWith("UNUSED", StringComparison.OrdinalIgnoreCase))
                continue;

            var tid = GetString(csv, "TID");
            if (!string.IsNullOrWhiteSpace(tid))
                currentTid = tid;

            var allowedCharacters = GetString(csv, "AllowedCharacters");
            if (!string.IsNullOrWhiteSpace(allowedCharacters))
                currentAllowedCharacters = allowedCharacters;

            var requiredBlacksmithLevel = GetInt(csv, "RequiredBlacksmithLevel");
            if (requiredBlacksmithLevel.HasValue)
                currentRequiredBlacksmithLevel = requiredBlacksmithLevel;

            var requiredCharacterLevel = GetInt(csv, "RequiredCharacterLevel");
            if (requiredCharacterLevel.HasValue)
                currentRequiredCharacterLevel = requiredCharacterLevel;

            var level = GetInt(csv, "Level") ?? 0;
            if (level <= 0)
                continue;

            rows.Add(new EquipmentCsvRow
            {
                Name = currentName!,
                GlobalId = currentGlobalId,
                Level = level,
                TID = currentTid,
                AllowedCharacters = currentAllowedCharacters,
                RequiredBlacksmithLevel = currentRequiredBlacksmithLevel,
                RequiredCharacterLevel = currentRequiredCharacterLevel,
                UpgradeResources = GetString(csv, "UpgradeResources"),
                UpgradeCosts = GetString(csv, "UpgradeCosts")
            });
        }

        return rows;
    }

    private static int BuildSyntheticGlobalId(int itemIndex)
        => (EquipmentGlobalIdPrefix * 1_000_000) + itemIndex;

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
}