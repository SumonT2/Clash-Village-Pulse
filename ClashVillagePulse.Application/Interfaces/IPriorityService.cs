using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.Interfaces;

public interface IPriorityService
{
    Task SaveVillagePrioritiesAsync(
        string ownerUserId,
        Guid villageId,
        IReadOnlyList<SavePriorityItemDto> items,
        CancellationToken cancellationToken = default);

    Task SaveClanPrioritiesAsync(
        string userId,
        Guid villageId,
        VillageSection section,
        int hallLevel,
        IReadOnlyList<SavePriorityItemDto> items,
        CancellationToken cancellationToken = default);

    Task SubmitSuggestionAsync(
        string userId,
        SubmitPrioritySuggestionDto request,
        CancellationToken cancellationToken = default);

    Task RespondToSuggestionAsync(
        string ownerUserId,
        Guid suggestionId,
        bool accept,
        CancellationToken cancellationToken = default);
}