using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Domain.Entities;

public class StaticImageAsset
{
    public Guid Id { get; set; }

    public StaticImageSourceType SourceType { get; set; }

    public string SourceUrl { get; set; } = null!;

    public string? SourcePageUrl { get; set; }

    public string LocalPath { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string ContentHash { get; set; } = null!;

    public string? MimeType { get; set; }

    public DateTime DownloadedAtUtc { get; set; }

    public double MatchScore { get; set; }

    public string? MatchReason { get; set; }

    public bool IsPrimary { get; set; }

    public ICollection<StaticItemImage> ItemLinks { get; set; } = new List<StaticItemImage>();
}
