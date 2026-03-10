using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class ClanMember
{
    public Guid Id { get; set; }

    public Guid ClanId { get; set; }
    public string UserId { get; set; } = null!;

    public ClanRole Role { get; set; } = ClanRole.Member;

    public DateTime JoinedAtUtc { get; set; } = DateTime.UtcNow;

    public Clan Clan { get; set; } = null!;
}