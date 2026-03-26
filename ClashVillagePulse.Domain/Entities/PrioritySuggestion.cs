using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class PrioritySuggestion
{
    public Guid Id { get; set; }

    public Guid VillageId { get; set; }

    public string SuggestedByUserId { get; set; } = null!;

    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }

    public int SuggestedPriorityRank { get; set; }

    public string? Message { get; set; }

    public SuggestionStatus Status { get; set; } = SuggestionStatus.Pending;

    public string? DecidedByUserId { get; set; }
    public DateTime? DecidedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Village Village { get; set; } = null!;
}