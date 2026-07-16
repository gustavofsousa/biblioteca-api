# Project State — biblioteca-api

## Decisions (Architecture Decisions)

- **AD-001 — Traction mode: MVP.** No confirmed traction. Ship the smallest correct slice; no
  over-spec. Livro CRUD is in scope; Usuario endpoints are NOT (model kept clean only).
- **AD-002 — Data access consolidated on EF Core.** The raw `Npgsql` connection in the `/livros`
  endpoint is removed. All persistence goes through `BibliotecaContext`. Provider: Npgsql
  (PostgreSQL) in app, EF InMemory in tests.
- **AD-003 — No secrets in source.** Connection string password removed from `appsettings.json`
  and code. Read from configuration: `dotnet user-secrets` in Development, environment variable
  (`ConnectionStrings__DefaultConnection`) in Production.
- **AD-004 — Minimal-API layering (KISS).** Endpoints extracted from `Program.cs` into a route-group
  module (`Endpoints/LivrosEndpoints.cs`) that injects `BibliotecaContext` directly. No repository /
  service layer for MVP — added only if it later earns its place. DTOs isolate the wire contract
  from entities.
- **AD-005 — EF initial migration committed.** Schema created via `dotnet ef migrations add` (no
  live DB required to generate).

## Handoff (in-flight)

Feature `organize-biblioteca-api` — executing. See `.specs/features/organize-biblioteca-api/`.
