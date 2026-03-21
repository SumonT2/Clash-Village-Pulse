using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Infrastructure.StaticData.Mapping;

public static class UpgradeResourceMapper
{
    public static UpgradeResourceType Map(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return UpgradeResourceType.Unknown;

        return value.Trim().ToLowerInvariant() switch
        {
            "gold" => UpgradeResourceType.Gold,
            "elixir" => UpgradeResourceType.Elixir,
            "darkelixir" => UpgradeResourceType.DarkElixir,
            "dark_elixir" => UpgradeResourceType.DarkElixir,

            "buildergold" => UpgradeResourceType.BuilderGold,
            "builder_gold" => UpgradeResourceType.BuilderGold,
            "gold2" => UpgradeResourceType.BuilderGold,

            "builderelixir" => UpgradeResourceType.BuilderElixir,
            "builder_elixir" => UpgradeResourceType.BuilderElixir,
            "elixir2" => UpgradeResourceType.BuilderElixir,

            "capitalgold" => UpgradeResourceType.CapitalGold,
            "capital_gold" => UpgradeResourceType.CapitalGold,

            "shinyore" => UpgradeResourceType.ShinyOre,
            "shiny_ore" => UpgradeResourceType.ShinyOre,
            "commonore" => UpgradeResourceType.ShinyOre,
            "common_ore" => UpgradeResourceType.ShinyOre,

            "glowyore" => UpgradeResourceType.GlowyOre,
            "glowy_ore" => UpgradeResourceType.GlowyOre,
            "rareore" => UpgradeResourceType.GlowyOre,
            "rare_ore" => UpgradeResourceType.GlowyOre,

            "starryore" => UpgradeResourceType.StarryOre,
            "starry_ore" => UpgradeResourceType.StarryOre,
            "epicore" => UpgradeResourceType.StarryOre,
            "epic_ore" => UpgradeResourceType.StarryOre,

            "diamonds" => UpgradeResourceType.Gem,

            _ => UpgradeResourceType.Unknown
        };
    }
}