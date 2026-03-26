using ClashVillagePulse.Application.DTOs;
using ClashVillagePulse.Application.Interfaces;
using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData;

public class StaticDataGenerationService : IStaticDataGenerationService
{
    private readonly AppDbContext _db;
    private readonly StaticDataDownloader _downloader;
    private readonly StaticDataDecompressor _decompressor;
    private readonly StaticDataRunTracker _runTracker;
    private readonly StaticDataProcessorRegistry _processorRegistry;

    public StaticDataGenerationService(
        AppDbContext db,
        StaticDataDownloader downloader,
        StaticDataDecompressor decompressor,
        StaticDataRunTracker runTracker,
        StaticDataProcessorRegistry processorRegistry)
    {
        _db = db;
        _downloader = downloader;
        _decompressor = decompressor;
        _runTracker = runTracker;
        _processorRegistry = processorRegistry;
    }

    public async Task<Guid> StartGenerationAsync(
        string userId,
        StaticDataGenerationRequest request,
        CancellationToken cancellationToken = default)
    {
        var run = new StaticDataRun
        {
            Id = Guid.NewGuid(),
            Fingerprint = request.Fingerprint,
            RequestedByUserId = userId,
            RequestedAtUtc = DateTime.UtcNow,
            StartedAtUtc = DateTime.UtcNow,
            Status = StaticDataRunStatus.Running
        };

        _db.StaticDataRuns.Add(run);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            var targetKeys = request.Targets.Any()
    ? request.Targets.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    : new List<string>
    {
        "buildings",
        "characters",
        "heroes",
        "spells",
        "pets",
        "equipment",
        "texts",
        "townhall-levels",
        "traps",
        "guardians",
        "helpers"
    };

            foreach (var targetKey in targetKeys)
            {
                var processor = _processorRegistry.GetRequired(targetKey);

                var context = new StaticDataProcessorContext
                {
                    RunId = run.Id,
                    Fingerprint = request.Fingerprint,
                    Db = _db,
                    Downloader = _downloader,
                    Decompressor = _decompressor,
                    RunTracker = _runTracker
                };

                await processor.ProcessAsync(context, cancellationToken);
            }

            run.Status = StaticDataRunStatus.Succeeded;
            run.CompletedAtUtc = DateTime.UtcNow;
            run.Message = "Static data generation completed successfully.";
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            run.Status = StaticDataRunStatus.Failed;
            run.CompletedAtUtc = DateTime.UtcNow;
            run.Message = ex.Message;
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }

        return run.Id;
    }

    public async Task<StaticDataRunDto?> GetRunAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _db.StaticDataRuns
            .AsNoTracking()
            .Include(x => x.Steps)
            .FirstOrDefaultAsync(x => x.Id == runId, cancellationToken);

        if (run is null)
            return null;

        return new StaticDataRunDto
        {
            Id = run.Id,
            Fingerprint = run.Fingerprint,
            Status = run.Status.ToString(),
            Steps = run.Steps
                .OrderBy(x => x.TargetKey)
                .ThenBy(x => x.StepType)
                .Select(x => new StaticDataRunStepDto
                {
                    TargetKey = x.TargetKey,
                    StepType = x.StepType.ToString(),
                    Status = x.Status.ToString(),
                    Message = x.Message
                })
                .ToList()
        };
    }
}