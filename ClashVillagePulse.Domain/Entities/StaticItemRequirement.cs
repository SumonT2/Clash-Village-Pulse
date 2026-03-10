using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class StaticItemRequirement
{
    public Guid Id { get; set; }

    public Guid StaticItemLevelId { get; set; }

    public StaticItemLevel StaticItemLevel { get; set; } = null!;

    public RequirementType RequirementType { get; set; }

    public int? RequiredItemDataId { get; set; }

    public int RequiredLevel { get; set; }
}