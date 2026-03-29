using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.Services;

public sealed class VillageQueryService : IVillageQueryService
{
    private readonly AppDbContext _db;

    public VillageQueryService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<VillageListItemDto>> GetMyVillagesAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Villages
            .AsNoTracking()
            .Where(x => x.OwnerUserId == ownerUserId && !x.IsArchived)
            .OrderBy(x => x.Name)
            .Select(x => new VillageListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                PlayerTag = x.PlayerTag,
                ClanName = x.ClanName,
                ClanTag = x.ClanTag,
                LastUploadedAtUtc = x.LastUploadedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VillageListItemDto>> GetClanVillagesAsync(
        string viewerUserId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from v in _db.Villages.AsNoTracking()
            join cm in _db.ClanMembers.AsNoTracking() on v.ClanId equals cm.ClanId
            join u in _db.Users.AsNoTracking() on v.OwnerUserId equals u.Id into ownerJoin
            from owner in ownerJoin.DefaultIfEmpty()
            where cm.UserId == viewerUserId && !v.IsArchived
            orderby v.ClanName, v.Name
            select new VillageListItemDto
            {
                Id = v.Id,
                Name = v.Name,
                PlayerTag = v.PlayerTag,
                ClanName = v.ClanName,
                ClanTag = v.ClanTag,
                OwnerDisplayName = owner != null
                    ? (owner.UserName ?? owner.Email ?? owner.Id)
                    : v.OwnerUserId,
                LastUploadedAtUtc = v.LastUploadedAtUtc
            };

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<VillageDetailsDto?> GetVillageDetailsAsync(
        string viewerUserId,
        Guid villageId,
        CancellationToken cancellationToken = default)
    {
        var village = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == villageId && !x.IsArchived, cancellationToken);

        if (village is null)
            return null;

        bool isOwner = village.OwnerUserId == viewerUserId;
        bool isClanMember = village.ClanId.HasValue && await _db.ClanMembers
            .AsNoTracking()
            .AnyAsync(x => x.ClanId == village.ClanId.Value && x.UserId == viewerUserId, cancellationToken);

        if (!isOwner && !isClanMember)
            return null;

        var itemStates = await BuildItemStatesAsync(village, cancellationToken);
        var helpers = await BuildHelperStatusesAsync(village, cancellationToken);
        var activeTimers = await BuildActiveTimersAsync(village, cancellationToken);

        var pendingSuggestions = Array.Empty<PrioritySuggestionDto>();

        if (isOwner)
        {
            var pending = await _db.PrioritySuggestions
                .AsNoTracking()
                .Where(x => x.VillageId == village.Id && x.Status == SuggestionStatus.Pending)
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken);

            var itemNameMap = itemStates.ToDictionary(
                x => (x.Section, x.ItemType, x.ItemDataId),
                x => x.ItemName);

            var suggesterIds = pending
                .Select(x => x.SuggestedByUserId)
                .Distinct()
                .ToList();

            var userMap = await _db.Users
                .AsNoTracking()
                .Where(x => suggesterIds.Contains(x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.UserName ?? x.Email ?? x.Id,
                    cancellationToken);

            pendingSuggestions = pending
                .Select(x => new PrioritySuggestionDto
                {
                    Id = x.Id,
                    VillageId = x.VillageId,
                    Section = x.Section,
                    ItemType = x.ItemType,
                    ItemDataId = x.ItemDataId,
                    ItemName = itemNameMap.TryGetValue((x.Section, x.ItemType, x.ItemDataId), out var name)
                        ? name
                        : $"{x.ItemType} #{x.ItemDataId}",
                    SuggestedPriorityRank = x.SuggestedPriorityRank,
                    Message = x.Message,
                    SuggestedByUserId = x.SuggestedByUserId,
                    SuggestedByDisplayName = userMap.TryGetValue(x.SuggestedByUserId, out var display)
                        ? display
                        : x.SuggestedByUserId,
                    CreatedAtUtc = x.CreatedAtUtc
                })
                .ToArray();
        }

        DateTime? exportedAtUtc = village.LastGameTimestamp.HasValue
            ? DateTimeOffset.FromUnixTimeSeconds(village.LastGameTimestamp.Value).UtcDateTime
            : null;

        return new VillageDetailsDto
        {
            Id = village.Id,
            Name = village.Name,
            PlayerTag = village.PlayerTag,
            ClanName = village.ClanName,
            ClanTag = village.ClanTag,
            TownHallLevel = village.TownHallLevel,
            BuilderHallLevel = village.BuilderHallLevel,
            LastUploadedAtUtc = village.LastUploadedAtUtc,
            ExportedAtUtc = exportedAtUtc,
            IsOwner = isOwner,
            CanSuggestPriority = !isOwner && isClanMember,
            CanManageClanPriorityTemplate = village.ClanId.HasValue && isClanMember,
            Helpers = helpers,
            ActiveTimers = activeTimers,
            ItemStates = itemStates,
            PendingSuggestions = pendingSuggestions
        };
    }

    public async Task<VillagePriorityEditDto?> GetVillagePriorityEditAsync(
        string ownerUserId,
        Guid villageId,
        CancellationToken cancellationToken = default)
    {
        var village = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == villageId && x.OwnerUserId == ownerUserId && !x.IsArchived, cancellationToken);

        if (village is null)
            return null;

        IReadOnlyList<VillageListItemDto> suggestionTargets = Array.Empty<VillageListItemDto>();

        suggestionTargets = await (
            from target in _db.Villages.AsNoTracking()
            join u in _db.Users.AsNoTracking() on target.OwnerUserId equals u.Id into ownerJoin
            from owner in ownerJoin.DefaultIfEmpty()
            where !target.IsArchived
                && target.Id != village.Id
                && (
                    target.OwnerUserId == ownerUserId ||
                    (village.ClanId.HasValue && target.ClanId == village.ClanId.Value)
                )
            orderby target.OwnerUserId == ownerUserId ? 0 : 1, target.Name
            select new VillageListItemDto
            {
                Id = target.Id,
                Name = target.Name,
                PlayerTag = target.PlayerTag,
                ClanName = target.ClanName,
                ClanTag = target.ClanTag,
                OwnerDisplayName = owner != null
                    ? (owner.UserName ?? owner.Email ?? owner.Id)
                    : target.OwnerUserId,
                LastUploadedAtUtc = target.LastUploadedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new VillagePriorityEditDto
        {
            VillageId = village.Id,
            VillageName = village.Name,
            Items = await BuildItemStatesAsync(village, cancellationToken),
            SuggestionTargets = suggestionTargets
        };
    }

    public async Task<ClanPriorityTemplateDto?> GetClanPriorityTemplateAsync(
        string userId,
        Guid villageId,
        VillageSection section,
        CancellationToken cancellationToken = default)
    {
        var village = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == villageId && !x.IsArchived, cancellationToken);

        if (village is null || !village.ClanId.HasValue)
            return null;

        bool isClanMember = await _db.ClanMembers
            .AsNoTracking()
            .AnyAsync(x => x.ClanId == village.ClanId.Value && x.UserId == userId, cancellationToken);

        if (!isClanMember)
            return null;

        int? hallLevel = section == VillageSection.HomeVillage
            ? village.TownHallLevel
            : village.BuilderHallLevel;

        if (!hallLevel.HasValue || hallLevel.Value <= 0)
            return null;

        var items = await BuildItemStatesAsync(village, cancellationToken);

        return new ClanPriorityTemplateDto
        {
            VillageId = village.Id,
            ClanId = village.ClanId.Value,
            ClanName = village.ClanName ?? village.ClanTag ?? "Clan",
            Section = section,
            HallLevel = hallLevel.Value,
            Items = items.Where(x => x.Section == section).ToList()
        };
    }

    private async Task<List<VillageHelperStatusDto>> BuildHelperStatusesAsync(
        Village village,
        CancellationToken cancellationToken)
    {
        if (!village.LastGameTimestamp.HasValue)
            return new List<VillageHelperStatusDto>();

        const int recurringCycleSeconds = 24 * 60 * 60;

        var exportedAtUtc = DateTimeOffset.FromUnixTimeSeconds(village.LastGameTimestamp.Value).UtcDateTime;
        var nowUtc = DateTime.UtcNow;

        var helperRows = await _db.VillageItemLevels
            .AsNoTracking()
            .Where(x => x.VillageId == village.Id && x.ItemType == ItemType.Helper)
            .OrderBy(x => x.ItemDataId)
            .ToListAsync(cancellationToken);

        if (helperRows.Count == 0)
            return new List<VillageHelperStatusDto>();

        var targetRows = await _db.VillageItemLevels
            .AsNoTracking()
            .Where(x =>
                x.VillageId == village.Id &&
                x.IsHelperRecurrent &&
                x.UpgradeTimerSeconds.HasValue &&
                x.UpgradeTimerSeconds.Value > 0)
            .ToListAsync(cancellationToken);

        var itemDataIds = helperRows
            .Select(x => x.ItemDataId)
            .Concat(targetRows.Select(x => x.ItemDataId))
            .Distinct()
            .ToList();

        var staticItems = await _db.StaticItems
            .AsNoTracking()
            .Include(x => x.Levels)
            .Where(x => itemDataIds.Contains(x.ItemDataId))
            .ToListAsync(cancellationToken);

        var staticMap = staticItems
            .GroupBy(x => (x.Section, x.ItemType, x.ItemDataId))
            .ToDictionary(x => x.Key, x => x.First());

        var builderCandidates = targetRows
            .Where(IsBuilderHelperTarget)
            .OrderByDescending(x => x.UpgradeTimerSeconds)
            .ThenBy(x => x.ItemDataId)
            .ToList();

        var labCandidates = targetRows
            .Where(IsLabHelperTarget)
            .OrderByDescending(x => x.UpgradeTimerSeconds)
            .ThenBy(x => x.ItemDataId)
            .ToList();

        var result = new List<VillageHelperStatusDto>(helperRows.Count);

        foreach (var row in helperRows)
        {
            staticMap.TryGetValue((row.Section, row.ItemType, row.ItemDataId), out var helperStaticItem);

            var helperKind = ResolveHelperKind(row.ItemDataId);
            var helperName = helperStaticItem?.Name ?? ResolveHelperDisplayName(row.ItemDataId);
            var helperStaticLevel = helperStaticItem?.Levels.FirstOrDefault(x => x.Level == row.Level);

            var upgradeSeconds = row.UpgradeTimerSeconds is > 0 ? row.UpgradeTimerSeconds : null;
            var cooldownSeconds = row.HelperCooldownSeconds is > 0 ? row.HelperCooldownSeconds : null;
            var upgradeFinishAtUtc = upgradeSeconds.HasValue ? exportedAtUtc.AddSeconds(upgradeSeconds.Value) : (DateTime?)null;
            var cooldownFinishAtUtc = cooldownSeconds.HasValue ? exportedAtUtc.AddSeconds(cooldownSeconds.Value) : (DateTime?)null;

            var boostTimeSeconds = helperStaticLevel?.BoostTimeSeconds is > 0 ? helperStaticLevel.BoostTimeSeconds : null;
            var boostMultiplier = helperStaticLevel?.BoostMultiplier is > 0 ? helperStaticLevel.BoostMultiplier : null;
            var savedSecondsPerCycle = boostTimeSeconds.HasValue && boostMultiplier.HasValue
                ? boostTimeSeconds.Value * boostMultiplier.Value
                : (int?)null;

            var candidates = helperKind switch
            {
                "Builder" => builderCandidates,
                "Research" => labCandidates,
                _ => new List<VillageItemLevel>()
            };

            var candidateInfos = candidates
                .Select(candidate => new
                {
                    Row = candidate,
                    FinishAtUtc = candidate.UpgradeTimerSeconds is > 0
                        ? exportedAtUtc.AddSeconds(candidate.UpgradeTimerSeconds.Value)
                        : (DateTime?)null
                })
                .ToList();

            var activeTargetInfos = candidateInfos
                .Where(x => x.FinishAtUtc.HasValue && x.FinishAtUtc.Value > nowUtc)
                .ToList();

            var hasActiveTarget = activeTargetInfos.Count > 0;
            var hasMultiplePossibleTargets = candidateInfos.Count > 1;

            var representativeTarget = !hasMultiplePossibleTargets && candidateInfos.Count == 1
                ? candidateInfos[0]
                : activeTargetInfos
                    .OrderByDescending(x => x.FinishAtUtc)
                    .ThenBy(x => x.Row.ItemDataId)
                    .FirstOrDefault()
                  ?? candidateInfos
                    .OrderByDescending(x => x.FinishAtUtc)
                    .ThenBy(x => x.Row.ItemDataId)
                    .FirstOrDefault();

            var resolvedTarget = !hasMultiplePossibleTargets && candidateInfos.Count == 1
                ? candidateInfos[0].Row
                : null;

            string? targetItemName = null;
            if (resolvedTarget is not null &&
                staticMap.TryGetValue((resolvedTarget.Section, resolvedTarget.ItemType, resolvedTarget.ItemDataId), out var targetStaticItem))
            {
                targetItemName = targetStaticItem.Name;
            }

            string? assignmentLabel = resolvedTarget is not null
                ? targetItemName ?? BuildFallbackTargetLabel(resolvedTarget)
                : hasActiveTarget
                    ? BuildGenericAssignmentLabel(helperKind)
                    : null;

            var targetFinishAtUtc = representativeTarget?.FinishAtUtc;
            DateTime? estimatedTargetFinishAtUtc = null;
            int? estimatedFutureSavedSeconds = null;

            if (hasActiveTarget &&
                cooldownFinishAtUtc.HasValue &&
                savedSecondsPerCycle.HasValue &&
                boostTimeSeconds.HasValue &&
                boostTimeSeconds.Value > 0 &&
                boostMultiplier.HasValue &&
                boostMultiplier.Value > 0 &&
                targetFinishAtUtc.HasValue)
            {
                estimatedTargetFinishAtUtc = EstimateRecurringFinishAtUtc(
                    exportedAtUtc,
                    representativeTarget!.Row.UpgradeTimerSeconds!.Value,
                    cooldownFinishAtUtc.Value,
                    boostTimeSeconds.Value,
                    savedSecondsPerCycle.Value,
                    recurringCycleSeconds);

                if (estimatedTargetFinishAtUtc.HasValue && estimatedTargetFinishAtUtc.Value < targetFinishAtUtc.Value)
                {
                    estimatedFutureSavedSeconds = (int)Math.Max(0, Math.Round((targetFinishAtUtc.Value - estimatedTargetFinishAtUtc.Value).TotalSeconds));
                }
            }

            var helperBusyUntilUtc = estimatedTargetFinishAtUtc
                ?? targetFinishAtUtc;

            var trueAvailableAtUtc = hasActiveTarget
                ? helperBusyUntilUtc
                : cooldownFinishAtUtc;

            var isRecurring = hasActiveTarget || candidateInfos.Count > 0;

            string statusLabel;
            string statusTone;
            string? recurringText = null;

            if (upgradeSeconds.HasValue)
            {
                statusLabel = "Leveling";
                statusTone = "orange";
            }
            else if (hasActiveTarget)
            {
                statusLabel = "Helping recurrent";
                statusTone = "azure";

                if (savedSecondsPerCycle.HasValue && cooldownFinishAtUtc.HasValue)
                {
                    recurringText = estimatedFutureSavedSeconds is > 0
                        ? $"Next reset at {cooldownFinishAtUtc.Value:yyyy-MM-dd HH:mm} UTC. Saves {FormatCompactDuration(savedSecondsPerCycle.Value)} per cycle. Estimated to save {FormatCompactDuration(estimatedFutureSavedSeconds.Value)} more before the target finishes."
                        : $"Next reset at {cooldownFinishAtUtc.Value:yyyy-MM-dd HH:mm} UTC. Saves {FormatCompactDuration(savedSecondsPerCycle.Value)} per cycle until the target upgrade finishes.";
                }
                else if (cooldownFinishAtUtc.HasValue)
                {
                    recurringText = $"Next reset at {cooldownFinishAtUtc.Value:yyyy-MM-dd HH:mm} UTC. Recurring support remains active until the target upgrade finishes.";
                }
                else
                {
                    recurringText = "Recurring support active until the target upgrade finishes.";
                }
            }
            else if (cooldownFinishAtUtc.HasValue && cooldownFinishAtUtc.Value > nowUtc)
            {
                statusLabel = "Cooldown";
                statusTone = "yellow";
            }
            else
            {
                statusLabel = "Available";
                statusTone = "green";
            }

            result.Add(new VillageHelperStatusDto
            {
                Section = row.Section,
                ItemType = row.ItemType,
                ItemDataId = row.ItemDataId,
                ItemName = helperName,
                Level = row.Level,
                HelperKind = helperKind,
                StatusLabel = statusLabel,
                StatusTone = statusTone,
                IsRecurring = isRecurring,
                RecurringText = recurringText,
                UpgradeSecondsAtExport = upgradeSeconds,
                UpgradeFinishAtUtc = upgradeFinishAtUtc,
                CooldownSecondsAtExport = cooldownSeconds,
                NextResetAtUtc = cooldownFinishAtUtc,
                AvailableAtUtc = trueAvailableAtUtc,
                BoostMultiplier = boostMultiplier,
                BoostTimeSeconds = boostTimeSeconds,
                SavedSecondsPerCycle = savedSecondsPerCycle,
                EstimatedFutureSavedSeconds = estimatedFutureSavedSeconds,
                EstimatedTargetFinishAtUtc = estimatedTargetFinishAtUtc,
                AssignmentLabel = assignmentLabel,
                TargetItemType = representativeTarget?.Row.ItemType,
                TargetItemDataId = representativeTarget?.Row.ItemDataId,
                TargetItemName = targetItemName,
                TargetRemainingSecondsAtExport = representativeTarget?.Row.UpgradeTimerSeconds,
                TargetIsInferred = representativeTarget is not null,
                HasMultiplePossibleTargets = hasMultiplePossibleTargets
            });
        }

        return result;
    }

    private async Task<List<VillageActiveTimerDto>> BuildActiveTimersAsync(
        Village village,
        CancellationToken cancellationToken)
    {
        if (!village.LastGameTimestamp.HasValue)
            return new List<VillageActiveTimerDto>();

        var exportedAtUtc = DateTimeOffset.FromUnixTimeSeconds(village.LastGameTimestamp.Value).UtcDateTime;

        var timedRows = await _db.VillageItemLevels
            .AsNoTracking()
            .Where(x =>
                x.VillageId == village.Id &&
                !HiddenVillageSummaryTypes.Contains(x.ItemType) &&
                x.ItemType != ItemType.Helper &&
                x.UpgradeTimerSeconds.HasValue &&
                x.UpgradeTimerSeconds.Value > 0)
            .ToListAsync(cancellationToken);

        if (timedRows.Count == 0)
            return new List<VillageActiveTimerDto>();

        var itemDataIds = timedRows
            .Select(x => x.ItemDataId)
            .Distinct()
            .ToList();

        var staticItems = await _db.StaticItems
            .AsNoTracking()
            .Include(x => x.Levels)
            .Where(x => itemDataIds.Contains(x.ItemDataId))
            .ToListAsync(cancellationToken);

        var staticMap = staticItems
            .GroupBy(x => (x.Section, x.ItemType, x.ItemDataId))
            .ToDictionary(x => x.Key, x => x.First());

        var nowUtc = DateTime.UtcNow;
        var result = new List<VillageActiveTimerDto>();

        foreach (var row in timedRows)
        {
            staticMap.TryGetValue((row.Section, row.ItemType, row.ItemDataId), out var staticItem);
            var itemName = staticItem?.Name ?? $"{row.ItemType} #{row.ItemDataId}";

            result.Add(BuildTimerDto(
                row,
                itemName,
                exportedAtUtc,
                nowUtc,
                row.UpgradeTimerSeconds!.Value,
                timerKind: "Upgrade",
                statusLabel: "Upgrading",
                fromLevel: row.Level,
                toLevel: row.Level + 1));
        }

        return result
            .OrderBy(x => GetProgressGroupOrder(x.ProgressGroup))
            .ThenBy(x => x.IsFinishedByNow)
            .ThenBy(x => x.FinishAtUtc)
            .ThenBy(x => x.ItemName)
            .ToList();
    }

    private static VillageActiveTimerDto BuildTimerDto(
        VillageItemLevel row,
        string itemName,
        DateTime exportedAtUtc,
        DateTime nowUtc,
        int timerSeconds,
        string timerKind,
        string statusLabel,
        int? fromLevel,
        int? toLevel)
    {
        var finishAtUtc = exportedAtUtc.AddSeconds(timerSeconds);
        var finishedByNow = finishAtUtc <= nowUtc;

        return new VillageActiveTimerDto
        {
            Section = row.Section,
            ItemType = row.ItemType,
            ItemDataId = row.ItemDataId,
            ItemName = itemName,
            Level = row.Level,
            FromLevel = fromLevel,
            ToLevel = toLevel,
            Count = row.Count,
            TimerKind = timerKind,
            StatusLabel = statusLabel,
            ProgressGroup = ResolveProgressGroup(row.ItemType),
            RemainingSecondsAtExport = timerSeconds,
            ExportedAtUtc = exportedAtUtc,
            FinishAtUtc = finishAtUtc,
            IsFinishedByNow = finishedByNow,
            IsStaleExport = finishedByNow,
            IsHelperAssisted = row.IsHelperRecurrent
        };
    }

    private static string ResolveProgressGroup(ItemType itemType)
    {
        if (itemType == ItemType.Pet)
            return "Pet House Upgrades";

        if (itemType == ItemType.Troop || itemType == ItemType.Spell || itemType == ItemType.SiegeMachine)
            return "Laboratory Upgrades";

        return "Builder Upgrades";
    }

    private static int GetProgressGroupOrder(string progressGroup) => progressGroup switch
    {
        "Builder Upgrades" => 1,
        "Laboratory Upgrades" => 2,
        "Pet House Upgrades" => 3,
        _ => 99
    };

    private async Task<List<VillageItemStateDto>> BuildItemStatesAsync(
        Village village,
        CancellationToken cancellationToken)
    {
        var villageItems = await _db.VillageItemLevels
            .AsNoTracking()
            .Where(x => x.VillageId == village.Id && !HiddenVillageSummaryTypes.Contains(x.ItemType))
            .ToListAsync(cancellationToken);

        if (villageItems.Count == 0)
            return new List<VillageItemStateDto>();

        var itemDataIds = villageItems
            .Select(x => x.ItemDataId)
            .Distinct()
            .ToList();

        var staticItems = await _db.StaticItems
            .AsNoTracking()
            .Where(x => itemDataIds.Contains(x.ItemDataId))
            .Include(x => x.Levels)
                .ThenInclude(x => x.Requirements)
            .ToListAsync(cancellationToken);

        var staticMap = staticItems
            .GroupBy(x => (x.Section, x.ItemType, x.ItemDataId))
            .ToDictionary(x => x.Key, x => x.First());

        var hallCaps = await _db.StaticHallItemCaps
            .AsNoTracking()
            .Where(x => itemDataIds.Contains(x.ItemDataId))
            .ToListAsync(cancellationToken);

        var hallCapMap = hallCaps
            .GroupBy(x => (x.Section, x.ItemType, x.ItemDataId))
            .ToDictionary(x => x.Key, x => x.ToList());

        var villagePriorityMap = await _db.VillagePriorityItems
            .AsNoTracking()
            .Where(x => x.VillageId == village.Id)
            .ToDictionaryAsync(
                x => (x.Section, x.ItemType, x.ItemDataId),
                x => (int?)x.PriorityRank,
                cancellationToken);

        Dictionary<(VillageSection Section, ItemType ItemType, int ItemDataId), int?> clanPriorityMap;

        if (village.ClanId.HasValue)
        {
            var clanPriorityRows = await _db.ClanPriorityItems
                .AsNoTracking()
                .Where(x => x.ClanId == village.ClanId.Value)
                .ToListAsync(cancellationToken);

            clanPriorityMap = clanPriorityRows
                .Where(x =>
                    (x.Section == VillageSection.HomeVillage && village.TownHallLevel.HasValue && x.TownHallLevel == village.TownHallLevel.Value) ||
                    (x.Section == VillageSection.BuilderBase && village.BuilderHallLevel.HasValue && x.BuilderHallLevel == village.BuilderHallLevel.Value))
                .GroupBy(x => (x.Section, x.ItemType, x.ItemDataId))
                .ToDictionary(x => x.Key, x => (int?)x.First().PriorityRank);
        }
        else
        {
            clanPriorityMap = new Dictionary<(VillageSection Section, ItemType ItemType, int ItemDataId), int?>();
        }

        var pendingSuggestionKeys = await _db.PrioritySuggestions
            .AsNoTracking()
            .Where(x => x.VillageId == village.Id && x.Status == SuggestionStatus.Pending)
            .Select(x => new { x.Section, x.ItemType, x.ItemDataId })
            .ToListAsync(cancellationToken);

        var pendingSet = pendingSuggestionKeys
            .Select(x => (x.Section, x.ItemType, x.ItemDataId))
            .ToHashSet();

        var result = new List<VillageItemStateDto>();

        foreach (var group in villageItems
                     .GroupBy(x => (x.Section, x.ItemType, x.ItemDataId))
                     .OrderBy(x => x.Key.Section)
                     .ThenBy(x => x.Key.ItemType)
                     .ThenBy(x => x.Key.ItemDataId))
        {
            staticMap.TryGetValue(group.Key, out var staticItem);

            int? hallLevel = group.Key.Section == VillageSection.HomeVillage
                ? village.TownHallLevel
                : village.BuilderHallLevel;

            int? maxLevelAtCurrentHall = GetMaxLevelAtCurrentHall(staticItem, group.Key.Section, hallLevel);
            int? globalMaxLevel = staticItem?.Levels.Count > 0
                ? staticItem.Levels.Max(x => x.Level)
                : null;

            int? maxCountAtCurrentHall = GetMaxCountAtCurrentHall(hallCapMap, group.Key, hallLevel);
            int? globalMaxCount = GetGlobalMaxCount(hallCapMap, group.Key);

            villagePriorityMap.TryGetValue(group.Key, out var villagePriority);
            clanPriorityMap.TryGetValue(group.Key, out var clanPriority);

            result.Add(new VillageItemStateDto
            {
                Section = group.Key.Section,
                ItemType = group.Key.ItemType,
                ItemDataId = group.Key.ItemDataId,
                ItemName = staticItem?.Name ?? $"{group.Key.ItemType} #{group.Key.ItemDataId}",
                CurrentTotalCount = group.Sum(x => x.Count),
                CurrentMinLevel = group.Min(x => x.Level),
                CurrentMaxLevel = group.Max(x => x.Level),
                CurrentLevelText = BuildLevelText(group),
                MaxLevelAtCurrentHall = maxLevelAtCurrentHall,
                GlobalMaxLevel = globalMaxLevel,
                MaxCountAtCurrentHall = maxCountAtCurrentHall,
                GlobalMaxCount = globalMaxCount,
                VillagePriorityRank = villagePriority,
                ClanPriorityRank = clanPriority,
                EffectivePriorityRank = villagePriority ?? clanPriority,
                EffectivePrioritySource = villagePriority.HasValue
                    ? "Village"
                    : clanPriority.HasValue
                        ? "Clan"
                        : null,
                HasPendingSuggestion = pendingSet.Contains(group.Key),
                LevelBuckets = group
                    .GroupBy(x => x.Level)
                    .OrderByDescending(x => x.Key)
                    .Select(x => new VillageItemLevelDto
                    {
                        Section = group.Key.Section,
                        ItemType = group.Key.ItemType,
                        ItemDataId = group.Key.ItemDataId,
                        Level = x.Key,
                        Count = x.Sum(y => y.Count),
                        UpgradeTimerSeconds = null
                    })
                    .ToArray()
            });
        }

        return result;
    }

    private static bool IsBuilderHelperTarget(VillageItemLevel row) =>
        row.Section == VillageSection.HomeVillage &&
        (row.ItemType == ItemType.Building ||
         row.ItemType == ItemType.Trap ||
         row.ItemType == ItemType.Hero);

    private static bool IsLabHelperTarget(VillageItemLevel row) =>
        row.Section == VillageSection.HomeVillage &&
        (row.ItemType == ItemType.Troop ||
         row.ItemType == ItemType.Spell ||
         row.ItemType == ItemType.SiegeMachine);

    private static DateTime? EstimateRecurringFinishAtUtc(
        DateTime exportedAtUtc,
        int exportedRemainingSeconds,
        DateTime nextResetAtUtc,
        int workSeconds,
        int savedSecondsPerCycle,
        int cycleSeconds)
    {
        if (exportedRemainingSeconds <= 0)
            return exportedAtUtc;

        if (workSeconds <= 0 || savedSecondsPerCycle <= 0 || cycleSeconds <= 0)
            return exportedAtUtc.AddSeconds(exportedRemainingSeconds);

        var current = exportedAtUtc;
        double remaining = exportedRemainingSeconds;

        if (nextResetAtUtc > current)
        {
            var untilReset = (nextResetAtUtc - current).TotalSeconds;
            if (remaining <= untilReset)
                return current.AddSeconds(remaining);

            remaining -= untilReset;
            current = nextResetAtUtc;
        }

        var cooldownSeconds = Math.Max(0, cycleSeconds - workSeconds);
        var effectiveRateDuringWork = 1d + ((double)savedSecondsPerCycle / workSeconds);
        var fullWorkProgress = workSeconds + savedSecondsPerCycle;

        for (var cycle = 0; cycle < 365 && remaining > 0; cycle++)
        {
            if (remaining <= fullWorkProgress)
            {
                var secondsIntoWork = remaining / effectiveRateDuringWork;
                return current.AddSeconds(secondsIntoWork);
            }

            remaining -= fullWorkProgress;
            current = current.AddSeconds(workSeconds);

            if (remaining <= cooldownSeconds)
                return current.AddSeconds(remaining);

            remaining -= cooldownSeconds;
            current = current.AddSeconds(cooldownSeconds);
        }

        return current;
    }

    private static string FormatCompactDuration(int totalSeconds)
    {
        totalSeconds = Math.Max(0, totalSeconds);
        var ts = TimeSpan.FromSeconds(totalSeconds);

        if (ts.TotalDays >= 1)
            return $"{(int)ts.TotalDays}d {ts.Hours}h";

        if (ts.TotalHours >= 1)
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";

        if (ts.TotalMinutes >= 1)
            return $"{(int)ts.TotalMinutes}m {ts.Seconds}s";

        return $"{ts.Seconds}s";
    }

    private static string ResolveHelperKind(int itemDataId) => itemDataId switch
    {
        93000000 => "Builder",
        93000001 => "Research",
        93000002 => "Alchemist",
        93000003 => "Prospector",
        _ => "Helper"
    };

    private static string ResolveHelperDisplayName(int itemDataId) => itemDataId switch
    {
        93000000 => "Builder's Apprentice",
        93000001 => "Lab Assistant",
        93000002 => "Alchemist",
        93000003 => "Prospector",
        _ => $"Helper #{itemDataId}"
    };

    private static string? BuildGenericAssignmentLabel(string helperKind) => helperKind switch
    {
        "Builder" => "Active builder upgrade",
        "Research" => "Active laboratory upgrade",
        _ => null
    };

    private static string BuildFallbackTargetLabel(VillageItemLevel target) => target.ItemType switch
    {
        ItemType.Building => $"Building #{target.ItemDataId}",
        ItemType.Trap => $"Trap #{target.ItemDataId}",
        ItemType.Hero => $"Hero #{target.ItemDataId}",
        ItemType.Troop => $"Troop #{target.ItemDataId}",
        ItemType.Spell => $"Spell #{target.ItemDataId}",
        ItemType.SiegeMachine => $"Siege Machine #{target.ItemDataId}",
        ItemType.Pet => $"Pet #{target.ItemDataId}",
        _ => $"{target.ItemType} #{target.ItemDataId}"
    };

    private static string BuildLevelText(IEnumerable<VillageItemLevel> group)
    {
        var parts = group
            .GroupBy(x => x.Level)
            .OrderByDescending(x => x.Key)
            .Select(x => x.Sum(y => y.Count) > 1 ? $"{x.Key} x{x.Sum(y => y.Count)}" : x.Key.ToString());

        return string.Join(", ", parts);
    }

    private static int? GetMaxLevelAtCurrentHall(
        StaticItem? staticItem,
        VillageSection section,
        int? hallLevel)
    {
        if (staticItem is null || !hallLevel.HasValue)
            return null;

        var hallRequirement = section == VillageSection.HomeVillage
            ? RequirementType.TownHall
            : RequirementType.BuilderHall;

        var allowed = staticItem.Levels
            .Where(level =>
            {
                var hallRequirements = level.Requirements
                    .Where(r => r.RequirementType == hallRequirement)
                    .ToList();

                if (hallRequirements.Count == 0)
                    return true;

                return hallRequirements.Max(r => r.RequiredLevel) <= hallLevel.Value;
            })
            .Select(x => (int?)x.Level)
            .ToList();

        return allowed.Count == 0 ? null : allowed.Max();
    }

    private static int? GetMaxCountAtCurrentHall(
        IReadOnlyDictionary<(VillageSection Section, ItemType ItemType, int ItemDataId), List<StaticHallItemCap>> hallCapMap,
        (VillageSection Section, ItemType ItemType, int ItemDataId) key,
        int? hallLevel)
    {
        if (!hallLevel.HasValue)
            return null;

        if (!hallCapMap.TryGetValue(key, out var caps) || caps.Count == 0)
            return null;

        return caps
            .Where(x => x.HallLevel <= hallLevel.Value)
            .OrderByDescending(x => x.HallLevel)
            .Select(x => (int?)x.MaxCount)
            .FirstOrDefault();
    }

    private static int? GetGlobalMaxCount(
        IReadOnlyDictionary<(VillageSection Section, ItemType ItemType, int ItemDataId), List<StaticHallItemCap>> hallCapMap,
        (VillageSection Section, ItemType ItemType, int ItemDataId) key)
    {
        if (!hallCapMap.TryGetValue(key, out var caps) || caps.Count == 0)
            return null;

        return caps.Max(x => x.MaxCount);
    }

    private static readonly ItemType[] HiddenVillageSummaryTypes =
    [
        ItemType.HousePart,
        ItemType.Obstacle,
        ItemType.Skin,
        ItemType.Decoration,
        ItemType.Scenery,
    ];
}