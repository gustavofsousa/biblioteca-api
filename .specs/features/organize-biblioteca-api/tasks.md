# Tasks — Organize biblioteca-api

One atomic commit per task. Gate must pass before a task is done.

## Phase 1 — Domain & data layer
- [ ] **T1** Add `BibliotecaAPI.Models` namespace to `Livro`/`Usuario`; make reference props
  non-nullable-safe (`required`). Verify `BibliotecaContext` resolves. → R2. Gate: `dotnet build`
  errors on models cleared.

## Phase 2 — API layer + tests
- [ ] **T2** Add `Dtos/` (request/response records) + `Endpoints/LivrosEndpoints.cs` (EF CRUD,
  validation, route group). Rewrite `Program.cs`: drop raw Npgsql, remove hardcoded connection,
  map the route group. → R3, R5, R6. Gate: build clean.
- [ ] **T3** Add `BibliotecaAPI.Tests` xUnit project (WebApplicationFactory + EF InMemory) covering
  AC2–AC6. → gate for R6. Gate: `dotnet test` green.

## Phase 3 — Config & docs
- [ ] **T4** Remove DB password from `appsettings.json`/`appsettings.Development.json`; keep a
  no-password placeholder; wire user-secrets/env-var reading. → R4. Gate: grep finds no password.
- [ ] **T5** `dotnet ef migrations add InitialCreate`. → R7. Gate: `Migrations/` present, build clean.
- [ ] **T6** Rewrite README (purpose, setup, run, endpoints) + fix `.http`. → R8.

## Closing
- [ ] **Verifier** (author ≠ verifier) → `validation.md`.
