namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class LocalizationProcessor : StaticDataTargetProcessorBase
{
    public override string TargetKey => "texts";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        var rawBytes = await DownloadAsync(context, "localization/texts.csv", cancellationToken);
        var decompressedBytes = await DecompressAsync(context, rawBytes, cancellationToken);

        await TrackParseAsync(
            context,
            async () =>
            {
                // Placeholder for later text/TID parsing
                return decompressedBytes.Length;
            },
            "Parsed localization CSV.",
            cancellationToken);

        await TrackSaveAsync(
            context,
            async () =>
            {
                // Placeholder: later map TID -> text into DB table or cache
                await Task.CompletedTask;
            },
            "Saved localization data.",
            cancellationToken);
    }
}