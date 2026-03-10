namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class CharactersProcessor : StaticDataTargetProcessorBase
{
    public override string TargetKey => "characters";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Characters processor not implemented yet.");
    }
}