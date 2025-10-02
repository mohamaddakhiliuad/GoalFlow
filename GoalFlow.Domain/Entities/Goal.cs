using System;

namespace GoalFlow.Domain.Entities;

/// <summary>
/// Represents the possible lifecycle states of a goal.
/// </summary>
public enum GoalStatus
{
    Draft,
    Active,
    Completed,
    Archived
}

/// <summary>
/// Represents the priority level of a goal.
/// </summary>
public enum GoalPriority
{
    Low,
    Medium,
    High
}

/// <summary>
/// The Goal aggregate root entity.
/// Encapsulates all business rules and state for user-defined goals,
/// including SMART attributes and lifecycle transitions.
/// </summary>
public sealed class Goal
{
    /// <summary>
    /// Unique identifier for the goal.
    /// </summary>
    public Guid Id { get; private set; } = Guid.NewGuid();

    /// <summary>
    /// Identifier of the user who owns this goal.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Short descriptive title of the goal.
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// Optional detailed description of the goal.
    /// </summary>
    public string? Description { get; private set; }

    /// <summary>
    /// Indicates the relative importance of the goal.
    /// </summary>
    public GoalPriority Priority { get; private set; }

    /// <summary>
    /// Current status of the goal in its lifecycle.
    /// </summary>
    public GoalStatus Status { get; private set; } = GoalStatus.Draft;

    // SMART properties (behavioural requirements)
    public string Specific { get; private set; }
    public string Measurable { get; private set; }
    public string Achievable { get; private set; }
    public string Relevant { get; private set; }
    public DateTimeOffset TimeBound { get; private set; }

    /// <summary>
    /// Date and time the goal was created (UTC).
    /// </summary>
    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Required for EF Core. Prevents direct construction without parameters.
    /// </summary>
    private Goal() { }

    /// <summary>
    /// Creates a new active goal with required SMART attributes.
    /// </summary>
    public Goal(
        Guid userId,
        string title,
        string specific,
        string measurable,
        string achievable,
        string relevant,
        DateTimeOffset timeBound,
        string? description = null,
        GoalPriority priority = GoalPriority.Medium)
    {
        UserId = userId;
        Title = title ?? throw new ArgumentNullException(nameof(title));
        Description = description;
        Priority = priority;

        Specific = specific;
        Measurable = measurable;
        Achievable = achievable;
        Relevant = relevant;
        TimeBound = timeBound;

        Status = GoalStatus.Active;
    }

    /// <summary>
    /// Updates all key details of the goal.
    /// Validates title and enforces basic invariants.
    /// </summary>
    public void UpdateDetails(
        string title,
        string specific,
        string measurable,
        string achievable,
        string relevant,
        DateTimeOffset timeBound,
        string? description,
        GoalPriority priority,
        GoalStatus status)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title is required.", nameof(title));

        Title = title.Trim();
        Specific = specific;
        Measurable = measurable;
        Achievable = achievable;
        Relevant = relevant;
        TimeBound = timeBound;
        Description = description;
        Priority = priority;
        Status = status;

        // TODO: add UpdatedAt tracking if needed
    }

    /// <summary>
    /// Marks the goal as completed.
    /// </summary>
    public void Complete() => Status = GoalStatus.Completed;

    /// <summary>
    /// Moves the goal into an archived state.
    /// </summary>
    public void Archive() => Status = GoalStatus.Archived;
}
