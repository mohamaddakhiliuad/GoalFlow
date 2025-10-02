using FluentValidation;
using MediatR;
using GoalFlow.Application.Common;
using GoalFlow.Domain.Entities;

namespace GoalFlow.Application.Goals
{
    /// <summary>
    /// Command to create a reminder for a specific goal.
    /// Implements <see cref="IRequest{TResponse}"/> with a response
    /// of <see cref="Result{Guid}"/> containing the new reminder ID.
    ///
    /// Notes:
    /// - A reminder is tied to a goal and delivered via a chosen channel
    ///   (e.g., email, push notification).
    /// - The schedule is expressed as a CRON string to support flexible
    ///   recurring reminders.
    /// - Handled in the Application layer, decoupled from infrastructure
    ///   (e.g., Hangfire, background jobs).
    /// </summary>
    /// <param name="GoalId">The identifier of the goal the reminder belongs to.</param>
    /// <param name="Channel">The delivery channel (Email or Push).</param>
    /// <param name="CronExpr">The CRON expression defining the schedule.</param>
    public record CreateReminderCommand(
        Guid GoalId,
        ReminderChannel Channel,
        string CronExpr
    ) : IRequest<Result<Guid>>;

    /// <summary>
    /// Validator for <see cref="CreateReminderCommand"/>.
    /// Ensures that a valid goal ID is provided and the CRON
    /// expression is present and within acceptable length.
    ///
    /// This validation occurs before the command is processed
    /// by its handler.
    /// </summary>
    public class CreateReminderValidator : AbstractValidator<CreateReminderCommand>
    {
        public CreateReminderValidator()
        {
            RuleFor(x => x.GoalId).NotEmpty();
            RuleFor(x => x.CronExpr).NotEmpty().MaximumLength(100);
        }
    }
}
