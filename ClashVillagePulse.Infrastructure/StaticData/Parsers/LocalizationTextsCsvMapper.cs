using CsvHelper;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Parsers;

public class LocalizationTextsCsvMapper
{
    public List<LocalizationTextCsvRow> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<LocalizationTextCsvRow>();

        // Row 0: header
        if (!csv.Read())
            return rows;

        csv.ReadHeader();

        // Row 1: type-definition row -> skip
        if (!csv.Read())
            return rows;

        while (csv.Read())
        {
            var tid = GetString(csv, "TID");
            var text = GetString(csv, "EN");

            if (string.IsNullOrWhiteSpace(tid))
                continue;

            if (string.IsNullOrWhiteSpace(text))
                continue;

            rows.Add(new LocalizationTextCsvRow
            {
                Tid = tid!,
                Text = text!
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
}