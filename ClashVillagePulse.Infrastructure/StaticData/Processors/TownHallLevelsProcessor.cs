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

        await TrackParseAsync(
            context,
            async () =>
            {
                // Placeholder for later TH level parsing
                return decompressedBytes.Length;
            },
            "Parsed townhall levels CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () =>
            {
                // Placeholder for later TH metadata persistence
                await Task.CompletedTask;
            },
            "Saved townhall level data.",
            cancellationToken);
    }
}