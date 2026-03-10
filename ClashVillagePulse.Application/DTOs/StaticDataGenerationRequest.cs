namespace ClashVillagePulse.Application.DTOs;

public class StaticDataGenerationRequest
{
    public string Fingerprint { get; set; } = string.Empty;

    public IReadOnlyList<string> Targets { get; set; } = new List<string>();
}