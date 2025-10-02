# ADR-001: Minimal APIs vs Controllers

- **Status:** Accepted
- **Date:** 2025-09-30
- **Layer:** API

## Context
We need a concise, interview-ready HTTP surface that makes cross-cutting concerns explicit and keeps business logic in the Application layer (MediatR). We compared MVC Controllers (attributes/filters) with Minimal APIs (lean bootstrapping, explicit pipeline).

## Decision
- Use **ASP.NET Core Minimal APIs** for the API layer.
- Keep endpoints **thin**; delegate to **MediatR** commands/queries.
- Configure cross-cutting concerns (JWT, CORS, rate limiting, ProblemDetails, Swagger, Serilog) **explicitly** in `Program.cs`.

## Consequences
**Positive**
- Faster startup and less ceremony; clear bootstrapping in a single place.
- Great for interviews—decisions are visible and easy to follow.
- Reduced “magic”; easier to reason about the middleware pipeline.

**Trade-offs**
- Lambda endpoints don’t support XML doc per action (we use file/inline comments + Swagger).
- Large route graphs may benefit from future endpoint grouping/modules.

## References
- `GoalFlow.Api/Program.cs` (bootstrapping)
- Application handlers (`GoalFlow.Application`)
