namespace GoalFlow.Domain.Entities;

/// <summary>
/// Represents a progress update made against a specific goal.
/// Used to track incremental achievements or setbacks over time.
/// </summary>
public sealed class ProgressLog
{
    /// <summary>
    /// Unique identifier for this log entry.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// The identifier of the goal this progress log belongs to.
    /// </summary>
    public Guid GoalId { get; private set; }

    /// <summary>
    /// The change value expressed as an integer (positive for progress, negative for regression).
    /// Example: +10 means 10% progress, -5 means a 5% setback.
    /// </summary>
    public int Delta { get; private set; }

    /// <summary>
    /// Optional descriptive note provided by the user for context.
    /// </summary>
    public string? Note { get; private set; }

    /// <summary>
    /// Date and time when the progress log entry was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Required for EF Core materialization.
    /// Prevents direct construction without parameters.
    /// </summary>
    private ProgressLog() { }

    /// <summary>
    /// Creates a new progress log entry for a goal.
    /// </summary>
    /// <param name="goalId">The goal identifier associated with this log.</param>
    /// <param name="delta">The change amount (positive or negative).</param>
    /// <param name="note">Optional user-provided note describing the update.</param>
    public ProgressLog(Guid goalId, int delta, string? note)
    {
        GoalId = goalId;
        Delta = delta;
        Note = note;
    }
}
