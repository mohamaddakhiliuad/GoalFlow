using GoalFlow.Application.Common;
using GoalFlow.Application.Goals;
using GoalFlow.Infrastructure.Caching;
using GoalFlow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GoalFlow.Infrastructure.Goals;

/// <summary>
/// Handles deletion of a Goal entity.
/// 
/// 🔑 Teaching Point:
/// - Shows full CQRS delete pipeline with EF Core + cache invalidation.
/// - Demonstrates ownership enforcement (user must own the goal).
/// - Uses cache tagging pattern to ensure user cache stays consistent.
/// </summary>
public sealed class DeleteGoalHandler : IRequestHandler<DeleteGoalCommand, Result<bool>>
{
    private readonly GoalFlowDbContext _db;
    private readonly IGoalsCache _cache;

    public DeleteGoalHandler(GoalFlowDbContext db, IGoalsCache cache)
    {
        _db = db;
        _cache = cache;
    }

    /// <summary>
    /// Handle DeleteGoalCommand:
    /// 1) Lookup goal by Id + UserId (ownership enforced).
    /// 2) If not found → return failure result.
    /// 3) Remove goal from DbContext.
    /// 4) Commit changes to DB.
    /// 5) Invalidate user's cache entries.
    /// 6) Return success.
    /// </summary>
    public async Task<Result<bool>> Handle(DeleteGoalCommand r, CancellationToken ct)
    {
        // Step 1: Lookup goal ensuring it belongs to current user
        var goal = await _db.Goals.FirstOrDefaultAsync(
            g => g.Id == r.Id && g.UserId == r.UserId, ct
        );

        // Step 2: Fail fast if not found or not owned by user
        if (goal is null)
            return Result<bool>.Failure("Goal.NotFound", "Goal not found or not owned by user.");

        // Step 3: Mark entity for deletion
        _db.Goals.Remove(goal);

        // Step 4: Commit deletion
        await _db.SaveChangesAsync(ct);

        // Step 5: Invalidate all cached goal pages for this user
        await _cache.InvalidateUser(r.UserId);

        // Step 6: Return success
        return Result<bool>.Success(true);
    }
}
