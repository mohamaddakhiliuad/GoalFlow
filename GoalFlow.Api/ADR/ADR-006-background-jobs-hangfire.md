# ADR-006: Background Jobs with Hangfire

- **Status:** Accepted
- **Date:** 2025-09-30
- **Layer:** API

## Context
We need scheduled/recurring tasks (e.g., reminder dispatch) independent of request/response flow.

## Decision
- Use **Hangfire** for job scheduling and monitoring.
- Expose dashboard at `/hangfire` (development only or protected in production).
- Sample recurring job: `ReminderProcessor.ProcessDueReminders()` every minute.

## Consequences
**Positive**
- Reliable, observable job execution with retries and dashboard.
- Decouples time-based workflows from API requests.

**Trade-offs**
- Requires storage/backing services and dashboard hardening in production.
- Operational overhead (authN/Z for the dashboard, alerts).

## References
- `RecurringJob.AddOrUpdate(...)` in `Program.cs`
- `GoalFlow.Infrastructure.Reminders.ReminderProcessor`
