namespace ClashVillagePulse.Domain.Entities;

public class Village
{
    public Guid Id { get; set; }

    public string OwnerUserId { get; set; } = null!;

    public Guid? ClanId { get; set; }

    public string PlayerTag { get; set; } = null!;
    public string Name { get; set; } = null!;

    public string? ClanTag { get; set; }
    public string? ClanName { get; set; }

    public int? TownHallLevel { get; set; }
    public int? BuilderHallLevel { get; set; }

    public DateTime LastUploadedAtUtc { get; set; } = DateTime.UtcNow;
    public long? LastGameTimestamp { get; set; }

    public bool IsArchived { get; set; }

    public Clan? Clan { get; set; }

    public ICollection<VillageItemLevel> ItemLevels { get; set; } = new List<VillageItemLevel>();
    public ICollection<VillagePriorityItem> PriorityItems { get; set; } = new List<VillagePriorityItem>();
    public ICollection<PrioritySuggestion> Suggestions { get; set; } = new List<PrioritySuggestion>();
}