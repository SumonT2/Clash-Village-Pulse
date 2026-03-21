namespace ClashVillagePulse.Infrastructure.StaticData.Models;

public class EquipmentCsvRow
{
    public string Name { get; set; } = string.Empty;

    public int? GlobalId { get; set; }

    public int Level { get; set; }

    public string? TID { get; set; }

    public string? AllowedCharacters { get; set; }

    public int? RequiredBlacksmithLevel { get; set; }

    public int? RequiredCharacterLevel { get; set; }

    public string? UpgradeResources { get; set; }

    public string? UpgradeCosts { get; set; }
}