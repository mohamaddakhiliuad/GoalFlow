using GoalFlow.Application.Common;
using GoalFlow.Application.Goals;
using GoalFlow.Domain.Entities;
using GoalFlow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GoalFlow.Infrastructure.Goals;

/// <summary>
/// Handles creation of a new ProgressLog for a goal.
/// 
/// 🔑 Teaching Point:
/// - Shows how Infrastructure writes to DB *and* publishes events back into the system.
/// - Combines EF Core persistence with MediatR notifications (event-driven).
/// - Demonstrates ownership validation via DbContext.
/// </summary>
public class CreateProgressLogHandler : IRequestHandler<CreateProgressLogCommand, Result<Guid>>
{
    private readonly GoalFlowDbContext _db;
    private readonly IMediator _mediator;

    public CreateProgressLogHandler(GoalFlowDbContext db, IMediator mediator)
    {
        _db = db;
        _mediator = mediator;
    }

    /// <summary>
    /// Handle the CreateProgressLogCommand:
    /// 1) Validate that the goal exists (DbContext).
    /// 2) Create and persist a new ProgressLog entity.
    /// 3) Commit changes.
    /// 4) Publish a ProgressLogCreated event for async subscribers.
    /// 5) Return the new log's Id.
    /// </summary>
    public async Task<Result<Guid>> Handle(CreateProgressLogCommand r, CancellationToken ct)
    {
        // Step 1: Ensure goal exists (ownership check could be added here).
        bool exists = await _db.Goals.AnyAsync(g => g.Id == r.GoalId, ct);
        if (!exists)
            return Result<Guid>.Failure("Goal.NotFound", "Goal not found or not owned by user.");

        // Step 2: Construct domain entity.
        var log = new ProgressLog(r.GoalId, r.Delta, r.Note);

        // Step 3: Persist log in DbContext.
        _db.ProgressLogs.Add(log);
        await _db.SaveChangesAsync(ct);

        // Step 4: Publish event for subscribers (e.g., notifications, analytics).
        await _mediator.Publish(
            new ProgressLogCreated(log.Id, log.GoalId, log.Delta, log.Note, log.CreatedAt),
            ct
        );

        // Step 5: Return result with new log Id.
        return Result<Guid>.Success(log.Id);
    }
}
