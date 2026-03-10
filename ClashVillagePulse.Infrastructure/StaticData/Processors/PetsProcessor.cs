namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class PetsProcessor : StaticDataTargetProcessorBase
{
    public override string TargetKey => "pets";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Pets processor not implemented yet.");
    }
}