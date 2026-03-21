namespace ClashVillagePulse.Infrastructure.StaticData.Models;

public class CharacterCsvRow
{
    public string Name { get; set; } = string.Empty;

    public int? GlobalId { get; set; }

    public int VisualLevel { get; set; }

    public string? TID { get; set; }

    public int? LaboratoryLevel { get; set; }

    public int? UnlockByTH { get; set; }

    public string? ProductionBuilding { get; set; }

    public string? UpgradeResource { get; set; }

    public long? UpgradeCost { get; set; }

    public int? UpgradeTimeH { get; set; }

    public int? UpgradeTimeM { get; set; }

    public string? VillageType { get; set; }
}