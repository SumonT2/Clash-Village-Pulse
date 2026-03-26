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

        var cleanedMessage = string.IsNullOrWhiteSpace(request.Message)
            ? null
            : request.Message.Trim();

        if (string.IsNullOrWhiteSpace(cleanedMessage))
            throw new InvalidOperationException("Please add a short message for the suggestion.");

        var existingPending = await _db.PrioritySuggestions
            .FirstOrDefaultAsync(x =>
                x.VillageId == request.VillageId &&
                x.SuggestedByUserId == userId &&
                x.Section == request.Section &&
                x.ItemType == request.ItemType &&
                x.ItemDataId == request.ItemDataId &&
                x.Status == SuggestionStatus.Pending,
                cancellationToken);

        var suggestedRank = request.SuggestedPriorityRank > 0
            ? request.SuggestedPriorityRank
            : existingPending?.SuggestedPriorityRank > 0
                ? existingPending.SuggestedPriorityRank
                : await GetNextSuggestedRankAsync(request.VillageId, cancellationToken);

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
                SuggestedPriorityRank = suggestedRank,
                Message = cleanedMessage,
                Status = SuggestionStatus.Pending,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            existingPending.SuggestedPriorityRank = suggestedRank;
            existingPending.Message = cleanedMessage;
            existingPending.CreatedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(cancellationToken);
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

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> GetNextSuggestedRankAsync(
        Guid villageId,
        CancellationToken cancellationToken)
    {
        var currentVillageMax = await _db.VillagePriorityItems
            .Where(x => x.VillageId == villageId)
            .Select(x => (int?)x.PriorityRank)
            .MaxAsync(cancellationToken) ?? 0;

        var currentPendingMax = await _db.PrioritySuggestions
            .Where(x => x.VillageId == villageId && x.Status == SuggestionStatus.Pending)
            .Select(x => (int?)x.SuggestedPriorityRank)
            .MaxAsync(cancellationToken) ?? 0;

        return Math.Max(currentVillageMax, currentPendingMax) + 1;
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
}
