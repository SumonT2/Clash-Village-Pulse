using ClashVillagePulse.Application.DTOs;

namespace ClashVillagePulse.Application.Interfaces;

public interface IStaticDataGenerationService
{
    Task<Guid> StartGenerationAsync(
        string userId,
        StaticDataGenerationRequest request,
        CancellationToken cancellationToken = default);

    Task<StaticDataRunDto?> GetRunAsync(
        Guid runId,
        CancellationToken cancellationToken = default);
}