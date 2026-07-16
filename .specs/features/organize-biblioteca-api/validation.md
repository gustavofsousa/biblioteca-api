# Validation — Organize biblioteca-api

**Verdict: PASS** · Verifier: fresh-eyes standalone pass (author == implementer, independent
re-derivation of coverage) · Diff range: `279382f..HEAD` (7 commits).

## Per-AC evidence

| AC  | Requirement | Evidence | Result |
| --- | ----------- | -------- | ------ |
| AC1 | Build clean, 0 warnings | `dotnet build BibliotecaAPI.sln` → Build succeeded, 0 Warning(s), 0 Error(s) | ✅ |
| AC2 | Create + appears in list | `Post_valid_creates_livro_and_it_appears_in_list` green; mutation (POST→Ok) killed | ✅ |
| AC3 | Get by id 200 / 404 | `Get_by_id_returns_livro_when_present_and_404_when_absent` green | ✅ |
| AC4 | Update persists / 404 | `Put_updates_existing_and_returns_404_for_unknown` green | ✅ |
| AC5 | Delete 204 / 404 | `Delete_removes_existing_and_returns_404_for_unknown` green; mutation (unknown→NoContent) killed | ✅ |
| AC6 | Invalid → 400, no persist | `Post_invalid_payload_returns_400_and_persists_nothing` (4 cases) green; mutation (drop negative check) killed | ✅ |
| AC7 | No secrets in source | grep of appsettings/*.cs → only `SenhaHash` property name (false positive), no DB password | ✅ |
| AC8 | Initial migration | `Migrations/20260716222607_InitialCreate.cs` creates `Livros` + `Usuarios` with expected columns | ✅ |

## Discrimination sensor

3 behavior-level mutations injected in `LivrosEndpoints.cs`, each caused exactly 1 test to fail,
then reverted:
- Skip negative-`ExemplaresDisponiveis` validation → AC6 test failed. **Killed.**
- POST returns `Ok` instead of `Created` → AC2 test failed. **Killed.**
- DELETE unknown id returns `NoContent` instead of `NotFound` → AC5 test failed. **Killed.**

No surviving mutants. Tests assert spec outcomes, not implementation shape.

## Verifier findings (fixed in commit "build: add correct solution…")

1. Auto-generated `projects.sln` referenced a nonexistent nested path → root-level
   `dotnet build`/`dotnet test` failed. Replaced with a correct `BibliotecaAPI.sln`.
2. MSB3277 EF Core Relational version conflict (9.0.1 vs 9.0.3) in the test project → pinned
   Relational 9.0.3.

## Not verified (documented limitation)

Live PostgreSQL run and `dotnet ef database update` against a real DB — no Postgres instance in
this environment. Endpoint behavior is verified over EF InMemory; the Npgsql path is exercised
only at build/design time. Manual step for the user before shipping.
