using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.DTOs;

public sealed class VillageHelperStatusDto
{
    public VillageSection Section { get; set; } = VillageSection.HomeVillage;
    public ItemType ItemType { get; set; } = ItemType.Helper;
    public int ItemDataId { get; set; }
    public string ItemName { get; set; } = null!;

    public int Level { get; set; }
    public string HelperKind { get; set; } = null!;

    public string StatusLabel { get; set; } = null!;
    public string StatusTone { get; set; } = "secondary";

    public bool IsRecurring { get; set; }
    public string? RecurringText { get; set; }

    public int? UpgradeSecondsAtExport { get; set; }
    public DateTime? UpgradeFinishAtUtc { get; set; }

    public int? CooldownSecondsAtExport { get; set; }
    public DateTime? AvailableAtUtc { get; set; }

    public string? AssignmentLabel { get; set; }
    public ItemType? TargetItemType { get; set; }
    public int? TargetItemDataId { get; set; }
    public string? TargetItemName { get; set; }
    public int? TargetRemainingSecondsAtExport { get; set; }
    public bool TargetIsInferred { get; set; }
    public bool HasMultiplePossibleTargets { get; set; }
}
