namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class SpellsProcessor : StaticDataTargetProcessorBase
{
    public override string TargetKey => "spells";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Spells processor not implemented yet.");
    }
}