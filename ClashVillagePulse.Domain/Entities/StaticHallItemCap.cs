using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class StaticHallItemCap
{
    public Guid Id { get; set; }

    public VillageSection Section { get; set; }

    // HomeVillage => TH level, BuilderBase => BH level
    public int HallLevel { get; set; }

    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }

    public int MaxCount { get; set; }
}