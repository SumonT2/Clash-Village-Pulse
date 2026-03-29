namespace ClashVillagePulse.Domain.Entities;

public class StaticItemImage
{
    public Guid Id { get; set; }

    public Guid StaticItemId { get; set; }

    public StaticItem StaticItem { get; set; } = null!;

    public Guid? StaticItemLevelId { get; set; }

    public StaticItemLevel? StaticItemLevel { get; set; }

    public Guid StaticImageAssetId { get; set; }

    public StaticImageAsset StaticImageAsset { get; set; } = null!;

    public string AssetKind { get; set; } = "render";

    public int? MatchedLevel { get; set; }

    public bool IsPreferred { get; set; }
}
