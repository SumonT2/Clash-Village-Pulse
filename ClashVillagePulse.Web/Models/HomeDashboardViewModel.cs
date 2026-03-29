namespace ClashVillagePulse.Web.Models;

public sealed class HomeDashboardViewModel
{
    public bool IsAuthenticated { get; set; }

    public int TotalVillages { get; set; }
    public int ClanLinkedVillages { get; set; }
    public int ActiveUpgrades { get; set; }
    public int PendingSuggestions { get; set; }
    public int BusyHelpers { get; set; }
    public int VillagesWithoutPriority { get; set; }
    public int StaleVillages { get; set; }

    public string? LatestUploadVillageName { get; set; }
    public DateTime? LatestUploadAtUtc { get; set; }

    public Guid? FocusVillageId { get; set; }
    public string? FocusVillageName { get; set; }

    public IReadOnlyList<HomeDashboardVillageCardViewModel> Villages { get; set; }
        = Array.Empty<HomeDashboardVillageCardViewModel>();

    public IReadOnlyList<HomeDashboardSeriesPointViewModel> UploadTrend { get; set; }
        = Array.Empty<HomeDashboardSeriesPointViewModel>();

    public IReadOnlyList<HomeDashboardSeriesPointViewModel> TownHallDistribution { get; set; }
        = Array.Empty<HomeDashboardSeriesPointViewModel>();

    public IReadOnlyList<HomeDashboardRecommendationViewModel> Recommendations { get; set; }
        = Array.Empty<HomeDashboardRecommendationViewModel>();

    public static HomeDashboardViewModel CreateAnonymous() => new()
    {
        IsAuthenticated = false
    };

    public static HomeDashboardViewModel CreateEmptyForSignedInUser() => new()
    {
        IsAuthenticated = true
    };
}

public sealed class HomeDashboardVillageCardViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PlayerTag { get; set; } = string.Empty;

    public string? ClanName { get; set; }
    public string? ClanTag { get; set; }

    public int? TownHallLevel { get; set; }
    public int? BuilderHallLevel { get; set; }

    public DateTime LastUploadedAtUtc { get; set; }
    public DateTime? ExportedAtUtc { get; set; }

    public int PriorityItemCount { get; set; }
    public int PendingSuggestionCount { get; set; }
    public int ActiveUpgradeCount { get; set; }
    public int BusyHelperCount { get; set; }

    public string FreshnessLabel { get; set; } = "Fresh";
    public string FreshnessTone { get; set; } = "green";

    public int UrgencyScore { get; set; }
}

public sealed class HomeDashboardSeriesPointViewModel
{
    public string Label { get; set; } = string.Empty;
    public string ShortLabel { get; set; } = string.Empty;
    public int Value { get; set; }
}

public sealed class HomeDashboardRecommendationViewModel
{
    public string Tone { get; set; } = "blue";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionText { get; set; }
    public string? ActionUrl { get; set; }
}
