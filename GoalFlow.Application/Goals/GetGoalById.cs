using MediatR;

namespace GoalFlow.Application.Goals
{
    /// <summary>
    /// Query to retrieve the full details of a specific goal.
    /// Implements <see cref="IRequest{TResponse}"/> returning
    /// a <see cref="GoalDetailDto"/> or null if not found.
    ///
    /// Notes:
    /// - Requires both goal ID and user ID to enforce ownership.
    /// - Read-only operation; does not modify application state.
    /// - Typically handled by a MediatR query handler that joins
    ///   domain and persistence data into a DTO.
    /// </summary>
    /// <param name="Id">The identifier of the goal to retrieve.</param>
    /// <param name="UserId">The identifier of the user requesting the goal (ownership check).</param>
    public record GetGoalByIdQuery(
        Guid Id,
        Guid UserId
    ) : IRequest<GoalDetailDto?>;

    /// <summary>
    /// Data transfer object representing the full details of a goal.
    /// Returned by <see cref="GetGoalByIdQuery"/>.
    /// </summary>
    /// <param name="Id">Unique identifier of the goal.</param>
    /// <param name="UserId">Identifier of the goal’s owner.</param>
    /// <param name="Title">Short title or headline for the goal.</param>
    /// <param name="Description">Optional detailed description.</param>
    /// <param name="Specific">SMART criteria: Specific (what exactly is to be achieved).</param>
    /// <param name="Measurable">SMART criteria: Measurable (how success is measured).</param>
    /// <param name="Achievable">SMART criteria: Achievable (is it realistic/feasible).</param>
    /// <param name="Relevant">SMART criteria: Relevant (why this goal matters).</param>
    /// <param name="Priority">Priority level (Low, Medium, High).</param>
    /// <param name="Status">Current status of the goal (e.g., Active, Completed, Cancelled).</param>
    /// <param name="TimeBound">Deadline or target completion date/time.</param>
    /// <param name="CreatedAt">Timestamp (UTC) when the goal was created.</param>
    public record GoalDetailDto(
        Guid Id,
        Guid UserId,
        string Title,
        string? Description,
        string Specific,
        string Measurable,
        string Achievable,
        string Relevant,
        string Priority,
        string Status,
        DateTimeOffset TimeBound,
        DateTimeOffset CreatedAt
    );
}
