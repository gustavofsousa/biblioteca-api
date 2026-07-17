# Validation — ci-pipeline (0.2)

**Verdict: PASS (steps validated locally; first GitHub run on push)** · Verifier: implementer.

## Scope

Continuous integration that builds and tests the solution on every push, protecting all later
work (AD-017). Chosen runner: GitHub Actions (repo remote is GitHub).

## Artifact

[.github/workflows/ci.yml](../../../.github/workflows/ci.yml) — job `build-test` on `ubuntu-latest`:
checkout → `setup-dotnet` (SDK from `global.json`) → restore → `build -c Release -warnaserror` →
`test --no-build -c Release` (trx) → upload trx artifact. Triggers: push to any branch, PR to `main`.
Concurrency group cancels superseded runs per ref.

## Evidence (exact CI commands run locally)

| Step | Command | Result |
| ---- | ------- | ------ |
| Restore | `dotnet restore BibliotecaAPI.sln` | ✅ restored |
| Build | `dotnet build --no-restore -c Release -warnaserror` | ✅ 0 Warning(s), 0 Error(s) |
| Test | `dotnet test --no-build -c Release --logger trx` | ✅ Passed 8, Failed 0; trx written |

`-warnaserror` makes any warning fail CI, enforcing the project's 0-warning bar.

## Not verified

The workflow's actual execution on GitHub Actions infrastructure happens on first push — the
step commands are proven locally but the hosted run (and `setup-dotnet` resolving `global.json`
on the runner) will be confirmed by the first green check on the branch/PR.
