# ADR-004: Rate Limiting & CORS

- **Status:** Accepted
- **Date:** 2025-09-30
- **Layer:** API

## Context
We need basic abuse protection and a safe dev experience for browser clients and SignalR.

## Decision
- **Rate limiting**:
  - Policy **`fixed`**: 100 req/min/IP for general endpoints.
  - Policy **`login`**: 5 req/min/IP for authentication endpoints.
  - No queuing; return **429** on rejection.
- **CORS**:
  - Policy **`client`** allows local dev origins and credentials to support SignalR.
  - Origins are explicit; production to use env-based configuration.

## Consequences
**Positive**
- Mitigates brute-force and accidental client hot-loops.
- Predictable throttling behaviour for clients (429).

**Trade-offs**
- Limits must be tuned per environment and usage patterns.
- CORS origins require coordination with FE deploys.

## References
- `AddRateLimiter(...)`, `.RequireRateLimiting("fixed"|"login")` in `Program.cs`
- `AddCors("client")` policy in `Program.cs`
