using FluentValidation;
using MediatR;
using GoalFlow.Application.Common;

namespace GoalFlow.Application.Goals
{
    /// <summary>
    /// Command to update an existing goal.
    /// Implements <see cref="IRequest{TResponse}"/> returning
    /// a <see cref="Result{Boolean}"/> indicating success or failure.
    ///
    /// Notes:
    /// - Requires both goal ID and user ID to ensure ownership.
    /// - Updates all SMART criteria, title, description, priority,
    ///   status, and time-bound deadline.
    /// - Handled by a dedicated MediatR handler that validates
    ///   ownership, applies changes, and persists updates.
    /// </summary>
    /// <param name="Id">The identifier of the goal to update.</param>
    /// <param name="UserId">The identifier of the user performing the update (ownership enforcement).</param>
    /// <param name="Title">Short title or headline for the goal.</param>
    /// <param name="Specific">SMART criteria: Specific.</param>
    /// <param name="Measurable">SMART criteria: Measurable.</param>
    /// <param name="Achievable">SMART criteria: Achievable.</param>
    /// <param name="Relevant">SMART criteria: Relevant.</param>
    /// <param name="TimeBound">Updated deadline or completion date/time.</param>
    /// <param name="Description">Optional detailed description.</param>
    /// <param name="Priority">Priority level (Low, Medium, High).</param>
    /// <param name="Status">Current status (e.g., Active, Completed, Cancelled).</param>
    public record UpdateGoalCommand(
        Guid Id,
        Guid UserId,
        string Title,
        string Specific,
        string Measurable,
        string Achievable,
        string Relevant,
        DateTimeOffset TimeBound,
        string? Description,
        string Priority,
        string Status
    ) : IRequest<Result<bool>>;

    /// <summary>
    /// Validator for <see cref="UpdateGoalCommand"/>.
    /// Ensures that the command has valid identifiers and
    /// required fields for title, priority, and status.
    ///
    /// Uses FluentValidation to enforce input consistency
    /// before the handler is executed.
    /// </summary>
    public class UpdateGoalValidator : AbstractValidator<UpdateGoalCommand>
    {
        public UpdateGoalValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
            RuleFor(x => x.Priority).NotEmpty();
            RuleFor(x => x.Status).NotEmpty();
        }
    }
}
