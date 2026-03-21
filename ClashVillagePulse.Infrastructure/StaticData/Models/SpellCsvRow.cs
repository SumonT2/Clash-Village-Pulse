namespace ClashVillagePulse.Infrastructure.StaticData.Models;

public class SpellCsvRow
{
    public string Name { get; set; } = string.Empty;

    public int? GlobalId { get; set; }

    public int Level { get; set; }

    public string? TID { get; set; }

    public int? SpellForgeLevel { get; set; }

    public int? LaboratoryLevel { get; set; }

    public string? UpgradeResource { get; set; }

    public long? UpgradeCost { get; set; }

    public int? UpgradeTimeH { get; set; }

    public string? VillageType { get; set; }
}