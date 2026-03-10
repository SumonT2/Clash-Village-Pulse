using CsvHelper;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData;

public class CsvParser
{
    public List<dynamic> Parse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        return csv.GetRecords<dynamic>().ToList();
    }
}