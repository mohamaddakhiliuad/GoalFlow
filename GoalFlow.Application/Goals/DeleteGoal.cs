using MediatR;
using GoalFlow.Application.Common;

namespace GoalFlow.Application.Goals
{
    /// <summary>
    /// Command to delete an existing goal.
    /// Implements <see cref="IRequest{TResponse}"/> with a response
    /// of <see cref="Result{Boolean}"/> indicating success or failure.
    ///
    /// Notes:
    /// - Requires both the goal ID and the user ID to ensure that
    ///   only the owner of the goal can request deletion.
    /// - The handler will enforce ownership policies and remove
    ///   the goal if valid.
    /// - Returns <c>true</c> on success or a failure result
    ///   containing error details.
    /// </summary>
    /// <param name="Id">The identifier of the goal to delete.</param>
    /// <param name="UserId">The identifier of the user requesting the deletion.</param>
    public record DeleteGoalCommand(
        Guid Id,
        Guid UserId
    ) : IRequest<Result<bool>>;
}
