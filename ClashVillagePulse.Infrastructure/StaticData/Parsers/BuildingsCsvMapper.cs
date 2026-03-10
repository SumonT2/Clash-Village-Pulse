using CsvHelper;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class BuildingsCsvMapper
{
    public List<BuildingCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<BuildingCsvRow>();

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
        int? currentTownHallLevel = null;
        int? currentCapitalHallLevel = null;

        while (csv.Read())
        {
            var name = GetString(csv, "Name");
            if (!string.IsNullOrWhiteSpace(name))
                currentName = name;

            var globalId = GetInt(csv, "GlobalID");
            if (globalId.HasValue)
                currentGlobalId = globalId;

            var tid = GetString(csv, "TID");
            if (!string.IsNullOrWhiteSpace(tid))
                currentTid = tid;

            var townHall = GetInt(csv, "TownHallLevel");
            if (townHall.HasValue)
                currentTownHallLevel = townHall;

            var capitalHall = GetInt(csv, "CapitalHallLevel");
            if (capitalHall.HasValue)
                currentCapitalHallLevel = capitalHall;

            var buildingLevel = GetInt(csv, "BuildingLevel") ?? 0;

            if (string.IsNullOrWhiteSpace(currentName))
                continue;

            if (buildingLevel <= 0)
                continue;

            rows.Add(new BuildingCsvRow
            {
                Name = currentName!,
                GlobalId = currentGlobalId,
                BuildingLevel = buildingLevel,
                TID = currentTid,
                BuildResource = GetString(csv, "BuildResource"),
                BuildCost = GetLong(csv, "BuildCost"),
                AlternateResource = GetString(csv, "AltResource")
                                    ?? GetString(csv, "AlternateResource")
                                    ?? GetString(csv, "AltBuildResource"),
                AlternateCost = GetLong(csv, "AltCost")
                                ?? GetLong(csv, "AlternateCost"),
                TownHallLevel = currentTownHallLevel,
                CapitalHallLevel = currentCapitalHallLevel,
                BuildTimeD = GetInt(csv, "BuildTimeD"),
                BuildTimeH = GetInt(csv, "BuildTimeH"),
                BuildTimeM = GetInt(csv, "BuildTimeM"),
                BuildTimeS = GetInt(csv, "BuildTimeS")
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