using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.DTOs;

public sealed class VillageItemStateDto
{
    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }
    public int ItemDataId { get; set; }
    public string ItemName { get; set; } = null!;

    public int CurrentTotalCount { get; set; }
    public string CurrentLevelText { get; set; } = null!;
    public int CurrentMinLevel { get; set; }
    public int CurrentMaxLevel { get; set; }

    public int? MaxLevelAtCurrentHall { get; set; }
    public int? GlobalMaxLevel { get; set; }

    public int? MaxCountAtCurrentHall { get; set; }
    public int? GlobalMaxCount { get; set; }

    public int? VillagePriorityRank { get; set; }
    public int? ClanPriorityRank { get; set; }
    public int? EffectivePriorityRank { get; set; }
    public string? EffectivePrioritySource { get; set; }

    public bool HasPendingSuggestion { get; set; }

    public IReadOnlyList<VillageItemLevelDto> LevelBuckets { get; set; }
        = Array.Empty<VillageItemLevelDto>();
}