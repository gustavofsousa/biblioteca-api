# biblioteca-api

A minimal ASP.NET Core (.NET 10) API for lending books. This repository currently ships the
**Livro** (book) catalog with full CRUD, backed by PostgreSQL through EF Core.

> **Product direction:** a multi-library (SaaS) system for physical libraries — free lending with
> due dates, a **reputation score** (no payments) that drives per-reader borrowing privileges, email
> reminders, and an operator UI. See [`docs/product/`](docs/product/): [vision](docs/product/vision.md),
> [domain model](docs/product/domain-model.md), [roadmap](docs/product/roadmap.md).
>
> This repository currently ships **Phase 0** (Livro catalog). Later phases (tenancy, auth, lending,
> reputation, notifications, UI) are planned in the roadmap and not built yet.

## Stack

- ASP.NET Core 10 Minimal API (SDK pinned via `global.json`)
- Entity Framework Core 10 + Npgsql 10 (PostgreSQL)
- Swagger / OpenAPI (Development only)
- xUnit endpoint tests over EF InMemory
- GitHub Actions CI (build + test on push) — [.github/workflows/ci.yml](.github/workflows/ci.yml)

## Project layout

```
BibliotecaAPI/
├── Program.cs               # Composition root: DI, middleware, route wiring
├── Endpoints/               # Route-group modules (LivrosEndpoints)
├── Dtos/                    # Request/response contracts (never expose entities)
├── Models/                  # EF entities (Livro, Usuario)
├── Data/                    # BibliotecaContext (DbContext)
└── Migrations/              # EF migrations
BibliotecaAPI.Tests/         # WebApplicationFactory + EF InMemory endpoint tests
```

## Configuration (no secrets in source)

The DB connection string lives in configuration under `ConnectionStrings:DefaultConnection`.
`appsettings.json` holds only a password-less placeholder. Supply the password out of band:

**Development** — user-secrets (stored outside the repo):

```bash
cd BibliotecaAPI
dotnet user-secrets set "ConnectionStrings:DefaultConnection" \
  "Host=localhost;Database=biblioteca;Username=biblioteca_user;Password=YOUR_PASSWORD"
```

**Production** — environment variable:

```bash
export ConnectionStrings__DefaultConnection="Host=...;Database=...;Username=...;Password=..."
```

## Database

Apply the schema with the committed EF migration:

```bash
dotnet tool restore                 # restores the pinned dotnet-ef
dotnet ef database update --project BibliotecaAPI
```

## Run

```bash
dotnet run --project BibliotecaAPI
# Swagger UI: http://localhost:5012/swagger
```

## Endpoints

| Method | Route           | Description            | Success |
| ------ | --------------- | ---------------------- | ------- |
| GET    | `/livros`       | List livros            | 200     |
| GET    | `/livros/{id}`  | Get a livro by id      | 200 / 404 |
| POST   | `/livros`       | Create a livro         | 201 / 400 |
| PUT    | `/livros/{id}`  | Update a livro         | 200 / 400 / 404 |
| DELETE | `/livros/{id}`  | Delete a livro         | 204 / 404 |

`LivroRequest`: `{ "titulo": string, "autor": string, "categoria": string, "exemplaresDisponiveis": int >= 0 }`

See [BibliotecaAPI/BibliotecaAPI.http](BibliotecaAPI/BibliotecaAPI.http) for ready-to-run requests.

## Tests

```bash
dotnet test
```

Endpoint tests boot the real API with the Npgsql provider swapped for an isolated in-memory
database, so no PostgreSQL instance is required to run them.

## Live-Postgres smoke check

The unit tests use EF InMemory. To verify the app against a **real PostgreSQL** end to end
(migrations apply, HTTP round-trip persists), run the smoke script (requires Docker):

```bash
scripts/postgres-smoke.sh
```

It spins up a throwaway Postgres container, applies the EF migrations, boots the API against it,
POSTs then GETs a Livro, confirms the row directly in Postgres, and tears everything down.
Prints `=== POSTGRES SMOKE: PASS ===` on success.
