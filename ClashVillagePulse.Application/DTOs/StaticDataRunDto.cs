namespace ClashVillagePulse.Application.DTOs;

public class StaticDataRunDto
{
    public Guid Id { get; set; }

    public string Fingerprint { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public List<StaticDataRunStepDto> Steps { get; set; } = new();
}

public class StaticDataRunStepDto
{
    public string TargetKey { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}