namespace ClashVillagePulse.Application.DTOs;

public sealed class VillageListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string PlayerTag { get; set; } = null!;
    public string? ClanName { get; set; }
    public string? ClanTag { get; set; }
    public string? OwnerDisplayName { get; set; }
    public DateTime LastUploadedAtUtc { get; set; }
}