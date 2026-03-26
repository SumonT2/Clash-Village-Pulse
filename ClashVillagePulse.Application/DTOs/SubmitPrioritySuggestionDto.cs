using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.DTOs;

public sealed class SubmitPrioritySuggestionDto
{
    public Guid VillageId { get; set; }
    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }
    public int SuggestedPriorityRank { get; set; }
    public string? Message { get; set; }
}