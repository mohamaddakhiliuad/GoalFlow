using MediatR;

namespace GoalFlow.Application.Goals
{
    /// <summary>
    /// Domain event published when a progress log entry is created
    /// for a goal. Used to trigger side effects (e.g., notifications,
    /// reminders, analytics) without coupling components directly.
    ///
    /// Notes:
    /// - Implements <see cref="INotification"/>, making it a "fire-and-forget"
    ///   event in MediatR.
    /// - Handlers can subscribe to this notification to react whenever
    ///   progress is recorded against a goal.
    /// - This promotes a clean separation of concerns by avoiding
    ///   direct dependencies between core use cases and external services.
    /// </summary>
    /// <param name="Id">Unique identifier of the progress log entry.</param>
    /// <param name="GoalId">Identifier of the goal associated with this log.</param>
    /// <param name="Delta">Amount of progress added (e.g., +15 minutes, +3 units).</param>
    /// <param name="Note">Optional note or comment attached to the progress log.</param>
    /// <param name="CreatedAt">Timestamp (UTC) when the progress log was created.</param>
    public record ProgressLogCreated(
        Guid Id,
        Guid GoalId,
        int Delta,
        string? Note,
        DateTimeOffset CreatedAt
    ) : INotification;
}
