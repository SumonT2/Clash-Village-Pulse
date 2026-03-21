using ClashVillagePulse.Application.DTOs;

namespace ClashVillagePulse.Application.Interfaces;

public interface IClashProfileSyncService
{
    Task<PlayerProfileDto> SyncVillageProfileAsync(
        Guid villageId,
        string currentUserId,
        CancellationToken cancellationToken = default);
}