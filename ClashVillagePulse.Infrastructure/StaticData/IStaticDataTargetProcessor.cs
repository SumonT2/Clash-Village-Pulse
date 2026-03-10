namespace ClashVillagePulse.Infrastructure.StaticData;

public interface IStaticDataTargetProcessor
{
    string TargetKey { get; }

    Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default);
}