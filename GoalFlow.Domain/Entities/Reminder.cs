namespace GoalFlow.Domain.Entities;

/// <summary>
/// Specifies the delivery channel for reminders.
/// </summary>
public enum ReminderChannel
{
    Email,
    Push
}

/// <summary>
/// Represents a reminder scheduled for a goal.
/// Reminders are configured using cron expressions
/// and may trigger via different channels (e.g., email or push).
/// </summary>
public sealed class Reminder
{
    /// <summary>
    /// Unique identifier for this reminder.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// The identifier of the goal this reminder belongs to.
    /// </summary>
    public Guid GoalId { get; private set; }

    /// <summary>
    /// The channel used to deliver the reminder (Email or Push).
    /// </summary>
    public ReminderChannel Channel { get; private set; }

    /// <summary>
    /// Cron expression defining when the reminder should trigger.
    /// Default is "0 * * * *" (once per hour).
    /// </summary>
    public string CronExpr { get; private set; } = "0 * * * *";

    /// <summary>
    /// The next scheduled run time (UTC) of this reminder.
    /// Calculated externally by scheduling infrastructure.
    /// </summary>
    public DateTimeOffset? NextRun { get; private set; }

    /// <summary>
    /// Indicates whether the reminder is active.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Date and time (UTC) when the reminder was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Required for EF Core materialization.
    /// </summary>
    private Reminder() { }

    /// <summary>
    /// Creates a new reminder for a specific goal.
    /// </summary>
    /// <param name="goalId">The goal identifier.</param>
    /// <param name="channel">The delivery channel (Email or Push).</param>
    /// <param name="cronExpr">Cron expression for scheduling.</param>
    public Reminder(Guid goalId, ReminderChannel channel, string cronExpr)
    {
        GoalId = goalId;
        Channel = channel;
        CronExpr = cronExpr;
    }

    /// <summary>
    /// Updates the next scheduled run time (UTC).
    /// Typically called by the infrastructure layer
    /// (e.g., background job scheduler).
    /// </summary>
    /// <param name="nextUtc">The next scheduled run time in UTC.</param>
    public void SetNextRun(DateTimeOffset nextUtc) => NextRun = nextUtc;
}
