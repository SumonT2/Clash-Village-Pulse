using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.DTOs;

public sealed class SavePriorityItemDto
{
    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }
    public int? PriorityRank { get; set; }
    public string? Note { get; set; }
}