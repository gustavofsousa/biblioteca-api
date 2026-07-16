# Roadmap — biblioteca-api

> Slices the [vision](vision.md) into phases. Each **feature** maps to a `.specs/features/<slug>/`
> built through the spec-driven pipeline (Specify → Design → Tasks → Execute + Verifier). Phases are
> ordered by dependency; features inside a phase can often run in parallel. Nothing here is built
> until its feature's `validation.md` says PASS.

**Legend:** ✅ done · 🔜 next · ⬜ planned · 🧩 backlog

## Status snapshot

| Phase | Theme | State |
| --- | --- | --- |
| 0 | Foundation & catalog (existing) | partly ✅ |
| 1 | Tenancy & identity | 🔜 |
| 2 | Catalog depth (Exemplar, Categoria, search) | ⬜ |
| 3 | Lending core (Emprestimo, Prazo, Renovação) | ⬜ |
| 4 | Reputation & profile levels | ⬜ |
| 5 | Notifications (email reminders) | ⬜ |
| 6 | Operation UI (desk + reader portal + admin) | ⬜ |
| 7 | Advanced & backlog | 🧩 |

---

## Phase 0 — Foundation & catalog *(mostly done)*

| # | Feature | Slug | State |
| --- | --- | --- | --- |
| **0.0** | **Upgrade .NET 8 → .NET 10** (preflight, before any new work) | `net10-upgrade` | 🔜 **blocked: SDK not installed** |
| 0.1 | Buildable, layered Minimal API + Livro CRUD | `organize-biblioteca-api` | ✅ |
| 0.2 | CI pipeline (build + test on push) | `ci-pipeline` | ⬜ |
| 0.3 | Live-Postgres smoke verification | `postgres-smoke` | ⬜ |

> **0.0 detail:** bump `TargetFramework` to `net10.0`, EF Core / Npgsql / OpenAPI packages to their
> v10 line, retarget the test project, `dotnet build` + `dotnet test` must stay green. **Requires the
> .NET 10 SDK on the machine** — currently only 8.0.x is installed here (see handoff note in `STATE.md`).

## Phase 1 — Tenancy & identity *(next — unblocks everything)*

Multi-tenancy and auth are foundational; retrofitting later is expensive (macro AD).

| # | Feature | Slug | Notes |
| --- | --- | --- | --- |
| 1.1 | **Biblioteca (tenant) model + isolation** | `tenancy-foundation` | `BibliotecaId` on every entity; global query filter resolved from the authenticated user. **Macro AD: isolation strategy.** |
| 1.2 | **Auth (JWT) + roles** | `auth-jwt-roles` | Register/login; `Perfil` = Admin / Atendente / Leitor; password hashing (Usuario.SenhaHash already exists). |
| 1.3 | **Usuario endpoints + profile** | `usuario-endpoints` | CRUD for staff/readers within a tenant; self-profile read. **Email + Celular required** on every Usuario. |

**Milestone M1:** a library can be created, an admin can log in, and add staff + readers, all isolated.

## Phase 2 — Catalog depth

| # | Feature | Slug | Notes |
| --- | --- | --- | --- |
| 2.1 | **Exemplar (physical copies)** | `exemplar-copies` | Split Livro (title) from Exemplar (copy). Migrate `ExemplaresDisponiveis` → Exemplar rows with status. |
| 2.2 | **Categoria as entity** | `categoria-entity` | Normalize the free string into a managed list (hierarchy/tags = design decision). |
| 2.3 | **Catalog search, filter, pagination** | `catalog-search` | By título/autor/ISBN/categoria; paged responses (a known MVP follow-up). |

**Milestone M2:** the catalog matches the physical shelf and is searchable.

## Phase 3 — Lending core

| # | Feature | Slug | Notes |
| --- | --- | --- | --- |
| 3.1 | **Emprestimo checkout / check-in** | `emprestimo-core` | Checkout against an Available Exemplar; return frees it; enforces max-concurrent from Nivel. |
| 3.2 | **Prazo + Renovação** | `prazo-renovacao` | Prazo = checkout + Nivel duration; renew within Nivel limits and if no Reserva waits. |
| 3.3 | **Overdue detection job** | `overdue-job` | Background worker flips OnLoan→Overdue when Prazo passes. |

**Milestone M3:** the desk can lend and receive books with correct due dates. *(Depends on Phase 4
for privilege numbers — ship with defaults, wire the real Nivel rules when 4 lands, or sequence 4
before 3.2.)*

## Phase 4 — Reputation & profile levels *(the differentiator)*

| # | Feature | Slug | Notes |
| --- | --- | --- | --- |
| 4.1 | **ReputationScore + late penalty** | `reputation-score` | Baseline **30**; late Devolução: `−PENALTY_PER_DAY×dias` (**no floor, may go negative**); on-time: `+1` cap 100. |
| 4.2 | **RegistroDeAtraso (history)** | `late-history` | Immutable per-incident record on the reader's profile. |
| 4.3 | **NivelDeLeitor → privileges** | `profile-levels` | Bands (Ouro/Prata/Bronze/Restrito/Atenção) drive max loans, duration, renewals. Per-tenant configurable. |
| 4.4 | **AjusteManual (attendant override)** | `ajuste-manual` | Attendant grants a specific Leitor extra loan slots and/or extra prazo days; auditable. |
| 4.5 | **ListaDeAtenção (score < 0)** | `lista-atencao` | Readers below 0 surface to the attendant to contact and handle the case. |

**Milestone M4:** returning late costs score, history is visible, privileges adapt automatically, and
attendants can both override for good readers and follow up on readers in the red.

## Phase 5 — Notifications (Email + WhatsApp)

Built around a **CanalDeNotificacao** abstraction so channels are pluggable (extensibility principle).

| # | Feature | Slug | Notes |
| --- | --- | --- | --- |
| 5.1 | **Notification channel abstraction** | `notification-channels` | One interface, per-channel delivery log + retries. Ships Email + WhatsApp; SMS/push later without touching reminder logic. |
| 5.2 | **Email channel + per-tenant sender** | `email-channel` | SMTP/provider config per Biblioteca. |
| 5.3 | **WhatsApp channel (external API)** | `whatsapp-channel` | Integrate a WhatsApp API (Cloud API or BSP — provider is an open decision); per-tenant credentials; opt-in/consent. |
| 5.4 | **Reminder scheduler** | `reminder-scheduler` | Before-due (T-3, T-1), on-due, and daily-overdue reminders; idempotent (no double-send) across channels. |
| 5.5 | **Templates** | `notification-templates` | Per-tenant customizable subject/body per channel. |

**Milestone M5:** readers get timely Email + WhatsApp reminders; overdue rate measurably drops.

## Phase 6 — Operation UI

API + UI was a confirmed requirement. Surface, not just data.

| # | Feature | Slug | Notes |
| --- | --- | --- | --- |
| 6.1 | **Attendant desk UI** | `ui-desk` | Fast checkout/check-in, member + copy lookup, overdue list, **ListaDeAtenção** with one-click contact, and **AjusteManual** controls (Goal G1: <30s). |
| 6.2 | **Reader portal** | `ui-reader-portal` | My loans, due dates, renew, my score + history, browse catalog. |
| 6.3 | **Admin console** | `ui-admin` | Manage catalog, categories, staff, score bands & loan rules, reports. |

**Milestone M6:** a non-technical librarian runs the whole desk from the browser.

## Phase 7 — Advanced & backlog 🧩

Not scheduled; pulled forward only with demand.

- **Reserva / holds queue** (`reserva-holds`) — waitlist + notify next in line on return.
- **ISBN metadata import** (`isbn-import`) — autofill Livro from an external catalog lookup.
- **Barcode/QR scan** at the desk (`barcode-scan`) — optional hardware acceleration.
- **Reports & analytics** (`reports-analytics`) — overdue trends, popular titles, active readers.
- **Book condition tracking** on Exemplar (`exemplar-condition`).
- **Reader self-check-out kiosk** (`kiosk-mode`).
- **Audit log** of privileged actions (`audit-log`).

## Cross-cutting concerns (every phase)

- **Extensibility (design principle)** — new work attaches at defined seams: pluggable
  `CanalDeNotificacao`, per-tenant scoring/band configuration, per-reader `AjusteManual`. Prefer
  adding a channel/rule/config over editing core flows. Keep the modular, feature-oriented layout.
- **Security & tenant authz** — no cross-`BibliotecaId` access; authz tests are a merge gate.
- **LGPD / data privacy** — reader email + borrowing history are personal data: consent, export,
  deletion. Address alongside Phase 1 identity and Phase 4 history.
- **Observability** — structured logs, health checks, and metrics for overdue/reminders.
- **Testing** — endpoint tests over EF InMemory (established pattern) + acceptance-criteria-derived
  tests per the spec-driven Verifier.

## Suggested sequencing

```
0.2 CI ─┐
        ├─▶ 1 Tenancy+Identity ─▶ 2 Catalog ─▶ 3 Lending ─▶ 4 Reputation ─▶ 5 Notifications ─▶ 6 UI
0.3 DB ─┘                                          └── (4 can precede 3.2 to supply real Nivel rules)
```

Recommendation: do **0.2 CI** first (cheap, protects everything after), then Phase 1. Consider
landing **Phase 4 before 3.2** so lending uses real privilege numbers instead of placeholders.
