using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class StaticDataRun
{
    public Guid Id { get; set; }

    public string Fingerprint { get; set; } = null!;

    public string RequestedByUserId { get; set; } = null!;

    public DateTime RequestedAtUtc { get; set; }

    public DateTime? StartedAtUtc { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public StaticDataRunStatus Status { get; set; }

    public string? Message { get; set; }

    public ICollection<StaticDataRunStep> Steps { get; set; } = new List<StaticDataRunStep>();
}