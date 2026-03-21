using ClashVillagePulse.Application.DTOs;

namespace ClashVillagePulse.Application.Interfaces;

public interface IClashApiService
{
    Task<PlayerProfileDto> GetPlayerProfileAsync(
        string playerTag,
        CancellationToken cancellationToken = default);
}