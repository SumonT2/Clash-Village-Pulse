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
    public IReadOnlyList<VillageItemLevelDto> Items { get; set; } = Array.Empty<VillageItemLevelDto>();
}