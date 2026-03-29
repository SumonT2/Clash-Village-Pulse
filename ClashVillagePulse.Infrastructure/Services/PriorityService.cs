using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.Services;

public sealed class PriorityService : IPriorityService
{
    private readonly AppDbContext _db;

    public PriorityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SaveVillagePrioritiesAsync(
        string ownerUserId,
        Guid villageId,
        IReadOnlyList<SavePriorityItemDto> items,
        CancellationToken cancellationToken = default)
    {
        var village = await _db.Villages
            .FirstOrDefaultAsync(x => x.Id == villageId && x.OwnerUserId == ownerUserId && !x.IsArchived, cancellationToken);

        if (village is null)
            throw new InvalidOperationException("Village not found or access denied.");

        var normalized = NormalizePriorityInputs(items);

        var existing = await _db.VillagePriorityItems
            .Where(x => x.VillageId == villageId)
            .ToListAsync(cancellationToken);

        foreach (var item in normalized)
        {
            var row = existing.FirstOrDefault(x =>
                x.Section == item.Section &&
                x.ItemType == item.ItemType &&
                x.ItemDataId == item.ItemDataId);

            if (!item.PriorityRank.HasValue)
            {
                if (row is not null)
                    _db.VillagePriorityItems.Remove(row);

                continue;
            }

            if (row is null)
            {
                _db.VillagePriorityItems.Add(new VillagePriorityItem
                {
                    Id = Guid.NewGuid(),
                    VillageId = villageId,
                    Section = item.Section,
                    ItemType = item.ItemType,
                    ItemDataId = item.ItemDataId,
                    PriorityRank = item.PriorityRank.Value,
                    Note = item.Note,
                    CreatedByUserId = ownerUserId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                row.PriorityRank = item.PriorityRank.Value;
                row.Note = item.Note;
                row.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveClanPrioritiesAsync(
        string userId,
        Guid villageId,
        VillageSection section,
        int hallLevel,
        IReadOnlyList<SavePriorityItemDto> items,
        CancellationToken cancellationToken = default)
    {
        var village = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == villageId && !x.IsArchived, cancellationToken);

        if (village is null || !village.ClanId.HasValue)
            throw new InvalidOperationException("Village has no clan.");

        bool isClanMember = await _db.ClanMembers
            .AnyAsync(x => x.ClanId == village.ClanId.Value && x.UserId == userId, cancellationToken);

        if (!isClanMember)
            throw new InvalidOperationException("Only clan members can edit clan priority templates.");

        var normalized = NormalizePriorityInputs(items)
            .Where(x => x.Section == section)
            .ToList();

        var existing = await _db.ClanPriorityItems
            .Where(x =>
                x.ClanId == village.ClanId.Value &&
                x.Section == section &&
                ((section == VillageSection.HomeVillage && x.TownHallLevel == hallLevel) ||
                 (section == VillageSection.BuilderBase && x.BuilderHallLevel == hallLevel)))
            .ToListAsync(cancellationToken);

        foreach (var item in normalized)
        {
            var row = existing.FirstOrDefault(x =>
                x.ItemType == item.ItemType &&
                x.ItemDataId == item.ItemDataId);

            if (!item.PriorityRank.HasValue)
            {
                if (row is not null)
                    _db.ClanPriorityItems.Remove(row);

                continue;
            }

            if (row is null)
            {
                _db.ClanPriorityItems.Add(new ClanPriorityItem
                {
                    Id = Guid.NewGuid(),
                    ClanId = village.ClanId.Value,
                    Section = item.Section,
                    TownHallLevel = section == VillageSection.HomeVillage ? hallLevel : null,
                    BuilderHallLevel = section == VillageSection.BuilderBase ? hallLevel : null,
                    ItemType = item.ItemType,
                    ItemDataId = item.ItemDataId,
                    PriorityRank = item.PriorityRank.Value,
                    CreatedByUserId = userId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                row.PriorityRank = item.PriorityRank.Value;
                row.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SubmitSuggestionAsync(
        string userId,
        SubmitPrioritySuggestionDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.SuggestedPriorityRank <= 0)
            throw new InvalidOperationException("Suggested rank must be greater than zero.");

        var village = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.VillageId && !x.IsArchived, cancellationToken);

        if (village is null)
            throw new InvalidOperationException("Village not found.");

        if (village.OwnerUserId == userId)
            throw new InvalidOperationException("Use village priority editing for your own village.");

        if (!village.ClanId.HasValue)
            throw new InvalidOperationException("Target village is not linked to a clan.");

        bool isClanMember = await _db.ClanMembers
            .AnyAsync(x => x.ClanId == village.ClanId.Value && x.UserId == userId, cancellationToken);

        if (!isClanMember)
            throw new InvalidOperationException("Only members of the same clan can suggest priorities.");

        bool itemExists = await _db.VillageItemLevels
            .AnyAsync(x =>
                x.VillageId == request.VillageId &&
                x.Section == request.Section &&
                x.ItemType == request.ItemType &&
                x.ItemDataId == request.ItemDataId,
                cancellationToken);

        if (!itemExists)
            throw new InvalidOperationException("Target item was not found in the village.");

        var existingPending = await _db.PrioritySuggestions
            .FirstOrDefaultAsync(x =>
                x.VillageId == request.VillageId &&
                x.SuggestedByUserId == userId &&
                x.Section == request.Section &&
                x.ItemType == request.ItemType &&
                x.ItemDataId == request.ItemDataId &&
                x.Status == SuggestionStatus.Pending,
                cancellationToken);

        if (existingPending is null)
        {
            _db.PrioritySuggestions.Add(new PrioritySuggestion
            {
                Id = Guid.NewGuid(),
                VillageId = request.VillageId,
                SuggestedByUserId = userId,
                Section = request.Section,
                ItemType = request.ItemType,
                ItemDataId = request.ItemDataId,
                SuggestedPriorityRank = request.SuggestedPriorityRank,
                Message = request.Message,
                Status = SuggestionStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existingPending.SuggestedPriorityRank = request.SuggestedPriorityRank;
            existingPending.Message = request.Message;
            existingPending.CreatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<PrioritySuggestionImportResultDto> SuggestVillagePriorityToVillageAsync(
        string userId,
        Guid sourceVillageId,
        Guid targetVillageId,
        string? message,
        CancellationToken cancellationToken = default)
    {
        var sourceVillage = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sourceVillageId && x.OwnerUserId == userId && !x.IsArchived, cancellationToken);

        if (sourceVillage is null)
            throw new InvalidOperationException("Source village not found or access denied.");

        var targetVillage = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == targetVillageId && !x.IsArchived, cancellationToken);

        if (targetVillage is null)
            throw new InvalidOperationException("Target village not found.");

        if (targetVillage.Id == sourceVillage.Id)
            throw new InvalidOperationException("Choose a different target village.");

        var isOwnOtherVillage = targetVillage.OwnerUserId == userId;
        var isSameClanTarget = sourceVillage.ClanId.HasValue
            && targetVillage.ClanId.HasValue
            && targetVillage.ClanId.Value == sourceVillage.ClanId.Value;

        if (!isOwnOtherVillage && !isSameClanTarget)
            throw new InvalidOperationException("Target village must be one of your other villages or belong to the same clan as the source village.");

        var sourcePriorities = await _db.VillagePriorityItems
            .AsNoTracking()
            .Where(x => x.VillageId == sourceVillageId)
            .OrderBy(x => x.PriorityRank)
            .ThenBy(x => x.ItemType)
            .ThenBy(x => x.ItemDataId)
            .ToListAsync(cancellationToken);

        if (sourcePriorities.Count == 0)
            throw new InvalidOperationException("The source village has no saved priority items yet.");

        var targetItems = await _db.VillageItemLevels
            .AsNoTracking()
            .Where(x => x.VillageId == targetVillageId)
            .Select(x => new { x.Section, x.ItemType, x.ItemDataId })
            .Distinct()
            .ToListAsync(cancellationToken);

        var targetItemKeys = targetItems
            .Select(x => new PriorityKey(x.Section, x.ItemType, x.ItemDataId))
            .ToHashSet();

        var existingPriorityKeys = await _db.VillagePriorityItems
            .AsNoTracking()
            .Where(x => x.VillageId == targetVillageId)
            .Select(x => new { x.Section, x.ItemType, x.ItemDataId })
            .ToListAsync(cancellationToken);

        var existingPendingKeys = await _db.PrioritySuggestions
            .AsNoTracking()
            .Where(x => x.VillageId == targetVillageId && x.Status == SuggestionStatus.Pending)
            .Select(x => new { x.Section, x.ItemType, x.ItemDataId })
            .ToListAsync(cancellationToken);

        var prioritizedKeySet = existingPriorityKeys
            .Select(x => new PriorityKey(x.Section, x.ItemType, x.ItemDataId))
            .ToHashSet();

        var pendingKeySet = existingPendingKeys
            .Select(x => new PriorityKey(x.Section, x.ItemType, x.ItemDataId))
            .ToHashSet();

        var result = new PrioritySuggestionImportResultDto
        {
            TargetVillageName = targetVillage.Name,
            SourcePriorityCount = sourcePriorities.Count
        };

        var createdAtUtc = DateTime.UtcNow;
        var customMessage = string.IsNullOrWhiteSpace(message) ? null : message.Trim();

        foreach (var sourceItem in sourcePriorities)
        {
            var key = new PriorityKey(sourceItem.Section, sourceItem.ItemType, sourceItem.ItemDataId);

            if (!targetItemKeys.Contains(key))
            {
                result.MissingInTargetCount++;
                continue;
            }

            if (prioritizedKeySet.Contains(key))
            {
                result.AlreadyPrioritizedCount++;
                continue;
            }

            if (pendingKeySet.Contains(key))
            {
                result.AlreadyPendingCount++;
                continue;
            }

            _db.PrioritySuggestions.Add(new PrioritySuggestion
            {
                Id = Guid.NewGuid(),
                VillageId = targetVillageId,
                SuggestedByUserId = userId,
                Section = sourceItem.Section,
                ItemType = sourceItem.ItemType,
                ItemDataId = sourceItem.ItemDataId,
                SuggestedPriorityRank = sourceItem.PriorityRank,
                Message = BuildTransferMessage(sourceVillage.Name, sourceItem.PriorityRank, customMessage),
                Status = SuggestionStatus.Pending,
                CreatedAtUtc = createdAtUtc
            });

            pendingKeySet.Add(key);
            result.AddedCount++;
        }

        if (result.AddedCount > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task RespondToSuggestionAsync(
        string ownerUserId,
        Guid suggestionId,
        bool accept,
        CancellationToken cancellationToken = default)
    {
        var suggestion = await _db.PrioritySuggestions
            .Include(x => x.Village)
            .FirstOrDefaultAsync(x => x.Id == suggestionId, cancellationToken);

        if (suggestion is null)
            throw new InvalidOperationException("Suggestion not found.");

        if (suggestion.Village.OwnerUserId != ownerUserId)
            throw new InvalidOperationException("Only the village owner can respond to a suggestion.");

        if (suggestion.Status != SuggestionStatus.Pending)
            throw new InvalidOperationException("This suggestion has already been processed.");

        suggestion.Status = accept ? SuggestionStatus.Accepted : SuggestionStatus.Rejected;
        suggestion.DecidedByUserId = ownerUserId;
        suggestion.DecidedAtUtc = DateTime.UtcNow;

        if (accept)
        {
            await ApplySuggestionToVillagePriorityAsync(ownerUserId, suggestion, cancellationToken);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RespondToAllSuggestionsAsync(
        string ownerUserId,
        Guid villageId,
        bool accept,
        CancellationToken cancellationToken = default)
    {
        var village = await _db.Villages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == villageId && !x.IsArchived, cancellationToken);

        if (village is null)
            throw new InvalidOperationException("Village not found.");

        if (village.OwnerUserId != ownerUserId)
            throw new InvalidOperationException("Only the village owner can respond to suggestions.");

        var pendingSuggestions = await _db.PrioritySuggestions
            .Where(x => x.VillageId == villageId && x.Status == SuggestionStatus.Pending)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.SuggestedPriorityRank)
            .ToListAsync(cancellationToken);

        if (pendingSuggestions.Count == 0)
            return 0;

        var decidedAtUtc = DateTime.UtcNow;

        foreach (var suggestion in pendingSuggestions)
        {
            suggestion.Status = accept ? SuggestionStatus.Accepted : SuggestionStatus.Rejected;
            suggestion.DecidedByUserId = ownerUserId;
            suggestion.DecidedAtUtc = decidedAtUtc;
        }

        if (accept)
        {
            var winningSuggestions = pendingSuggestions
                .GroupBy(x => new PriorityKey(x.Section, x.ItemType, x.ItemDataId))
                .Select(g => g
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .ThenByDescending(x => x.SuggestedPriorityRank)
                    .First())
                .ToList();

            foreach (var suggestion in winningSuggestions)
            {
                await ApplySuggestionToVillagePriorityAsync(ownerUserId, suggestion, cancellationToken);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return pendingSuggestions.Count;
    }

    private async Task ApplySuggestionToVillagePriorityAsync(
        string ownerUserId,
        PrioritySuggestion suggestion,
        CancellationToken cancellationToken)
    {
        var existing = await _db.VillagePriorityItems
            .FirstOrDefaultAsync(x =>
                x.VillageId == suggestion.VillageId &&
                x.Section == suggestion.Section &&
                x.ItemType == suggestion.ItemType &&
                x.ItemDataId == suggestion.ItemDataId,
                cancellationToken);

        if (existing is null)
        {
            _db.VillagePriorityItems.Add(new VillagePriorityItem
            {
                Id = Guid.NewGuid(),
                VillageId = suggestion.VillageId,
                Section = suggestion.Section,
                ItemType = suggestion.ItemType,
                ItemDataId = suggestion.ItemDataId,
                PriorityRank = suggestion.SuggestedPriorityRank,
                Note = suggestion.Message,
                CreatedByUserId = ownerUserId,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existing.PriorityRank = suggestion.SuggestedPriorityRank;
            existing.Note = suggestion.Message;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private static List<SavePriorityItemDto> NormalizePriorityInputs(IReadOnlyList<SavePriorityItemDto> items)
    {
        return items
            .GroupBy(x => new { x.Section, x.ItemType, x.ItemDataId })
            .Select(x => x.Last())
            .Select(x => new SavePriorityItemDto
            {
                Section = x.Section,
                ItemType = x.ItemType,
                ItemDataId = x.ItemDataId,
                PriorityRank = x.PriorityRank.HasValue && x.PriorityRank.Value > 0
                    ? x.PriorityRank.Value
                    : null,
                Note = x.Note
            })
            .ToList();
    }

    private static string BuildTransferMessage(string sourceVillageName, int sourceRank, string? customMessage)
    {
        var baseMessage = $"Suggested from {sourceVillageName} priority #{sourceRank}.";
        return string.IsNullOrWhiteSpace(customMessage)
            ? baseMessage
            : $"{baseMessage} {customMessage}";
    }

    private readonly record struct PriorityKey(
        VillageSection Section,
        ItemType ItemType,
        int ItemDataId);
}
