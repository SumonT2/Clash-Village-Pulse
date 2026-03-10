namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class HeroesProcessor : StaticDataTargetProcessorBase
{
    public override string TargetKey => "heroes";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Heroes processor not implemented yet.");
    }
}