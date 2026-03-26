using ClashVillagePulse.Infrastructure.StaticData.Models;
using CsvHelper;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class TrapsCsvMapper
{
    public List<TrapCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<TrapCsvRow>();

        if (!csv.Read())
            return rows;

        csv.ReadHeader();

        // type-definition row
        if (!csv.Read())
            return rows;

        string? currentName = null;
        int? currentGlobalId = null;
        string? currentTid = null;
        int? currentTownHallLevel = null;
        string? currentVillageType = null;

        while (csv.Read())
        {
            var name = GetString(csv, "Name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                currentName = name;
                currentGlobalId = null;
                currentTid = null;
                currentTownHallLevel = null;
                currentVillageType = null;
            }

            var globalId = GetInt(csv, "GlobalID");
            if (globalId.HasValue)
                currentGlobalId = globalId;

            var tid = GetString(csv, "TID");
            if (!string.IsNullOrWhiteSpace(tid))
                currentTid = tid;

            var townHall = GetInt(csv, "TownHallLevel");
            if (townHall.HasValue)
                currentTownHallLevel = townHall;

            var villageType = GetString(csv, "VillageType");
            if (!string.IsNullOrWhiteSpace(villageType))
                currentVillageType = villageType;

            var level = GetInt(csv, "Level") ?? 0;

            if (string.IsNullOrWhiteSpace(currentName) || level <= 0)
                continue;

            rows.Add(new TrapCsvRow
            {
                Name = currentName!,
                GlobalId = currentGlobalId,
                Level = level,
                TID = currentTid,
                BuildResource = GetString(csv, "BuildResource"),
                BuildCost = GetLong(csv, "BuildCost"),
                TownHallLevel = currentTownHallLevel,
                BuildTimeD = GetInt(csv, "BuildTimeD"),
                BuildTimeH = GetInt(csv, "BuildTimeH"),
                BuildTimeM = GetInt(csv, "BuildTimeM"),
                BuildTimeS = GetInt(csv, "BuildTimeS"),
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