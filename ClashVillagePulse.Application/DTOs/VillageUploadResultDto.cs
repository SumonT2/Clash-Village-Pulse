namespace ClashVillagePulse.Application.DTOs;

public sealed class VillageUploadResultDto
{
    public Guid VillageId { get; set; }
    public string PlayerTag { get; set; } = null!;
    public string VillageName { get; set; } = null!;
    public int TotalItemsImported { get; set; }
    public bool VillageCreated { get; set; }
    public bool ClanLinked { get; set; }
    public string? ClanTag { get; set; }
    public string? ClanName { get; set; }
}