namespace ClashVillagePulse.Application.DTOs;

public sealed class PlayerProfileDto
{
    public string PlayerTag { get; set; } = null!;
    public string? PlayerName { get; set; }
    public string? ClanTag { get; set; }
    public string? ClanName { get; set; }
    public int? TownHallLevel { get; set; }
    public int? BuilderHallLevel { get; set; }
}