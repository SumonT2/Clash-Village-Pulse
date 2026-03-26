using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.StaticData.Helpers;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class TownHallLevelsProcessor : StaticDataTargetProcessorBase
{
    public override string TargetKey => "townhall-levels";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "logic/townhall_levels.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        var rows = await TrackParseAsync(
            context,
            async () => await ParseRowsAsync(decompressedBytes, cancellationToken),
            "Parsed townhall levels CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} hall-cap rows.",
            cancellationToken);
    }

    private static async Task<List<Dictionary<string, string>>> ParseRowsAsync(
        byte[] decompressedBytes,
        CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(decompressedBytes);
        using var reader = new StreamReader(stream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

        var rows = new List<Dictionary<string, string>>();

        if (!await csv.ReadAsync())
            return rows;

        csv.ReadHeader();
        var headers = csv.HeaderRecord?.ToList() ?? new List<string>();

        if (!await csv.ReadAsync())
            return rows; // skip type row

        while (await csv.ReadAsync())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var header in headers)
            {
                row[header] = (csv.GetField(header) ?? string.Empty).Trim();
            }

            rows.Add(row);
        }

        return rows;
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<Dictionary<string, string>> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var staticItems = await db.StaticItems
            .AsNoTracking()
            .Where(x => x.ItemType == ItemType.Building || x.ItemType == ItemType.Trap)
            .ToListAsync(cancellationToken);

        var itemLookup = staticItems
            .GroupBy(x => HallItemAliasResolver.Normalize(x.Name))
            .ToDictionary(x => x.Key, x => x.First());

        var existing = await db.StaticHallItemCaps.ToListAsync(cancellationToken);
        if (existing.Count > 0)
        {
            db.StaticHallItemCaps.RemoveRange(existing);
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var row in rows)
        {
            if (!row.TryGetValue("Name", out var hallLevelText) ||
                !int.TryParse(hallLevelText, out var hallLevel))
            {
                continue;
            }

            foreach (var pair in row)
            {
                if (pair.Key.Equals("Name", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!int.TryParse(pair.Value, out var maxCount) || maxCount <= 0)
                    continue;

                var normalizedHeader = HallItemAliasResolver.Normalize(pair.Key);

                if (!itemLookup.TryGetValue(normalizedHeader, out var staticItem))
                    continue;

                db.StaticHallItemCaps.Add(new StaticHallItemCap
                {
                    Id = Guid.NewGuid(),
                    Section = staticItem.Section,
                    HallLevel = hallLevel,
                    ItemType = staticItem.ItemType,
                    ItemDataId = staticItem.ItemDataId,
                    MaxCount = maxCount
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}