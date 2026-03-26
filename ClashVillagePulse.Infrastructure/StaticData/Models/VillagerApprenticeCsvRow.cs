namespace ClashVillagePulse.Infrastructure.StaticData.Models;

public class VillagerApprenticeCsvRow
{
    public string Name { get; set; } = string.Empty;

    public int GlobalId { get; set; }

    public int Level { get; set; }

    public string? TID { get; set; }

    public string? InfoTID { get; set; }

    public int? RequiredTownHallLevel { get; set; }

    public string? HelperType { get; set; }

    public int? BoostMultiplier { get; set; }

    public int? BoostTimeSeconds { get; set; }

    public string? CostResource { get; set; }

    public long? Cost { get; set; }
}