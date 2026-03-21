using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Infrastructure.StaticData.Models;
using ClashVillagePulse.Infrastructure.StaticData.Parsers;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class LocalizationProcessor : StaticDataTargetProcessorBase
{
    private readonly LocalizationTextsCsvMapper _mapper;

    public LocalizationProcessor(LocalizationTextsCsvMapper mapper)
    {
        _mapper = mapper;
    }

    public override string TargetKey => "texts";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "localization/texts.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        var rows = await TrackParseAsync(
            context,
            async () =>
            {
                using var stream = new MemoryStream(decompressedBytes);
                return _mapper.Parse(stream)
                    .Where(x =>
                        !string.IsNullOrWhiteSpace(x.Tid) &&
                        !string.IsNullOrWhiteSpace(x.Text))
                    .ToList();
            },
            "Parsed localization CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () => await SaveAsync(context, rows, cancellationToken),
            $"Saved {rows.Count} localization rows.",
            cancellationToken);
    }

    private static async Task SaveAsync(
        StaticDataProcessorContext context,
        List<LocalizationTextCsvRow> rows,
        CancellationToken cancellationToken)
    {
        var db = context.Db;

        var existing = await db.LocalizationTexts
            .Where(x => x.LanguageCode == "EN")
            .ToListAsync(cancellationToken);

        if (existing.Count > 0)
        {
            db.LocalizationTexts.RemoveRange(existing);
            await db.SaveChangesAsync(cancellationToken);
        }

        foreach (var row in rows)
        {
            db.LocalizationTexts.Add(new LocalizationText
            {
                Id = Guid.NewGuid(),
                Tid = row.Tid,
                LanguageCode = "EN",
                Text = row.Text
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}