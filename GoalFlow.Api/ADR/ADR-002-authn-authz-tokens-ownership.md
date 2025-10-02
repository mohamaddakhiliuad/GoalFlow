# ADR-002: AuthN/AuthZ — JWT, Refresh Rotation, Ownership Policy

- **Status:** Accepted
- **Date:** 2025-09-30
- **Layer:** API

## Context
We require secure authentication with good developer ergonomics and clear data isolation across users (per-resource ownership). Alternatives considered:
- Long-lived access tokens (worse security).
- Storing raw refresh tokens (DB compromise risk).
- No ownership enforcement (multi-tenancy risk).

## Decision
- **JWT Bearer** for access tokens (HMAC-SHA256), **15-minute** lifetime.
- **Refresh tokens**: 64-byte random → return **raw** to client, store **SHA-256 hash** only.
- **Rotation** on refresh: revoke the old token; issue a new access/refresh pair.
- **Ownership policy**: `MustOwnGoal` requirement + `MustOwnGoalHandler`; apply policy `"OwnerGoal"` on `/api/goals/{id}` routes.

## Consequences
**Positive**
- Short-lived access limits blast radius.
- Hash-at-rest reduces damage if DB is leaked.
- Clear, testable ownership boundary at the API.

**Trade-offs**
- Client must store refresh tokens securely (HttpOnly/Secure cookie or secure storage).
- Slightly more moving parts for rotation/revocation bookkeeping.

## References
- `TokenService` (issue/validate/rotate)
- `MustOwnGoal`, `MustOwnGoalHandler`
- `.RequireAuthorization("OwnerGoal")` usage in `Program.cs`
