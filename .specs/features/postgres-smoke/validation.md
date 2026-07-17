# Validation — postgres-smoke (0.3)

**Verdict: PASS** · Verifier: implementer, script executed twice against a live container.

## Scope

Close the gap left by `organize-biblioteca-api` (unit tests run on EF InMemory only): prove the
app works against a **real PostgreSQL** — migrations apply and an HTTP write actually persists
(AD-018). Delivered as an on-demand script, not a CI gate.

## Artifact

[scripts/postgres-smoke.sh](../../../scripts/postgres-smoke.sh) — starts `postgres:16-alpine`,
applies EF migrations, boots the API against it, POSTs then GETs a Livro, asserts the row directly
via `psql`, and tears everything down (trap on exit). Prints `=== POSTGRES SMOKE: PASS ===`.

## Per-AC evidence (observed run)

| AC  | Requirement | Evidence | Result |
| --- | ----------- | -------- | ------ |
| AC1 | Migrations apply to real Postgres | EF created `Livros` + `Usuarios` + `__EFMigrationsHistory`; row `('20260716222607_InitialCreate','10.0.10')` | ✅ |
| AC2 | API serves against the live DB | `GET /livros` returned 200 after boot with `ConnectionStrings__DefaultConnection` → container | ✅ |
| AC3 | Write round-trips over HTTP | `POST /livros` → 201 id=1; `GET /livros/1` → titulo "O Cortiço" matches | ✅ |
| AC4 | Data physically persisted in PG | `SELECT COUNT(*) FROM "Livros" WHERE "Id"=1` via `psql` → `1` | ✅ |
| AC5 | Repeatable + self-cleaning | Ran twice, both PASS; container removed on exit | ✅ |

## Note

Requires Docker + the .NET 10 SDK + the `dotnet-ef` local tool. Runs over HTTP (Production env,
single http URL) so `UseHttpsRedirection` passes through without an HTTPS port configured.
