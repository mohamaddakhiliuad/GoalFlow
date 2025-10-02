using MediatR;

namespace GoalFlow.Application.Goals
{
    /// <summary>
    /// Query to retrieve a paginated list of goals for a specific user.
    /// Implements <see cref="IRequest{TResponse}"/> returning a
    /// <see cref="PagedResult{GoalSummaryDto}"/>.
    ///
    /// Notes:
    /// - Supports optional filters by search term, status, and priority.
    /// - Results are paginated to improve performance and scalability.
    /// - Enforces ownership by requiring a user ID.
    /// - Read-only operation; does not modify application state.
    /// </summary>
    /// <param name="UserId">The identifier of the user whose goals are being retrieved.</param>
    /// <param name="Page">The page index to retrieve (1-based, defaults to 1).</param>
    /// <param name="PageSize">The number of goals per page (defaults to 20).</param>
    /// <param name="Search">Optional free-text search on goal title/description.</param>
    /// <param name="Status">Optional filter by goal status (e.g., Active, Completed).</param>
    /// <param name="Priority">Optional filter by goal priority (Low, Medium, High).</param>
    public record GetGoalsQuery(
        Guid UserId,
        int Page = 1,
        int PageSize = 20,
        string? Search = null,
        string? Status = null,
        string? Priority = null
    ) : IRequest<PagedResult<GoalSummaryDto>>;

    /// <summary>
    /// Data transfer object representing a summary view of a goal.
    /// Used in paginated listings (not full details).
    /// </summary>
    /// <param name="Id">Unique identifier of the goal.</param>
    /// <param name="Title">Short title or headline of the goal.</param>
    /// <param name="Description">Optional brief description.</param>
    /// <param name="Priority">Priority level (Low, Medium, High).</param>
    /// <param name="Status">Current status (e.g., Active, Completed, Cancelled).</param>
    /// <param name="TimeBound">Deadline or target completion date/time.</param>
    /// <param name="CreatedAt">Timestamp (UTC) when the goal was created.</param>
    public record GoalSummaryDto(
        Guid Id,
        string Title,
        string? Description,
        string Priority,
        string Status,
        DateTimeOffset TimeBound,
        DateTimeOffset CreatedAt
    );

    /// <summary>
    /// Generic wrapper for paginated results.
    /// Provides the returned items along with pagination metadata.
    /// </summary>
    /// <typeparam name="T">The type of the items being paginated.</typeparam>
    /// <param name="Items">The collection of results on the current page.</param>
    /// <param name="Page">The current page index (1-based).</param>
    /// <param name="PageSize">The number of items per page.</param>
    /// <param name="Total">The total number of items across all pages.</param>
    public record PagedResult<T>(
        IReadOnlyList<T> Items,
        int Page,
        int PageSize,
        int Total
    );
}
