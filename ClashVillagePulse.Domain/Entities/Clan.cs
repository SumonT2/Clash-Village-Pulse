namespace ClashVillagePulse.Domain.Entities;

public class Clan
{
    public Guid Id { get; set; }

    public string ClanTag { get; set; } = null!;
    public string Name { get; set; } = null!;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<ClanMember> Members { get; set; } = new List<ClanMember>();
    public ICollection<Village> Villages { get; set; } = new List<Village>();
    public ICollection<ClanPriorityItem> PriorityItems { get; set; } = new List<ClanPriorityItem>();
}