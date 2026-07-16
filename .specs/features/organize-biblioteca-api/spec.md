# Spec — Organize biblioteca-api

**Mode:** MVP · **Scope:** Medium (structure + fix existing; no new features beyond hinted Livro CRUD)

## Problem

The project does not build (models lack the `BibliotecaAPI.Models` namespace that
`BibliotecaContext` imports — CS0234). Even ignoring that, `Program.cs` opens a raw `Npgsql`
connection with a hardcoded password inside the `/livros` endpoint, bypassing the configured EF
Core `DbContext`; the DB password is also versioned in `appsettings.json`. There are no layers,
no DTOs, no migration, and the README/`.http` are template leftovers.

## Goal

A buildable, cleanly layered minimal API where persistence goes through EF Core, no secrets live
in source, and Livro has a working CRUD — the smallest correct slice.

## Requirements (traceable)

- **R1 — Builds clean.** `dotnet build` succeeds with **zero errors and zero warnings**.
- **R2 — Models correct.** `Livro` and `Usuario` live in `BibliotecaAPI.Models`; reference-type
  properties are non-nullable-safe (`required`/initialized) so no CS86xx nullable warnings.
- **R3 — EF-only data access.** No `NpgsqlConnection`/`NpgsqlCommand` in application code; all
  persistence goes through `BibliotecaContext`.
- **R4 — No secrets in source.** No DB password in `appsettings.json` or code. Connection string
  read from configuration; dev via user-secrets, prod via env var. A placeholder (no password)
  documents the key.
- **R5 — Layered API.** Livros endpoints live in a dedicated route-group module, not inline in
  `Program.cs`. DTOs (not entities) cross the HTTP boundary.
- **R6 — Livro CRUD.** `GET /livros`, `GET /livros/{id}`, `POST /livros`, `PUT /livros/{id}`,
  `DELETE /livros/{id}` work through EF Core with basic input validation.
- **R7 — Initial migration.** An EF migration for the current schema (Livros, Usuarios) exists.
- **R8 — Docs current.** README states purpose, setup (user-secrets), run, and endpoints; `.http`
  exercises the real Livro endpoints (no `weatherforecast`).

## Acceptance Criteria (Given/When/Then)

- **AC1 (R1,R2,R3)** — Given the repo, When `dotnet build` runs, Then it exits 0 with no warnings.
- **AC2 (R6)** — Given an empty DB, When `POST /livros` with a valid body, Then 201 + the created
  livro (id assigned); When `GET /livros`, Then it appears in the list.
- **AC3 (R6)** — Given an existing livro, When `GET /livros/{id}`, Then 200 + that livro; for an
  unknown id, Then 404.
- **AC4 (R6)** — Given an existing livro, When `PUT /livros/{id}` with a valid body, Then 200/204
  and the change persists; unknown id → 404.
- **AC5 (R6)** — Given an existing livro, When `DELETE /livros/{id}`, Then 204 and it is gone;
  unknown id → 404.
- **AC6 (R6)** — Given `POST`/`PUT` with an invalid body (blank Titulo/Autor or negative
  Exemplares), Then 400 and nothing persists.
- **AC7 (R4)** — Given the source tree, When grepping `appsettings*.json` and `*.cs`, Then no DB
  password string is present.
- **AC8 (R7)** — Given the repo, When listing `Migrations/`, Then an initial migration exists that
  creates Livros and Usuarios.

## Out of scope

Usuario endpoints/auth, notifications (README tagline), loan/lending flow, pagination, scalable
architecture, CI. Recorded for later; not built now (MVP).

## Verification gate

`dotnet build` clean (AC1, AC7 via grep) + xUnit endpoint tests over EF InMemory covering
AC2–AC6. Live Postgres verification is a documented manual step (no DB in this environment).
