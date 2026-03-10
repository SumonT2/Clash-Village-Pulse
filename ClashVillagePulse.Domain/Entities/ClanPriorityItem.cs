using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class ClanPriorityItem
{
    public Guid Id { get; set; }

    public Guid ClanId { get; set; }

    public VillageSection Section { get; set; }

    // Use TownHallLevel for HomeVillage priorities
    public int? TownHallLevel { get; set; }

    // Use BuilderHallLevel for BuilderBase priorities
    public int? BuilderHallLevel { get; set; }

    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }

    public int PriorityRank { get; set; }

    public string CreatedByUserId { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public Clan Clan { get; set; } = null!;
}