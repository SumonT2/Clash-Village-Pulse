using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class VillageItemLevel
{
    public Guid Id { get; set; }

    public Guid VillageId { get; set; }

    public VillageSection Section { get; set; }
    public ItemType ItemType { get; set; }

    // Game asset id from upload JSON, such as 1000002 / 28000000 / 26000000
    public int ItemDataId { get; set; }

    public int Level { get; set; }

    // For grouped entries like cnt in buildings/traps/decos/obstacles
    public int Count { get; set; } = 1;

    // Optional upload metadata
    public int? UpgradeTimerSeconds { get; set; }
    public int? HelperCooldownSeconds { get; set; }
    public int? HelperTimerSeconds { get; set; }

    public bool IsExtra { get; set; }
    public bool IsGearUp { get; set; }
    public bool IsHelperRecurrent { get; set; }

    public int? SuperchargeLevel { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public Village Village { get; set; } = null!;
}