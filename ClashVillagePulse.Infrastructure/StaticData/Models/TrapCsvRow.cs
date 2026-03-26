namespace ClashVillagePulse.Infrastructure.StaticData.Models;

public class TrapCsvRow
{
    public string Name { get; set; } = string.Empty;
    public int? GlobalId { get; set; }
    public int Level { get; set; }
    public string? TID { get; set; }
    public string? BuildResource { get; set; }
    public long? BuildCost { get; set; }
    public int? TownHallLevel { get; set; }
    public int? BuildTimeD { get; set; }
    public int? BuildTimeH { get; set; }
    public int? BuildTimeM { get; set; }
    public int? BuildTimeS { get; set; }
    public string? VillageType { get; set; }
}