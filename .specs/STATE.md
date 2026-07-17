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

- **AD-006 — Product-mode pivot (supersedes AD-001's MVP posture).** Direction confirmed: this is a
  full product, not a throwaway MVP. Spec depth, testing, and docs are warranted. Product-level docs
  live in `docs/product/` (vision, domain-model, roadmap); features flow through the spec-driven
  pipeline. Livro CRUD (AD-001..AD-005) remains the built foundation.
- **AD-007 — Multi-tenant SaaS.** Serves many libraries on one deployment; each **Biblioteca** is a
  tenant with isolated data (`BibliotecaId` on every entity). Isolation strategy (shared-schema
  global filter vs. schema-per-tenant) is an open macro decision, settled in the `tenancy-foundation`
  feature's design phase. Default lean: shared schema + global query filter.
- **AD-008 — No payments; reputation replaces money.** "Aluguel" = free lending with a `Prazo`.
  Late returns decrement a per-Leitor **ReputationScore** (`−PENALTY_PER_DAY×dias`, floor 0) and
  create an immutable **RegistroDeAtraso**; the score drives **NivelDeLeitor** bands that grant
  borrowing privileges (max concurrent, duration, renewals). No gateway/fines/invoices. Score params
  and bands are per-tenant configurable. See `docs/product/domain-model.md`.
- **AD-009 — Delivery surface: API + operation UI.** Roadmap includes an attendant desk UI, a reader
  portal, and an admin console (Phase 6), not just the REST API.

- **AD-010 — Score baseline 30, may go negative.** New Leitor starts at `SCORE_BASELINE = 30` (trust
  earned, not granted). Late penalty has **no floor**; score below 0 is the trigger for the
  ListaDeAtenção. Bands rebased: Ouro 80–100, Prata 50–79, Bronze 20–49, Restrito 0–19, Atenção <0.
  Supersedes the earlier baseline-100/floor-0 sketch.
- **AD-011 — AjusteManual (attendant override).** An `Atendente` may grant a specific Leitor extra
  loan slots (`bonusMaxEmprestimos`) and/or extra prazo days (`bonusDiasPrazo`) on top of their Nivel,
  auditable. Effective limits = Nivel + AjusteManual. This is the deliberate flexibility escape hatch.
- **AD-012 — ListaDeAtenção.** Leitores with score < 0 surface to the Atendente for human contact
  (email/WhatsApp/phone) — a workflow, not an automatic permanent ban.
- **AD-013 — Contact channels: Email + Celular required; notifications multi-channel.** Every Usuario
  has both. Notifications go over a pluggable `CanalDeNotificacao` (Email + WhatsApp now, SMS/push
  later). WhatsApp uses an external API (Cloud API vs. BSP = open decision).
- **AD-014 — Extensible-by-default architecture.** Not a closed product. Seams: pluggable notification
  channels, per-tenant scoring/band config, per-reader AjusteManual. Add at seams over editing core.
- **AD-015 — Target framework .NET 10 (preflight `net10-upgrade`). DONE.** App + tests retargeted to
  net10.0; EF Core 10.0.10, Npgsql 10.0.3, Npgsql.EFCore.PostgreSQL 10.0.3, AspNetCore.OpenApi 10.0.10,
  Swashbuckle 10.2.3, `dotnet-ef` tool 10.0.10. SDK pinned via `global.json` (10.0.100 rollForward
  latestFeature → resolves 10.0.110). Deprecated `WithOpenApi()` calls removed (ASPDEPR002) to hold
  0 warnings. Build clean (`-warnaserror` in CI), 8/8 tests green on net10.0. **Block cleared:** the
  .NET 10 SDK (10.0.110) is now installed alongside 8.0.128; the earlier "SDK not installed" note is
  obsolete.
- **AD-017 — CI on GitHub Actions (`ci-pipeline`).** `.github/workflows/ci.yml` runs restore → build
  (Release, `-warnaserror`) → test on every push (any branch) and PR to `main`; SDK from `global.json`
  via `setup-dotnet`. Warnings fail the build to protect the 0-warning bar. Test results uploaded as a
  trx artifact.
- **AD-018 — Live-Postgres smoke as a script, not a CI gate (`postgres-smoke`).** `scripts/postgres-smoke.sh`
  (Docker Postgres 16 → migrations → API boot → HTTP round-trip → direct psql assertion → teardown) is
  the live-DB check, kept out of CI to avoid a Docker-in-CI dependency for the MVP. CI stays fast on
  InMemory tests; the smoke script is the on-demand real-DB verification. Verified PASS locally.
- **AD-016 — Brazil-first.** Product targets libraries across Brazil: PT-BR UI, LGPD, WhatsApp as a
  first-class channel.

## Handoff (in-flight)

**Phase 0 — COMPLETE ✅** (branch `phase-0-foundation`). All four features done:

- `organize-biblioteca-api` (0.1) — DONE/PASS (validation.md). 8 ACs, 0 warnings, 8 tests green,
  3/3 mutants killed.
- `net10-upgrade` (0.0) — DONE/PASS. See AD-015 + `.specs/features/net10-upgrade/validation.md`.
- `ci-pipeline` (0.2) — DONE. See AD-017 + `.specs/features/ci-pipeline/validation.md`. (First real
  GitHub Actions run happens on push — CI steps validated locally.)
- `postgres-smoke` (0.3) — DONE/PASS. See AD-018 + `.specs/features/postgres-smoke/validation.md`.

**Next:** Phase 1 — Tenancy & identity (`tenancy-foundation` first; macro AD on isolation strategy,
default lean = shared schema + global query filter per AD-007).

Open follow-ups still out of scope: Usuario endpoints/auth, lending flow, notifications, pagination.
