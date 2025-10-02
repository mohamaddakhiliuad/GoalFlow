using global::GoalFlow.Application.Common;
using global::GoalFlow.Application.Goals;
using global::GoalFlow.Domain.Entities;
using global::GoalFlow.Infrastructure.Persistence;
using MediatR;

namespace GoalFlow.Infrastructure.Goals;

/// <summary>
/// Handles creation of a new Goal entity.
/// 
/// 🔑 Teaching Point:
/// - This is an *Infrastructure handler* implementing Application command.
/// - It bridges the Application request to the actual persistence (EF Core DbContext).
/// - Encapsulates all database interaction for CreateGoalCommand.
/// </summary>
public class CreateGoalHandler : IRequestHandler<CreateGoalCommand, Result<Guid>>
{
    private readonly GoalFlowDbContext _db;

    public CreateGoalHandler(GoalFlowDbContext db) => _db = db;

    /// <summary>
    /// Handles the CreateGoalCommand.
    /// 1) Maps incoming request → Domain entity.
    /// 2) Adds entity to DbContext.
    /// 3) Saves changes (async).
    /// 4) Returns Result with the new entity Id.
    /// </summary>
    public async Task<Result<Guid>> Handle(CreateGoalCommand request, CancellationToken ct)
    {
        // Step 1: Construct a new Goal domain entity from request data.
        var goal = new Goal(
            request.UserId,
            request.Title,
            request.Specific,
            request.Measurable,
            request.Achievable,
            request.Relevant,
            request.TimeBound,
            request.Description,
            request.Priority
        );

        // Step 2: Track the new goal in DbContext.
        _db.Goals.Add(goal);

        // Step 3: Commit to database.
        await _db.SaveChangesAsync(ct);

        // Step 4: Return success result with the new goal ID.
        return Result<Guid>.Success(goal.Id);
    }
}
