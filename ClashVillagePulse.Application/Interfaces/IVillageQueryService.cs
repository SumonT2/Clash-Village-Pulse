using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Domain.Enums;

namespace ClashVillagePulse.Application.Interfaces;

public interface IVillageQueryService
{
    Task<IReadOnlyList<VillageListItemDto>> GetMyVillagesAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VillageListItemDto>> GetClanVillagesAsync(
        string viewerUserId,
        CancellationToken cancellationToken = default);

    Task<VillageDetailsDto?> GetVillageDetailsAsync(
        string viewerUserId,
        Guid villageId,
        CancellationToken cancellationToken = default);

    Task<VillagePriorityEditDto?> GetVillagePriorityEditAsync(
        string ownerUserId,
        Guid villageId,
        CancellationToken cancellationToken = default);

    Task<ClanPriorityTemplateDto?> GetClanPriorityTemplateAsync(
        string userId,
        Guid villageId,
        VillageSection section,
        CancellationToken cancellationToken = default);
}