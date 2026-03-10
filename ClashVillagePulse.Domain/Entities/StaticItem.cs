using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class StaticItem
{
    public Guid Id { get; set; }

    public int ItemDataId { get; set; }

    public ItemType ItemType { get; set; }

    public string Name { get; set; } = null!;

    public VillageSection Section { get; set; }

    public bool IsUpgradeable { get; set; }

    public ICollection<StaticItemLevel> Levels { get; set; } = new List<StaticItemLevel>();
}