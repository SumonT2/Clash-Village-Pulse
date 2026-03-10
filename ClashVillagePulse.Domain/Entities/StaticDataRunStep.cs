using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class StaticDataRunStep
{
    public Guid Id { get; set; }

    public Guid StaticDataRunId { get; set; }

    public StaticDataRun StaticDataRun { get; set; } = null!;

    public string TargetKey { get; set; } = null!;

    public StaticDataStepType StepType { get; set; }

    public StaticDataStepStatus Status { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? Message { get; set; }
}