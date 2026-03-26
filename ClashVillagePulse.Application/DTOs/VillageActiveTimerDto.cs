using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.DTOs;

public sealed class VillageActiveTimerDto
{
    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }
    public string ItemName { get; set; } = null!;

    public int Level { get; set; }
    public int? FromLevel { get; set; }
    public int? ToLevel { get; set; }
    public int Count { get; set; }

    public string TimerKind { get; set; } = null!;
    public string StatusLabel { get; set; } = null!;
    public string ProgressGroup { get; set; } = null!;

    public int RemainingSecondsAtExport { get; set; }
    public DateTime ExportedAtUtc { get; set; }
    public DateTime FinishAtUtc { get; set; }

    public bool IsFinishedByNow { get; set; }
    public bool IsStaleExport { get; set; }
    public bool IsHelperAssisted { get; set; }
}