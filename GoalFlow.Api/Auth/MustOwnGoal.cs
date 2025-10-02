using System.Security.Claims;
using GoalFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GoalFlow.Api.Auth;

/// <summary>
/// Represents a requirement that ensures a user can only access or modify their own goal.
/// </summary>
public sealed class MustOwnGoal : IAuthorizationRequirement { }

/// <summary>
/// Authorization handler that validates whether the current user owns the goal
/// specified in the route.
/// </summary>
public sealed class MustOwnGoalHandler : AuthorizationHandler<MustOwnGoal>
{
    private readonly GoalFlowDbContext _db;

    /// <summary>
    /// Creates a new instance of the <see cref="MustOwnGoalHandler"/>.
    /// </summary>
    /// <param name="db">The database context used to validate goal ownership.</param>
    public MustOwnGoalHandler(GoalFlowDbContext db) => _db = db;

    /// <summary>
    /// Handles the authorization logic to verify if the current user owns the goal.
    /// </summary>
    /// <param name="context">The authorization context.</param>
    /// <param name="requirement">The ownership requirement being validated.</param>
    /// <remarks>
    /// Extracts the goal ID from the route (<c>/api/goals/{id}</c>) and compares it
    /// with the current user's UID claim. If the user is the owner, the requirement succeeds.
    /// </remarks>
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        MustOwnGoal requirement)
    {
        if (context.Resource is not HttpContext http)
            return;

        var routeId = http.Request.RouteValues["id"] as string; // /api/goals/{id}
        var uid = context.User.FindFirst("uid")?.Value;

        if (Guid.TryParse(routeId, out var goalId) && Guid.TryParse(uid, out var userId))
        {
            bool isOwner = await _db.Goals
                .AsNoTracking()
                .AnyAsync(g => g.Id == goalId && g.UserId == userId);

            if (isOwner)
                context.Succeed(requirement);
        }
    }
}
