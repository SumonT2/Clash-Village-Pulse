namespace ClashVillagePulse.Infrastructure.StaticData.Models;

public class HeroCsvRow
{
    public string Name { get; set; } = string.Empty;

    public int? GlobalId { get; set; }

    public int VisualLevel { get; set; }

    public string? TID { get; set; }

    public int? RequiredTownHallLevel { get; set; }

    public int? RequiredHeroTavernLevel { get; set; }

    public string? UpgradeResource { get; set; }

    public long? UpgradeCost { get; set; }

    public int? UpgradeTimeH { get; set; }

    public string? VillageType { get; set; }
}