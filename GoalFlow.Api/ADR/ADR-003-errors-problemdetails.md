# ADR-003: Errors via Hellang ProblemDetails

- **Status:** Accepted
- **Date:** 2025-09-30
- **Layer:** API

## Context
Clients need consistent error shapes and predictable mappings from exceptions to HTTP status codes.

## Decision
- Use **Hellang.Middleware.ProblemDetails** to map common cases:
  - Validation (FluentValidation) → **400 Bad Request**
  - Unauthorized → **401 Unauthorized**
  - Not Found → **404 Not Found**
  - Fallback → **500 Internal Server Error**
- Include readable `Title` and `Detail`.

## Consequences
**Positive**
- Uniform, debuggable error payloads.
- Easier client integration and automated testing.

**Trade-offs**
- Mappings must stay aligned with domain/application exceptions.

## References
- `AddProblemDetails(...)` configuration in `Program.cs`
