using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class VillagePriorityItem
{
    public Guid Id { get; set; }

    public Guid VillageId { get; set; }

    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }

    public int PriorityRank { get; set; }
    public string? Note { get; set; }

    public string CreatedByUserId { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public Village Village { get; set; } = null!;
}