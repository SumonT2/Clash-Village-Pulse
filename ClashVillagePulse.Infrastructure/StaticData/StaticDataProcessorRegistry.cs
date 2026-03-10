namespace ClashVillagePulse.Infrastructure.StaticData;

public class StaticDataProcessorRegistry
{
    private readonly IReadOnlyDictionary<string, IStaticDataTargetProcessor> _processors;

    public StaticDataProcessorRegistry(IEnumerable<IStaticDataTargetProcessor> processors)
    {
        _processors = processors.ToDictionary(
            x => x.TargetKey,
            x => x,
            StringComparer.OrdinalIgnoreCase);
    }

    public IStaticDataTargetProcessor GetRequired(string targetKey)
    {
        if (_processors.TryGetValue(targetKey, out var processor))
            return processor;

        throw new InvalidOperationException($"No processor registered for target '{targetKey}'.");
    }

    public IReadOnlyList<string> GetAvailableTargetKeys()
        => _processors.Keys.OrderBy(x => x).ToList();
}