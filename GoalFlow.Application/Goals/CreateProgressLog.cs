using FluentValidation;
using MediatR;
using GoalFlow.Application.Common;

namespace GoalFlow.Application.Goals
{
    /// <summary>
    /// Command to create a new progress log entry for a goal.
    /// Implements <see cref="IRequest{TResponse}"/> with a response
    /// of <see cref="Result{Guid}"/> containing the new log ID.
    ///
    /// Notes:
    /// - Supports positive and negative deltas (e.g., +15 minutes
    ///   worked or -5 minutes corrected).
    /// - Handled by a MediatR command handler in the Application layer.
    /// </summary>
    /// <param name="GoalId">The identifier of the goal being updated.</param>
    /// <param name="UserID">The identifier of the user making the update.</param>
    /// <param name="Delta">The amount of progress (+/-) to record.</param>
    /// <param name="Note">Optional comment or annotation for the log.</param>
    public record CreateProgressLogCommand(
        Guid GoalId,
        Guid UserID,
        int Delta,
        string? Note
    ) : IRequest<Result<Guid>>;

    /// <summary>
    /// Validator for <see cref="CreateProgressLogCommand"/>.
    /// Ensures that the command has a valid user, goal,
    /// and a reasonable delta with optional notes.
    /// </summary>
    public class CreateProgressLogValidator : AbstractValidator<CreateProgressLogCommand>
    {
        public CreateProgressLogValidator()
        {
            RuleFor(x => x.UserID).NotEmpty();
            RuleFor(x => x.GoalId).NotEmpty();
            RuleFor(x => x.Delta).InclusiveBetween(-100, 100);
            RuleFor(x => x.Note).MaximumLength(500);
        }
    }

    /// <summary>
    /// Data transfer object representing a progress log entry.
    /// Returned to clients when querying logs for a goal.
    /// </summary>
    /// <param name="Id">Unique identifier of the progress log.</param>
    /// <param name="GoalId">Identifier of the associated goal.</param>
    /// <param name="Delta">The recorded progress (+/- value).</param>
    /// <param name="Note">Optional comment or annotation.</param>
    /// <param name="CreatedAt">Timestamp (UTC) when the log was created.</param>
    public record ProgressLogDto(
        Guid Id,
        Guid GoalId,
        int Delta,
        string? Note,
        DateTimeOffset CreatedAt
    );

    /// <summary>
    /// Query to retrieve paginated progress logs for a goal.
    /// Implements <see cref="IRequest{TResponse}"/> returning
    /// a read-only list of <see cref="ProgressLogDto"/>.
    ///
    /// Defaults:
    /// - Page = 1 (first page)
    /// - PageSize = 50 (maximum logs per page)
    /// </summary>
    /// <param name="GoalId">The identifier of the goal to retrieve logs for.</param>
    /// <param name="Page">The page index to retrieve (1-based).</param>
    /// <param name="PageSize">The number of logs per page.</param>
    public record GetProgressLogsQuery(
        Guid GoalId,
        int Page = 1,
        int PageSize = 50
    ) : IRequest<IReadOnlyList<ProgressLogDto>>;
}
