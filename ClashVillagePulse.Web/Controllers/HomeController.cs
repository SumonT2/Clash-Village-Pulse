using System.Diagnostics;
using System.Security.Claims;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.Database;
using ClashVillagePulse.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Web.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Title"] = "Dashboard";

        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return View(HomeDashboardViewModel.CreateAnonymous());
        }

        var villages = await _db.Villages
            .AsNoTracking()
            .Where(x => x.OwnerUserId == userId && !x.IsArchived)
            .OrderByDescending(x => x.LastUploadedAtUtc)
            .ThenBy(x => x.Name)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.PlayerTag,
                x.ClanName,
                x.ClanTag,
                x.TownHallLevel,
                x.BuilderHallLevel,
                x.LastUploadedAtUtc,
                x.LastGameTimestamp
            })
            .ToListAsync(cancellationToken);

        if (villages.Count == 0)
        {
            return View(HomeDashboardViewModel.CreateEmptyForSignedInUser());
        }

        var villageIds = villages.Select(x => x.Id).ToList();

        var priorityCounts = await _db.VillagePriorityItems
            .AsNoTracking()
            .Where(x => villageIds.Contains(x.VillageId))
            .GroupBy(x => x.VillageId)
            .Select(g => new { VillageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VillageId, x => x.Count, cancellationToken);

        var pendingSuggestionCounts = await _db.PrioritySuggestions
            .AsNoTracking()
            .Where(x => villageIds.Contains(x.VillageId) && x.Status == SuggestionStatus.Pending)
            .GroupBy(x => x.VillageId)
            .Select(g => new { VillageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VillageId, x => x.Count, cancellationToken);

        var activeUpgradeCounts = await _db.VillageItemLevels
            .AsNoTracking()
            .Where(x => villageIds.Contains(x.VillageId) && x.UpgradeTimerSeconds.HasValue && x.UpgradeTimerSeconds.Value > 0)
            .GroupBy(x => x.VillageId)
            .Select(g => new { VillageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VillageId, x => x.Count, cancellationToken);

        var busyHelperCounts = await _db.VillageItemLevels
            .AsNoTracking()
            .Where(x => villageIds.Contains(x.VillageId)
                && x.ItemType == ItemType.Helper
                && (
                    (x.UpgradeTimerSeconds.HasValue && x.UpgradeTimerSeconds.Value > 0) ||
                    (x.HelperCooldownSeconds.HasValue && x.HelperCooldownSeconds.Value > 0) ||
                    (x.HelperTimerSeconds.HasValue && x.HelperTimerSeconds.Value > 0)
                ))
            .GroupBy(x => x.VillageId)
            .Select(g => new { VillageId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.VillageId, x => x.Count, cancellationToken);

        var nowUtc = DateTime.UtcNow;

        static (string freshness, string tone, int urgency) GetFreshness(DateTime activityAtUtc, int pendingSuggestions, int priorityCount)
        {
            var age = DateTime.UtcNow - activityAtUtc;

            if (age.TotalDays <= 1)
            {
                return ("Fresh", "green", pendingSuggestions * 3 + (priorityCount == 0 ? 2 : 0));
            }

            if (age.TotalDays <= 7)
            {
                return ("Aging", "orange", 3 + pendingSuggestions * 3 + (priorityCount == 0 ? 2 : 0));
            }

            return ("Stale", "red", 6 + pendingSuggestions * 3 + (priorityCount == 0 ? 2 : 0));
        }

        var villageCards = villages
            .Select(v =>
            {
                var exportedAtUtc = v.LastGameTimestamp.HasValue
                    ? DateTimeOffset.FromUnixTimeSeconds(v.LastGameTimestamp.Value).UtcDateTime
                    : (DateTime?)null;

                var referenceAt = exportedAtUtc ?? v.LastUploadedAtUtc;
                var priorityCount = priorityCounts.GetValueOrDefault(v.Id);
                var pendingCount = pendingSuggestionCounts.GetValueOrDefault(v.Id);
                var activeCount = activeUpgradeCounts.GetValueOrDefault(v.Id);
                var helperCount = busyHelperCounts.GetValueOrDefault(v.Id);
                var freshness = GetFreshness(referenceAt, pendingCount, priorityCount);

                var urgencyScore = freshness.urgency
                    + activeCount
                    + helperCount
                    + (string.IsNullOrWhiteSpace(v.ClanTag) ? 1 : 0);

                return new HomeDashboardVillageCardViewModel
                {
                    Id = v.Id,
                    Name = v.Name,
                    PlayerTag = v.PlayerTag,
                    ClanName = v.ClanName,
                    ClanTag = v.ClanTag,
                    TownHallLevel = v.TownHallLevel,
                    BuilderHallLevel = v.BuilderHallLevel,
                    LastUploadedAtUtc = v.LastUploadedAtUtc,
                    ExportedAtUtc = exportedAtUtc,
                    PriorityItemCount = priorityCount,
                    PendingSuggestionCount = pendingCount,
                    ActiveUpgradeCount = activeCount,
                    BusyHelperCount = helperCount,
                    FreshnessLabel = freshness.freshness,
                    FreshnessTone = freshness.tone,
                    UrgencyScore = urgencyScore
                };
            })
            .OrderByDescending(x => x.UrgencyScore)
            .ThenByDescending(x => x.PendingSuggestionCount)
            .ThenByDescending(x => x.ActiveUpgradeCount)
            .ThenBy(x => x.Name)
            .ToList();

        var totalVillages = villageCards.Count;
        var clanLinkedVillages = villageCards.Count(x => !string.IsNullOrWhiteSpace(x.ClanTag) || !string.IsNullOrWhiteSpace(x.ClanName));
        var activeUpgrades = villageCards.Sum(x => x.ActiveUpgradeCount);
        var pendingSuggestions = villageCards.Sum(x => x.PendingSuggestionCount);
        var busyHelpers = villageCards.Sum(x => x.BusyHelperCount);
        var villagesWithoutPriority = villageCards.Count(x => x.PriorityItemCount == 0);
        var staleVillages = villageCards.Count(x => string.Equals(x.FreshnessLabel, "Stale", StringComparison.OrdinalIgnoreCase));

        var latestUpload = villageCards.OrderByDescending(x => x.LastUploadedAtUtc).FirstOrDefault();
        var focusVillage = villageCards.OrderByDescending(x => x.UrgencyScore).FirstOrDefault();

        var uploadTrend = Enumerable.Range(0, 14)
            .Select(offset =>
            {
                var day = nowUtc.Date.AddDays(-13 + offset);
                var count = villageCards.Count(x => x.LastUploadedAtUtc.Date == day);
                return new HomeDashboardSeriesPointViewModel
                {
                    Label = day.ToString("dd MMM"),
                    ShortLabel = day.ToString("dd"),
                    Value = count
                };
            })
            .ToList();

        var townHallDistribution = villageCards
            .Where(x => x.TownHallLevel.HasValue && x.TownHallLevel.Value > 0)
            .GroupBy(x => x.TownHallLevel!.Value)
            .OrderByDescending(x => x.Key)
            .Select(g => new HomeDashboardSeriesPointViewModel
            {
                Label = $"TH {g.Key}",
                ShortLabel = g.Key.ToString(),
                Value = g.Count()
            })
            .ToList();

        var recommendationTarget = villageCards
            .OrderByDescending(x => x.PendingSuggestionCount)
            .ThenByDescending(x => x.UrgencyScore)
            .FirstOrDefault();

        var recommendations = new List<HomeDashboardRecommendationViewModel>();

        if (pendingSuggestions > 0 && recommendationTarget is not null)
        {
            recommendations.Add(new HomeDashboardRecommendationViewModel
            {
                Tone = "purple",
                Title = "Review pending suggestions",
                Message = $"{pendingSuggestions} village suggestion(s) are waiting. Start with {recommendationTarget.Name} to keep your upgrade queue aligned.",
                ActionText = "Open village",
                ActionUrl = Url.Action("Details", "Village", new { id = recommendationTarget.Id })
            });
        }

        if (villagesWithoutPriority > 0)
        {
            var target = villageCards.FirstOrDefault(x => x.PriorityItemCount == 0);
            recommendations.Add(new HomeDashboardRecommendationViewModel
            {
                Tone = "orange",
                Title = "Set priority for uncovered villages",
                Message = $"{villagesWithoutPriority} village(s) still have no personal queue. A priority list will make Details and recommendations much more useful.",
                ActionText = target is null ? "Open villages" : "Set priority",
                ActionUrl = target is null
                    ? Url.Action("Index", "Village")
                    : Url.Action("Edit", "VillagePriority", new { villageId = target.Id })
            });
        }

        if (staleVillages > 0)
        {
            var target = villageCards.FirstOrDefault(x => x.FreshnessTone == "red");
            recommendations.Add(new HomeDashboardRecommendationViewModel
            {
                Tone = "red",
                Title = "Refresh stale export data",
                Message = $"{staleVillages} village(s) have stale export snapshots. Upload a new export or sync profile details before acting on timers.",
                ActionText = "Upload export",
                ActionUrl = Url.Action("Index", "VillageUpload")
            });
        }

        if (clanLinkedVillages < totalVillages)
        {
            recommendations.Add(new HomeDashboardRecommendationViewModel
            {
                Tone = "blue",
                Title = "Improve clan coverage",
                Message = $"{totalVillages - clanLinkedVillages} village(s) are missing clan linkage. Syncing profile metadata will make clan collaboration much easier.",
                ActionText = "View villages",
                ActionUrl = Url.Action("Index", "Village")
            });
        }

        if (activeUpgrades > 0 && focusVillage is not null)
        {
            recommendations.Add(new HomeDashboardRecommendationViewModel
            {
                Tone = "green",
                Title = "Use the dashboard as a daily checklist",
                Message = $"{activeUpgrades} active upgrade timer(s) are already in flight. {focusVillage.Name} currently has the highest urgency score.",
                ActionText = "View focus village",
                ActionUrl = Url.Action("Details", "Village", new { id = focusVillage.Id })
            });
        }

        var model = new HomeDashboardViewModel
        {
            IsAuthenticated = true,
            TotalVillages = totalVillages,
            ClanLinkedVillages = clanLinkedVillages,
            ActiveUpgrades = activeUpgrades,
            PendingSuggestions = pendingSuggestions,
            BusyHelpers = busyHelpers,
            VillagesWithoutPriority = villagesWithoutPriority,
            StaleVillages = staleVillages,
            LatestUploadVillageName = latestUpload?.Name,
            LatestUploadAtUtc = latestUpload?.LastUploadedAtUtc,
            FocusVillageName = focusVillage?.Name,
            FocusVillageId = focusVillage?.Id,
            UploadTrend = uploadTrend,
            TownHallDistribution = townHallDistribution,
            Villages = villageCards,
            Recommendations = recommendations
        };

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
