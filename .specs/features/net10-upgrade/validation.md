# Validation — net10-upgrade (0.0)

**Verdict: PASS** · Verifier: implementer, independent re-run of build/test evidence on the
`phase-0-foundation` branch.

## Scope

Preflight upgrade net8.0 → net10.0 before any Phase 1 work (AD-015). Retarget app + tests, move
EF Core / Npgsql / OpenAPI to the v10 line, keep the build clean and all tests green.

## Per-AC evidence

| AC  | Requirement | Evidence | Result |
| --- | ----------- | -------- | ------ |
| AC1 | App + tests target `net10.0` | `TargetFramework` = `net10.0` in both csproj; test run reports `.NETCoreApp,Version=v10.0` | ✅ |
| AC2 | EF Core / Npgsql / OpenAPI on v10 | EF Core 10.0.10, Npgsql 10.0.3, Npgsql.EFCore.PostgreSQL 10.0.3, AspNetCore.OpenApi 10.0.10, Swashbuckle 10.2.3; `dotnet-ef` tool 10.0.10 | ✅ |
| AC3 | Build clean, 0 warnings | `dotnet build -c Release -warnaserror` → Build succeeded, 0 Warning(s), 0 Error(s) | ✅ |
| AC4 | Tests green | `dotnet test` → Passed 8, Failed 0 on net10.0 | ✅ |
| AC5 | Reproducible SDK | `global.json` pins 10.0.100 rollForward `latestFeature`; `dotnet --version` → 10.0.110 | ✅ |
| AC6 | Migrations still work under EF10 | `dotnet ef database update` applied `InitialCreate` writing `ProductVersion '10.0.10'` (see postgres-smoke) | ✅ |

## Notable change

`.WithOpenApi()` is deprecated in .NET 10 (ASPDEPR002) and produced 5 warnings after the bump.
Removed the calls in `LivrosEndpoints.cs`; Swagger still enumerates endpoints via ApiExplorer
(`WithName`/`WithTags` retained), restoring the 0-warning baseline.

## Environment note

The prior "SDK not installed" block (STATE handoff / roadmap 0.0) is obsolete — 10.0.110 is now
installed alongside 8.0.128. Bump verified end to end.
