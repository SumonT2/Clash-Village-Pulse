using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.DTOs;

public sealed class PrioritySuggestionDto
{
    public Guid Id { get; set; }
    public Guid VillageId { get; set; }

    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }
    public string ItemName { get; set; } = null!;

    public int SuggestedPriorityRank { get; set; }
    public string? Message { get; set; }

    public string SuggestedByUserId { get; set; } = null!;
    public string SuggestedByDisplayName { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; }
}