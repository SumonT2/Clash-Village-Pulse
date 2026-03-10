using ClashVillagePulse.Infrastructure.Database;

namespace ClashVillagePulse.Infrastructure.StaticData;

public class StaticDataProcessorContext
{
    public Guid RunId { get; init; }

    public string Fingerprint { get; init; } = string.Empty;

    public AppDbContext Db { get; init; } = null!;

    public StaticDataDownloader Downloader { get; init; } = null!;

    public StaticDataDecompressor Decompressor { get; init; } = null!;

    public StaticDataRunTracker RunTracker { get; init; } = null!;
}