# GoalFlow – Domain Layer (Teaching Point)

**Version:** v1  
**Audience:** Interviewers, collaborators, and future maintainers  
**Tone/Style:** Canadian professional English (clear, neutral, concise)

---

## 1) Purpose & Scope
The Domain layer defines GoalFlow’s **core business model** independent of frameworks, databases, and UI. It contains **entities, value objects, aggregates, domain rules, and policies**. All behaviour that expresses the ubiquitous language of the product belongs here.

**Out of scope** for this layer: persistence concerns (ORM mappings, repositories), web concerns (controllers/endpoints), and infrastructure details (schedulers, email gateways, caches). Those live in Infrastructure and API layers.

---

## 2) Principles (DDD & Enterprise Practices)
- **Persistence‑ignorant:** Domain types do not depend on EF Core or any database APIs.  
- **Behaviour‑rich:** Prefer behaviour (methods enforcing rules) over anemic data models.  
- **Invariants inside aggregates:** Keep state valid at all times; expose **intention‑revealing methods**.  
- **Encapsulation:** Private setters; construct through **factories/constructors** and **methods** that enforce rules.  
- **Small, well‑named types:** Use enums/value objects to model constraints clearly.  
- **Time is UTC:** Use `DateTimeOffset.UtcNow` for server‑side timestamps.  
- **Security by design:** Do not persist secrets; store only hashes for tokens.  
- **Auditable:** Prefer immutable create‑once logs; if mutability is allowed, consider `UpdatedAt` and/or domain events.  

---

## 3) Key Aggregates & Entities

### 3.1 Goal (Aggregate Root)
Represents a user’s objective with **SMART** fields and lifecycle state.

**Core fields**  
- Identity & ownership: `Id`, `UserId`  
- Content: `Title`, `Description`  
- Priority & status: `GoalPriority (Low|Medium|High)`, `GoalStatus (Draft|Active|Completed|Archived)`  
- SMART: `Specific`, `Measurable`, `Achievable`, `Relevant`, `TimeBound`  
- Audit: `CreatedAt` (UTC)

**Key behaviours**  
- `UpdateDetails(...)` – validates and updates core fields in one place.  
- `Complete()` – transition to `Completed`.  
- `Archive()` – transition to `Archived`.

**Invariants & notes**  
- `Title` is required (non‑blank).  
- SMART attributes must be provided at creation; `TimeBound` expresses a real deadline.  
- Status transitions are explicit methods to avoid accidental invalid states.

---

### 3.2 ProgressLog (Entity)
Append‑only log for **incremental progress** or setbacks on a goal.

**Core fields**  
- Identity: `Id`  
- Relation: `GoalId`  
- Change metric: `Delta` (e.g., +10 for +10%, −5 for −5%)  
- Optional note: `Note`  
- Audit: `CreatedAt` (UTC)

**Behaviour**  
- `Update(delta, note)` is included for demo purposes. In production, prefer **append‑only** semantics for audit integrity.

**Invariants & notes**  
- A `ProgressLog` must reference a valid `GoalId`.  
- Consider capping `Delta` ranges (e.g., −100 to +100) if needed by business rules.

---

### 3.3 RefreshToken (Entity)
Long‑lived token record to support **JWT refresh** securely.

**Core fields**  
- Identity & ownership: `Id`, `UserId`  
- Security: `TokenHash` (**store hash only; never the raw token**)  
- Lifetime: `ExpiresAt`, `CreatedAt` (UTC)  
- Rotation: `RevokedAt`, `ReplacedByToken`

**Behaviour**  
- `IsActive` – true if not revoked and not expired.  
- `Revoke(replacedBy)` – immediate revocation; optionally link to replacement for **rotation** chains.

**Invariants & notes**  
- `TokenHash` is required and should be a SHA‑256 (or stronger) hash.  
- Do not mutate `UserId` or `TokenHash` post‑creation.

---

### 3.4 Reminder (Entity)
Cron‑based reminder configuration for a goal across channels.

**Core fields**  
- Identity & relation: `Id`, `GoalId`  
- Channel: `ReminderChannel (Email|Push)`  
- Schedule: `CronExpr` (default `"0 * * * *"` → every hour)  
- Execution: `NextRun` (UTC), `IsActive`  
- Audit: `CreatedAt` (UTC)

**Behaviour**  
- `SetNextRun(nextUtc)` – infrastructure (scheduler) updates the next fire time.

**Invariants & notes**  
- `CronExpr` is infrastructure‑agnostic (Hangfire/Quartz compatible).  
- `IsActive` toggles delivery without deleting configuration.

---

## 4) Cross‑cutting Domain Concerns

### 4.1 Validation Strategy
- **Constructor/method guards** for required fields (e.g., `Title`).  
- Use **value objects** in future (e.g., `NonEmptyString`, `Percentage`) to centralize rules.  
- Keep domain validation **synchronous and deterministic**.

### 4.2 Domain Events (Future‑Ready)
- Examples: `GoalCompleted`, `ProgressLogged`, `ReminderScheduled`, `RefreshTokenRevoked`.  
- Emitted from domain methods; handled by Application layer to trigger side effects (notifications, projections).  
- Keep event payloads minimal, referencing aggregate IDs.

### 4.3 Concurrency & Consistency
- Aggregate methods should be **atomic** at the application boundary.  
- Use optimistic concurrency in persistence (e.g., rowversion) from Infrastructure.  
- Avoid exposing partial setters that could be interleaved incorrectly.

### 4.4 Time & Time Zones
- Persist and reason in **UTC** (`DateTimeOffset.UtcNow`).  
- Convert to local time **only** at presentation edges.

### 4.5 Security Notes
- **Never** store raw refresh tokens; store **hashes** only.  
- Avoid leaking internal IDs in public URLs if not needed; prefer opaque identifiers where appropriate.  
- Encapsulate status transitions to reduce illegal state exposure.

---

## 5) Coding Conventions
- **XML documentation comments** (`///`) for public types/members.  
- **Private setters**; mutate through behaviour methods.  
- **Enums** for finite sets (`GoalStatus`, `GoalPriority`, `ReminderChannel`).  
- **File/namespace layout:** `GoalFlow.Domain.Entities` for entities and `GoalFlow.Domain.ValueObjects` for future value objects.  
- **Naming:** Clear, intention‑revealing names (Canadian spelling where relevant; e.g., “behaviour”).

---

## 6) Example XML Doc Pattern
```csharp
/// <summary>
/// Updates the goal’s details while enforcing invariants.
/// </summary>
/// <exception cref="ArgumentException">Thrown when <paramref name="title"/> is null or whitespace.</exception>
public void UpdateDetails(...)
```

---

## 7) Folder Structure (Suggested)
```
GoalFlow.Domain/
  Entities/
    Goal.cs
    ProgressLog.cs
    RefreshToken.cs
    Reminder.cs
  ValueObjects/
    (future types)
  Events/
    (future domain events)
  README.md  (this file can be named teaching-point.md)
```

---

## 8) Extensibility Roadmap
- **Value Objects:** `GoalTitle`, `ProgressDelta`, `CronExpression` wrappers with validation.  
- **Domain Events:** Emit on `Complete()`, `Archive()`, `Revoke()`, `SetNextRun()`.  
- **UpdatedAt fields:** Add when mutation beyond creation is required by business/audit.  
- **Policies:** Ownership/business policies as first‑class domain services if logic becomes cross‑aggregate.  
- **Read Models:** Projection layer for dashboards without bloating aggregates.

---

## 9) Quick Review Checklist (PR‑Friendly)
- [ ] Are invariants enforced inside aggregate methods?  
- [ ] Are all timestamps UTC?  
- [ ] Are public members documented with XML comments?  
- [ ] Are secrets stored as hashes only (e.g., refresh tokens)?  
- [ ] Are setters private and mutations intentional?  
- [ ] Do names match the ubiquitous language (SMART, Goal, Progress, Reminder)?  
- [ ] Are future events/VOs considered but not prematurely added?

---

## 10) ADR Snapshot (Why This Design)
- **DDD‑style aggregates** improve correctness and interview readability.  
- **Persistence‑ignorant design** allows swapping Infrastructure (EF Core, Dapper) without touching domain logic.  
- **Explicit behaviour methods** (e.g., `Complete`, `Archive`, `UpdateDetails`) communicate intent and make policies testable.  
- **Security posture** for tokens avoids common pitfalls and meets Canadian enterprise expectations.

---

### Appendix A – Entity Summaries (for quick onboarding)
- **Goal:** Aggregate root for SMART goals; owner‑scoped; status transitions controlled.  
- **ProgressLog:** Progress deltas linked to a Goal; designed for append‑only audit.  
- **RefreshToken:** Hash‑only token storage with revocation/rotation; `IsActive` encapsulates validity.  
- **Reminder:** Cron‑based schedule holder; infra computes `NextRun`; channel‑aware.

---

**Authoring note:** This file is intended to live at `GoalFlow.Domain/teaching-point.md` (or `README.md`) and be read independently from API/Infrastructure docs.

