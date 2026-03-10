using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class StaticItemLevelUpgradeCost
{
    public Guid Id { get; set; }

    public Guid StaticItemLevelId { get; set; }

    public StaticItemLevel StaticItemLevel { get; set; } = null!;

    public UpgradeResourceType ResourceType { get; set; }

    public long Amount { get; set; }
}