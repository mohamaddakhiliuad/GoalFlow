using GoalFlow.Application.Common;
using GoalFlow.Application.Goals;
using GoalFlow.Domain.Entities;
using GoalFlow.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NCrontab; // Infrastructure-level dependency for cron parsing

namespace GoalFlow.Infrastructure.Goals;

/// <summary>
/// Handles creation of a new Reminder entity for a goal.
/// 
/// 🔑 Teaching Point:
/// - Demonstrates Infrastructure logic where domain + external libs meet.
/// - Uses EF Core persistence + NCrontab schedule calculation.
/// - Encapsulates reminder creation flow end-to-end.
/// </summary>
public class CreateReminderHandler : IRequestHandler<CreateReminderCommand, Result<Guid>>
{
    private readonly GoalFlowDbContext _db;

    public CreateReminderHandler(GoalFlowDbContext db) => _db = db;

    /// <summary>
    /// Handle CreateReminderCommand:
    /// 1) Ensure goal exists.
    /// 2) Create Reminder domain entity.
    /// 3) Calculate next execution time using NCrontab.
    /// 4) Save to database.
    /// 5) Return new reminder Id.
    /// </summary>
    public async Task<Result<Guid>> Handle(CreateReminderCommand r, CancellationToken ct)
    {
        // Step 1: Validate goal existence
        bool goalExists = await _db.Goals.AnyAsync(g => g.Id == r.GoalId, ct);
        if (!goalExists)
            return Result<Guid>.Failure("Goal.NotFound", "Goal not found.");

        // Step 2: Construct Reminder domain entity
        var reminder = new Reminder(r.GoalId, r.Channel, r.CronExpr);

        // Step 3: Compute next run time using NCrontab
        // Infrastructure concern: domain only stores cron string;
        // Infrastructure interprets it and sets the actual next runtime.
        var schedule = CrontabSchedule.Parse(reminder.CronExpr);
        var next = schedule.GetNextOccurrence(DateTime.UtcNow);
        reminder.SetNextRun(new DateTimeOffset(next, TimeSpan.Zero));

        // Step 4: Persist reminder in DbContext
        _db.Reminders.Add(reminder);
        await _db.SaveChangesAsync(ct);

        // Step 5: Return result
        return Result<Guid>.Success(reminder.Id);
    }
}
