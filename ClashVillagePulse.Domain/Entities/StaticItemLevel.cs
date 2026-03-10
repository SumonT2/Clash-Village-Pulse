namespace ClashVillagePulse.Domain.Entities;

public class StaticItemLevel
{
    public Guid Id { get; set; }

    public Guid StaticItemId { get; set; }

    public StaticItem StaticItem { get; set; } = null!;

    public int Level { get; set; }

    public int? UpgradeTimeSeconds { get; set; }

    public ICollection<StaticItemLevelUpgradeCost> UpgradeCosts { get; set; }
        = new List<StaticItemLevelUpgradeCost>();

    public ICollection<StaticItemRequirement> Requirements { get; set; }
        = new List<StaticItemRequirement>();
}