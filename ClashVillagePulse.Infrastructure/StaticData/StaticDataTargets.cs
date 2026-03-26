namespace ClashVillagePulse.Infrastructure.StaticData;

public static class StaticDataTargets
{
    public static readonly IReadOnlyList<StaticDataTarget> Targets =
        new List<StaticDataTarget>
        {
            new() { Key = "buildings", Path = "logic/buildings.csv" },
            new() { Key = "characters", Path = "logic/characters.csv" },
            new() { Key = "heroes", Path = "logic/heroes.csv" },
            new() { Key = "guardians", Path = "logic/characters.csv" },
            new() { Key = "spells", Path = "logic/spells.csv" },
            new() { Key = "pets", Path = "logic/pets.csv" },
            new() { Key = "equipment", Path = "logic/character_items.csv" },
            new() { Key = "texts", Path = "localization/texts.csv" },
            new() { Key = "townhall-levels", Path = "logic/townhall_levels.csv" },
            new() { Key = "traps", Path = "logic/traps.csv" },
            new() { Key = "helpers", Path = "logic/villager_apprentices.csv" }
        };
}