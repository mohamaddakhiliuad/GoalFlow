using GoalFlow.Application.Common;
using GoalFlow.Application.Goals;
using GoalFlow.Infrastructure.Caching;
using GoalFlow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GoalFlow.Infrastructure.Goals;

/// <summary>
/// Handles update requests for an existing goal.
/// </summary>
/// <remarks>
/// This command handler ensures that the goal exists and is owned by the requesting user.
/// It parses and validates enum values (<see cref="GoalPriority"/> and <see cref="GoalStatus"/>),
/// updates the domain entity through its aggregate method, commits changes, and finally
/// invalidates all cached goal lists for the user to prevent stale reads.
/// </remarks>
public sealed class UpdateGoalHandler : IRequestHandler<UpdateGoalCommand, Result<bool>>
{
    private readonly GoalFlowDbContext _db;
    private readonly IGoalsCache _cache;

    /// <summary>
    /// Creates a new instance of <see cref="UpdateGoalHandler"/>.
    /// </summary>
    /// <param name="db">The EF Core DbContext used for persistence.</param>
    /// <param name="cache">The caching abstraction for goal lists.</param>
    public UpdateGoalHandler(GoalFlowDbContext db, IGoalsCache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>
    /// Handles the <see cref="UpdateGoalCommand"/> request.
    /// </summary>
    /// <param name="r">The update command containing the new goal details.</param>
    /// <param name="ct">A cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> indicating success or failure.
    /// Returns <c>true</c> on success, or a failure with a code and message otherwise.
    /// </returns>
    public async Task<Result<bool>> Handle(UpdateGoalCommand r, CancellationToken ct)
    {
        // Step 1: Fetch goal with ownership check
        var goal = await _db.Goals.FirstOrDefaultAsync(
            g => g.Id == r.Id && g.UserId == r.UserId, ct);

        if (goal is null)
            return Result<bool>.Failure("Goal.NotFound", "Goal not found or not owned by user.");

        // Step 2: Parse and validate enum values (priority and status)
        if (!Enum.TryParse<GoalFlow.Domain.Entities.GoalPriority>(r.Priority, true, out var p))
            return Result<bool>.Failure("Goal.Priority", "Invalid priority.");

        if (!Enum.TryParse<GoalFlow.Domain.Entities.GoalStatus>(r.Status, true, out var s))
            return Result<bool>.Failure("Goal.Status", "Invalid status.");

        // Step 3: Update domain entity through aggregate method
        goal.UpdateDetails(
            r.Title,
            r.Specific,
            r.Measurable,
            r.Achievable,
            r.Relevant,
            r.TimeBound,
            r.Description,
            p,
            s
        );

        // Step 4: Persist changes
        await _db.SaveChangesAsync(ct);

        // Step 5: Invalidate all cached goal lists for this user
        await _cache.InvalidateUser(r.UserId);

        return Result<bool>.Success(true);
    }
}
