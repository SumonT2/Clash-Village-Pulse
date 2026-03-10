using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Infrastructure.StaticData;

public abstract class StaticDataTargetProcessorBase : IStaticDataTargetProcessor
{
    public abstract string TargetKey { get; }

    public abstract Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default);

    protected async Task<byte[]> DownloadAsync(
        StaticDataProcessorContext context,
        string assetPath,
        CancellationToken cancellationToken = default)
    {
        var step = await context.RunTracker.StartStepAsync(
            context.RunId, TargetKey, StaticDataStepType.Download, cancellationToken);

        try
        {
            var url = $"https://game-assets.clashofclans.com/{context.Fingerprint}/{assetPath}";
            var bytes = await context.Downloader.DownloadAsync(url);
            await context.RunTracker.CompleteStepAsync(
                step.Id, $"Downloaded {bytes.Length} bytes.", cancellationToken);
            return bytes;
        }
        catch (Exception ex)
        {
            await context.RunTracker.FailStepAsync(step.Id, ex.Message, cancellationToken);
            throw;
        }
    }

    protected async Task<byte[]> DecompressAsync(
        StaticDataProcessorContext context,
        byte[] rawBytes,
        CancellationToken cancellationToken = default)
    {
        var step = await context.RunTracker.StartStepAsync(
            context.RunId, TargetKey, StaticDataStepType.Decompress, cancellationToken);

        try
        {
            var bytes = context.Decompressor.DecompressIfNeeded(rawBytes);
            await context.RunTracker.CompleteStepAsync(
                step.Id, $"Decompressed {bytes.Length} bytes.", cancellationToken);
            return bytes;
        }
        catch (Exception ex)
        {
            await context.RunTracker.FailStepAsync(step.Id, ex.Message, cancellationToken);
            throw;
        }
    }

    protected async Task<T> TrackParseAsync<T>(
        StaticDataProcessorContext context,
        Func<Task<T>> action,
        string successMessage,
        CancellationToken cancellationToken = default)
    {
        var step = await context.RunTracker.StartStepAsync(
            context.RunId, TargetKey, StaticDataStepType.Parse, cancellationToken);

        try
        {
            var result = await action();
            await context.RunTracker.CompleteStepAsync(step.Id, successMessage, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            await context.RunTracker.FailStepAsync(step.Id, ex.Message, cancellationToken);
            throw;
        }
    }

    protected async Task TrackSaveAsync(
        StaticDataProcessorContext context,
        Func<Task> action,
        string successMessage,
        CancellationToken cancellationToken = default)
    {
        var step = await context.RunTracker.StartStepAsync(
            context.RunId, TargetKey, StaticDataStepType.Save, cancellationToken);

        try
        {
            await action();
            await context.RunTracker.CompleteStepAsync(step.Id, successMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            await context.RunTracker.FailStepAsync(step.Id, ex.Message, cancellationToken);
            throw;
        }
    }
}