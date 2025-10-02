namespace GoalFlow.Api.Contracts
{
    /// <summary>
    /// Request body for creating a new goal.
    /// </summary>
    /// <param name="Title">The title of the goal (e.g., "Learn React").</param>
    /// <param name="Specific">A clear and specific statement of what the user wants to achieve.</param>
    /// <param name="Measurable">A measurable indicator of progress or success.</param>
    /// <param name="Achievable">A statement confirming the goal is realistic and attainable.</param>
    /// <param name="Relevant">Explains why this goal matters and aligns with the user's objectives.</param>
    /// <param name="TimeBound">The target completion date and time for the goal.</param>
    /// <param name="Description">An optional free-form description with additional details.</param>
    /// <param name="Priority">The goal priority level (allowed values: Low | Medium | High).</param>
    public record CreateGoalBody(
        string Title,
        string Specific,
        string Measurable,
        string Achievable,
        string Relevant,
        DateTimeOffset TimeBound,
        string? Description,
        string Priority // Low|Medium|High
    );

    /// <summary>
    /// Request body for creating a new progress log entry associated with a goal.
    /// </summary>
    /// <param name="Delta">The amount of progress made (e.g., +15 minutes, +2 tasks completed).</param>
    /// <param name="Note">An optional note or comment describing the progress update.</param>
    public record CreateProgressBody(int Delta, string? Note);
}
