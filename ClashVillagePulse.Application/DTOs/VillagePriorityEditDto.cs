namespace ClashVillagePulse.Application.DTOs;

public sealed class VillagePriorityEditDto
{
    public Guid VillageId { get; set; }
    public string VillageName { get; set; } = null!;
    public List<VillageItemStateDto> Items { get; set; } = new();
    public IReadOnlyList<VillageListItemDto> SuggestionTargets { get; set; } = Array.Empty<VillageListItemDto>();
}