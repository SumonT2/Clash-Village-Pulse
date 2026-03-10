using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.DTOs;

public sealed class VillageItemLevelDto
{
    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }
    public int Level { get; set; }
    public int Count { get; set; }
    public int? UpgradeTimerSeconds { get; set; }
}