# ADR-005: Real-time via SignalR

- **Status:** Accepted
- **Date:** 2025-09-30
- **Layer:** API

## Context
Goal progress updates should appear in real time for connected clients without polling.

## Decision
- Use **SignalR** with a typed hub: `ProgressHub : Hub<IProgressClient>`.
- Clients join per-goal groups via `JoinGoal(goalId)` → group name `goal:{id}`.
- On domain event `ProgressLogCreated`, broadcast `ProgressLogged(dto)` to the corresponding group.

## Consequences
**Positive**
- Instant UX feedback for collaborators on the same goal.
- Well-structured server → client contract via `IProgressClient`.

**Trade-offs**
- Requires sticky connections and CORS credentials in browsers.
- Additional operational considerations (scale-out, backplanes) if needed later.

## References
- `ProgressHub`, `IProgressClient`
- `ProgressLogCreatedHandler` (MediatR notification → SignalR)
- `/hubs/progress` mapping in `Program.cs`
