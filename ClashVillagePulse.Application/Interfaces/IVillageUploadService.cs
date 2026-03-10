using ClashVillagePulse.Application.DTOs;

namespace ClashVillagePulse.Application.Interfaces;

public interface IVillageUploadService
{
    Task<VillageUploadResultDto> UploadAsync(
        string ownerUserId,
        string json,
        CancellationToken cancellationToken = default);
}