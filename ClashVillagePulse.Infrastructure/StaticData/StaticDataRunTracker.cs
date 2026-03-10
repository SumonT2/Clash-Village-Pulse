using ClashVillagePulse.Domain.Entities;
using ClashVillagePulse.Domain.Enums;
using ClashVillagePulse.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace ClashVillagePulse.Infrastructure.StaticData;

public class StaticDataRunTracker
{
    private readonly AppDbContext _db;

    public StaticDataRunTracker(AppDbContext db)
    {
        _db = db;
    }

    public async Task<StaticDataRunStep> StartStepAsync(
        Guid runId,
        string targetKey,
        StaticDataStepType stepType,
        CancellationToken cancellationToken = default)
    {
        var step = await _db.StaticDataRunSteps.FirstOrDefaultAsync(
            x => x.StaticDataRunId == runId &&
                 x.TargetKey == targetKey &&
                 x.StepType == stepType,
            cancellationToken);

        if (step is null)
        {
            step = new StaticDataRunStep
            {
                Id = Guid.NewGuid(),
                StaticDataRunId = runId,
                TargetKey = targetKey,
                StepType = stepType,
                Status = StaticDataStepStatus.Pending
            };

            _db.StaticDataRunSteps.Add(step);
        }

        step.Status = StaticDataStepStatus.Running;
        step.StartedAtUtc = DateTime.UtcNow;
        step.CompletedAtUtc = null;
        step.Message = null;

        await _db.SaveChangesAsync(cancellationToken);

        return step;
    }

    public async Task CompleteStepAsync(
        Guid stepId,
        string? message = null,
        CancellationToken cancellationToken = default)
    {
        var step = await _db.StaticDataRunSteps
            .FirstAsync(x => x.Id == stepId, cancellationToken);

        step.Status = StaticDataStepStatus.Succeeded;
        step.CompletedAtUtc = DateTime.UtcNow;
        step.Message = message;

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task FailStepAsync(
        Guid stepId,
        string message,
        CancellationToken cancellationToken = default)
    {
        var step = await _db.StaticDataRunSteps
            .FirstAsync(x => x.Id == stepId, cancellationToken);

        step.Status = StaticDataStepStatus.Failed;
        step.CompletedAtUtc = DateTime.UtcNow;
        step.Message = message;

        await _db.SaveChangesAsync(cancellationToken);
    }
}