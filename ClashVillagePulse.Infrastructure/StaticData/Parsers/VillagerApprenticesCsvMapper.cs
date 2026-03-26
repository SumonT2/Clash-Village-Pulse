using CsvHelper;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class VillagerApprenticesCsvMapper
{
    private static readonly Dictionary<string, (int GlobalId, string DisplayName)> HelperMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["BuilderApprentice"] = (93000000, "Builder's Apprentice"),
            ["ResearchApprentice"] = (93000001, "Lab Assistant"),
            ["Alchemist"] = (93000002, "Alchemist"),
            ["Prospector"] = (93000003, "Prospector")
        };

    public List<VillagerApprenticeCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<VillagerApprenticeCsvRow>();

        // header row
        if (!csv.Read())
            return rows;

        csv.ReadHeader();

        // type-definition row
        if (!csv.Read())
            return rows;

        string? currentRawName = null;
        string? currentDisplayName = null;
        int? currentGlobalId = null;
        string? currentTid = null;
        string? currentInfoTid = null;
        int? currentRequiredTownHallLevel = null;
        string? currentHelperType = null;
        string? currentCostResource = null;
        var currentLevel = 0;

        while (csv.Read())
        {
            var rawName = GetString(csv, "Name");
            if (!string.IsNullOrWhiteSpace(rawName))
            {
                if (!HelperMap.TryGetValue(rawName, out var helper))
                {
                    currentRawName = null;
                    currentDisplayName = null;
                    currentGlobalId = null;
                    currentTid = null;
                    currentInfoTid = null;
                    currentRequiredTownHallLevel = null;
                    currentHelperType = null;
                    currentCostResource = null;
                    currentLevel = 0;
                    continue;
                }

                currentRawName = rawName;
                currentDisplayName = helper.DisplayName;
                currentGlobalId = helper.GlobalId;

                currentTid = null;
                currentInfoTid = null;
                currentRequiredTownHallLevel = null;
                currentHelperType = null;
                currentCostResource = null;
                currentLevel = 0;
            }

            if (string.IsNullOrWhiteSpace(currentRawName) || !currentGlobalId.HasValue || string.IsNullOrWhiteSpace(currentDisplayName))
                continue;

            var tid = GetString(csv, "TID");
            if (!string.IsNullOrWhiteSpace(tid))
                currentTid = tid;

            var infoTid = GetString(csv, "InfoTID");
            if (!string.IsNullOrWhiteSpace(infoTid))
                currentInfoTid = infoTid;

            var requiredTownHallLevel = GetInt(csv, "RequiredTownHallLevel");
            if (requiredTownHallLevel.HasValue)
                currentRequiredTownHallLevel = requiredTownHallLevel;

            var helperType = GetString(csv, "Type");
            if (!string.IsNullOrWhiteSpace(helperType))
                currentHelperType = helperType;

            var costResource = GetString(csv, "CostResource");
            if (!string.IsNullOrWhiteSpace(costResource))
                currentCostResource = costResource;

            currentLevel++;

            rows.Add(new VillagerApprenticeCsvRow
            {
                Name = currentDisplayName!,
                GlobalId = currentGlobalId.Value,
                Level = currentLevel,
                TID = currentTid,
                InfoTID = currentInfoTid,
                RequiredTownHallLevel = currentRequiredTownHallLevel,
                HelperType = currentHelperType,
                BoostMultiplier = GetInt(csv, "BoostMultiplier"),
                BoostTimeSeconds = GetInt(csv, "BoostTimeSeconds"),
                CostResource = currentCostResource,
                Cost = GetLong(csv, "Cost")
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