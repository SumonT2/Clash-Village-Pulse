namespace ClashVillagePulse.Infrastructure.StaticData.Processors;

public class EquipmentProcessor : StaticDataTargetProcessorBase
{
    public override string TargetKey => "equipment";

    public override async Task ProcessAsync(
        StaticDataProcessorContext context,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException("Equipment processor not implemented yet.");
    }
}