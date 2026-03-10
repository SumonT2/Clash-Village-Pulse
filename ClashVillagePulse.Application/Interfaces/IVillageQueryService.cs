using ClashVillagePulse.Application.DTOs;

namespace ClashVillagePulse.Application.Interfaces;

public interface IVillageQueryService
{
    Task<IReadOnlyList<VillageListItemDto>> GetMyVillagesAsync(
        string ownerUserId,
        CancellationToken cancellationToken = default);

    Task<VillageDetailsDto?> GetVillageDetailsAsync(
        string ownerUserId,
        Guid villageId,
        CancellationToken cancellationToken = default);
}