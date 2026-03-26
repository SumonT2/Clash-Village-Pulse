namespace ClashVillagePulse.Application.DTOs;

public sealed class VillageDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string PlayerTag { get; set; } = null!;
    public string? ClanName { get; set; }
    public string? ClanTag { get; set; }
    public int? TownHallLevel { get; set; }
    public int? BuilderHallLevel { get; set; }
    public DateTime LastUploadedAtUtc { get; set; }
    public DateTime? ExportedAtUtc { get; set; }

    public bool IsOwner { get; set; }
    public bool CanSuggestPriority { get; set; }
    public bool CanManageClanPriorityTemplate { get; set; }

    public IReadOnlyList<VillageHelperStatusDto> Helpers { get; set; } = Array.Empty<VillageHelperStatusDto>();
    public IReadOnlyList<VillageActiveTimerDto> ActiveTimers { get; set; } = Array.Empty<VillageActiveTimerDto>();
    public IReadOnlyList<VillageItemStateDto> ItemStates { get; set; } = Array.Empty<VillageItemStateDto>();
    public IReadOnlyList<PrioritySuggestionDto> PendingSuggestions { get; set; } = Array.Empty<PrioritySuggestionDto>();
}
