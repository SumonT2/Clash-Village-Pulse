using ClashVillagePulse.Domain.Enums;

public static class VillageTypeResolver
{
    public static VillageSection ResolveSection(string? villageType)
    {
        if (string.IsNullOrWhiteSpace(villageType))
            return VillageSection.HomeVillage;

        var value = villageType.Trim().ToLowerInvariant();

        if (value == "1")
            return VillageSection.BuilderBase;

        return VillageSection.HomeVillage;
    }

    public static RequirementType ResolveTownHallRequirement(string? villageType)
    {
        return ResolveSection(villageType) == VillageSection.BuilderBase
            ? RequirementType.BuilderHall
            : RequirementType.TownHall;
    }
}