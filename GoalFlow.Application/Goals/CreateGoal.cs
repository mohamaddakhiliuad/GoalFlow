using System;
using FluentValidation;
using MediatR;
using GoalFlow.Domain.Entities;
using GoalFlow.Application.Common;

namespace GoalFlow.Application.Goals
{
    /// <summary>
    /// Command representing the creation of a new SMART goal
    /// by a specific user. Implements <see cref="IRequest{TResponse}"/>
    /// to work with MediatR, returning a <see cref="Result{Guid}"/>
    /// containing the ID of the newly created goal on success.
    ///
    /// Notes:
    /// - Follows the CQRS pattern: intent is captured as a command,
    ///   and processed by a dedicated handler.
    /// - Uses the <see cref="Result{T}"/> wrapper to enforce
    ///   explicit success/failure semantics.
    /// </summary>
    /// <param name="UserId">The identifier of the user creating the goal.</param>
    /// <param name="Title">Short title or headline for the goal (max 120 characters).</param>
    /// <param name="Specific">The “Specific” part of SMART criteria (what exactly is to be achieved).</param>
    /// <param name="Measurable">The “Measurable” part of SMART criteria (how success will be tracked).</param>
    /// <param name="Achievable">The “Achievable” part of SMART criteria (ensuring feasibility).</param>
    /// <param name="Relevant">The “Relevant” part of SMART criteria (why this goal matters).</param>
    /// <param name="TimeBound">The deadline or target completion date/time (must be in the future).</param>
    /// <param name="Description">Optional free-form description for additional context.</param>
    /// <param name="Priority">Priority level of the goal (Low, Medium, High).</param>
    public record CreateGoalCommand(
        Guid UserId,
        string Title,
        string Specific,
        string Measurable,
        string Achievable,
        string Relevant,
        DateTimeOffset TimeBound,
        string? Description,
        GoalPriority Priority
    ) : IRequest<Result<Guid>>;

    /// <summary>
    /// Validator for <see cref="CreateGoalCommand"/>.
    /// Ensures that SMART criteria are present and the goal
    /// has a valid title, user, and future deadline.
    ///
    /// Uses FluentValidation to enforce business rules
    /// at the application boundary.
    /// </summary>
    public class CreateGoalValidator : AbstractValidator<CreateGoalCommand>
    {
        public CreateGoalValidator()
        {
            RuleFor(x => x.UserId).NotEmpty();
            RuleFor(x => x.Title).NotEmpty().MaximumLength(120);
            RuleFor(x => x.Specific).NotEmpty();
            RuleFor(x => x.Measurable).NotEmpty();
            RuleFor(x => x.Achievable).NotEmpty();
            RuleFor(x => x.Relevant).NotEmpty();
            RuleFor(x => x.TimeBound).GreaterThan(DateTimeOffset.UtcNow);
        }
    }
}
