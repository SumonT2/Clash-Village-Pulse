namespace ClashVillagePulse.Infrastructure.StaticData.Models;

public class BuildingCsvRow
{
    public string Name { get; set; } = string.Empty;

    public int? GlobalId { get; set; }

    public int BuildingLevel { get; set; }

    public string? TID { get; set; }

    public string? BuildResource { get; set; }

    public long? BuildCost { get; set; }

    public string? AlternateResource { get; set; }

    public long? AlternateCost { get; set; }

    public int? TownHallLevel { get; set; }

    public int? CapitalHallLevel { get; set; }

    public int? BuildTimeD { get; set; }

    public int? BuildTimeH { get; set; }

    public int? BuildTimeM { get; set; }

    public int? BuildTimeS { get; set; }
}