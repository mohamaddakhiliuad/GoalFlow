using GoalFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace GoalFlow.Infrastructure.Reminders;

/// <summary>
/// Background processor that evaluates and executes due reminders.
/// </summary>
/// <remarks>
/// This service queries the database for active reminders whose scheduled
/// execution time (<see cref="Reminder.NextRun"/>) is either null or past due.
/// 
/// When a reminder is triggered:
/// - A notification is logged (placeholder for email/push delivery).
/// - The next run time is recalculated using <see cref="NCrontab"/> based on the stored cron expression.
/// - Updated reminders are persisted back to the database.
/// 
/// This processor is typically invoked on a schedule (e.g., via Hangfire, Quartz, or a hosted service).
/// </remarks>
public class ReminderProcessor
{
    private readonly GoalFlowDbContext _db;
    private readonly ILogger<ReminderProcessor> _log;

    /// <summary>
    /// Creates a new instance of <see cref="ReminderProcessor"/>.
    /// </summary>
    /// <param name="db">The EF Core database context.</param>
    /// <param name="log">The logger for diagnostic and audit information.</param>
    public ReminderProcessor(GoalFlowDbContext db, ILogger<ReminderProcessor> log)
    {
        _db = db;
        _log = log;
    }

    /// <summary>
    /// Processes due reminders:
    /// 1. Queries up to 100 active reminders where <c>NextRun</c> is null or past due.
    /// 2. Logs a notification (placeholder for actual delivery).
    /// 3. Computes the next run using the reminder's cron expression.
    /// 4. Updates the reminder in the database.
    /// </summary>
    public async Task ProcessDueReminders()
    {
        var now = DateTimeOffset.UtcNow;

        // Step 1: Query reminders that are due
        var due = await _db.Reminders
            .Where(r => r.IsActive && (r.NextRun == null || r.NextRun <= now))
            .OrderBy(r => r.NextRun)
            .Take(100)
            .ToListAsync();

        foreach (var r in due)
        {
            // Step 2: Log reminder (placeholder for email/push send)
            _log.LogInformation(
                "Reminder fired: Goal {GoalId} via {Channel} at {Now}",
                r.GoalId, r.Channel, now);

            // Step 3: Compute next occurrence using NCrontab
            var schedule = CrontabSchedule.Parse(r.CronExpr);
            var next = schedule.GetNextOccurrence(now.UtcDateTime);

            // Step 4: Update entity with new NextRun
            r.SetNextRun(new DateTimeOffset(next, TimeSpan.Zero));
        }

        // Step 5: Save changes if any reminders were processed
        if (due.Count > 0)
            await _db.SaveChangesAsync();
    }
}
