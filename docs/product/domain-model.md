# Domain Model & Ubiquitous Language — biblioteca-api

> Companion to [vision.md](vision.md). Domain nouns stay in Portuguese to match the existing code
> (`Livro`, `Usuario`, `BibliotecaContext`); prose is English. This is the shared vocabulary — code,
> docs, issues, and specs must use these exact terms.

## Ubiquitous language (glossary)

| Term | Meaning |
| --- | --- |
| **Biblioteca** | A tenant. One physical library. Root of data isolation; everything below belongs to exactly one Biblioteca. |
| **Usuario** | A person with an account in a Biblioteca. Has a `Perfil` (role) and two contact channels: **Email** and **Celular** (both required). Already exists as a model. |
| **Perfil (Role)** | What a Usuario is allowed to do: `Admin`, `Atendente`, `Leitor`. Distinct from **NivelDeLeitor** below. |
| **Livro** | A catalog title (metadata: título, autor, ISBN, editora, ano). Already exists as a model. |
| **Exemplar** | A physical copy of a Livro sitting on a shelf. Loans happen against an Exemplar, not a Livro. A Livro has 1..N Exemplares. |
| **Categoria** | Classification of a Livro (e.g., "Ficção", "Técnico"). Today a free string on `Livro`; to be normalized into an entity, optionally hierarchical/tags. |
| **Emprestimo** | A lending record: which Exemplar, to which Leitor, when checked out, `Prazo` (due date), returned-at, status. |
| **Prazo** | The due date of an Emprestimo. Derived from checkout date + loan duration granted by the reader's NivelDeLeitor, plus any **AjusteManual** bonus days. |
| **Renovacao** | Extending an Emprestimo's Prazo, allowed a limited number of times based on NivelDeLeitor and only if no reservation is waiting. |
| **Devolucao** | The act/record of returning an Exemplar. On-time or late; late devolução triggers the score penalty. |
| **Reserva** | A hold placed by a Leitor on a Livro whose Exemplares are all out; forms a waiting queue (backlog feature). |
| **ReputationScore** | A signed integer per Leitor. New readers start at **30** (`SCORE_BASELINE`); capped at 100, **may go negative**. Decrements on late Devolução, recovers on on-time returns. Drives NivelDeLeitor. |
| **NivelDeLeitor (Profile level)** | A band derived from ReputationScore (Ouro/Prata/Bronze/Restrito/Atenção) that grants borrowing privileges. |
| **AjusteManual (per-reader override)** | An `Atendente`-set exception on a specific Leitor granting **extra loan slots** (`bonusMaxEmprestimos`) and/or **extra loan days** (`bonusDiasPrazo`) on top of their Nivel. Auditable (who/when/why). This is what keeps the system flexible, not rigid. |
| **RegistroDeAtraso (LateReturnRecord)** | An immutable history entry created on each late Devolução: emprestimo, dias de atraso, pontos perdidos, data. Lives on the reader's profile. |
| **ListaDeAtencao (attention list)** | The queue of Leitores whose ReputationScore fell **below 0**, surfaced to the `Atendente` to reach out (email/WhatsApp/phone) and understand the case. Not an automatic ban — a human-contact workflow. |
| **Notificacao** | A reminder tied to an Emprestimo/Prazo (before-due, due, overdue), delivered over one or more **channels** (Email, WhatsApp) with a per-channel delivery log. |
| **CanalDeNotificacao** | A pluggable delivery channel for a Notificacao. Ships with Email and WhatsApp (external API); new channels (SMS, push) can be added without touching the reminder logic. |

## Core entities & relationships

```
Biblioteca (tenant)
  ├── Usuario (Perfil: Admin | Atendente | Leitor; Email + Celular)
  │     └── (if Leitor) ReputationScore, NivelDeLeitor, AjusteManual?, RegistroDeAtraso[]
  ├── Categoria
  ├── Livro ──< Exemplar
  └── Emprestimo (Exemplar ↔ Leitor, Prazo)  ──< Notificacao ──< (Email | WhatsApp) delivery log
        Reserva (Livro ↔ Leitor)   [backlog]

Every entity carries a BibliotecaId (tenant key). No query crosses BibliotecaId.
```

- A **Livro** belongs to one **Biblioteca**, has one **Categoria**, and has 1..N **Exemplares**.
- An **Emprestimo** links one **Exemplar** to one **Leitor** with a **Prazo**; an Exemplar has at
  most one open Emprestimo at a time.
- A **Leitor** (Usuario with Perfil=Leitor) has a **ReputationScore**, a derived **NivelDeLeitor**,
  and a list of **RegistroDeAtraso**.

## The reputation & profile-level model (the heart)

This is the differentiator, so it is specified concretely. All numbers below are **per-tenant
configurable defaults** — the model is fixed, the parameters are tunable.

### Score

- **New Leitor starts at 30** (`SCORE_BASELINE`) — trust is *earned*, not granted. Capped at **100**;
  **may go negative** (no floor) — negative is the signal that drives the **ListaDeAtenção**.
- **On-time Devolução:** `+1` up to a cap of 100 (rewards recovery without runaway inflation).
- **Late Devolução:** `−PENALTY_PER_DAY × diasDeAtraso`, default `PENALTY_PER_DAY = 3`, **no floor**.
  - `diasDeAtraso = max(0, dataDevolucao − Prazo)` in whole days.
- Every late Devolução also creates one **RegistroDeAtraso** `{ emprestimoId, livroTitulo,
  diasDeAtraso, pontosPerdidos, data }` — immutable, shown in the reader's profile/history.
- **Recovery (decision to confirm):** score only recovers via on-time returns (above). A time-based
  decay of past penalties is an option flagged for the design phase, not assumed.

### Profile levels (NivelDeLeitor) → privileges

Derived from the current score; recomputed whenever the score changes. Bands are per-tenant
configurable; a **new reader (score 30) starts at Bronze** and climbs with good behavior.

| Nivel | Score band | Max concurrent loans | Loan duration | Renewals |
| --- | --- | --- | --- | --- |
| **Ouro** | 80–100 | 5 | 21 days | 2 |
| **Prata** | 50–79 | 3 | 14 days | 1 |
| **Bronze** | 20–49 | 2 | 7 days | 0 |
| **Restrito** | 0–19 | 1 | 7 days | 0 |
| **Atenção** | below 0 | 0 (cannot borrow) | — | — |

Effective limits at checkout = **Nivel privilege + AjusteManual bonus** for that Leitor. Loan rules
are **read at checkout time** — this is how "níveis de perfil", "prazos", and the attendant override
connect.

### AjusteManual — the attendant override (keeps it flexible)

An `Atendente` can grant a specific Leitor an exception without changing the global bands:

- `bonusMaxEmprestimos` — extra concurrent loan slots on top of the Nivel's max.
- `bonusDiasPrazo` — extra days added to the Prazo at checkout.
- Auditable: records who set it, when, and an optional reason. Can be time-bounded or permanent
  (design decision). This satisfies "atendente pode dar a certos perfis mais limite de livros ou mais
  dias de entrega" and is the primary escape hatch that stops the system from being rigid.

### ListaDeAtenção — human follow-up for score < 0

When a Leitor's score drops **below 0**, they surface on the **ListaDeAtenção** shown to the
`Atendente`. This is not an automatic permanent ban — the Atenção Nivel blocks new loans, but the
list exists so a human reaches out (Email / WhatsApp / phone) to understand the case, and can then
apply an AjusteManual, waive, or let recovery proceed.

## Emprestimo lifecycle (state machine)

```
            checkout                       return (on time)
 [Available] ─────────▶ [OnLoan] ───────────────────────────▶ [Returned]
     ▲   │                 │  │                                    │
     │   │ (all copies out) │  │ renew (if allowed, no reserva)    │ score +1
     │   └─────Reserva──────┘  │                                   │
     │                         │ Prazo passes, not returned        │
     │                         ▼                                   │
     │                     [Overdue] ──── return (late) ──────────┘
     └───────────────────────────────  score −(PENALTY_PER_DAY×dias), +RegistroDeAtraso
```

- **checkout:** requires an Available Exemplar; Leitor not in Nivel **Atenção** (score ≥ 0) and under
  their effective max concurrent loans (`Nivel.max + AjusteManual.bonusMaxEmprestimos`). Sets
  Prazo = today + `Nivel.duration + AjusteManual.bonusDiasPrazo`.
- **renew:** allowed only if Nivel.renewals not exhausted and no Reserva is waiting; extends Prazo.
- **overdue:** a background job flips OnLoan→Overdue when Prazo passes; drives overdue reminders.
- **return:** frees the Exemplar (Available); if late, applies penalty + RegistroDeAtraso; if a
  Reserva waits, notifies the next in queue (backlog).

## Multi-tenancy model

- Every tenant-scoped entity carries **`BibliotecaId`**. The chosen isolation strategy
  (shared schema + mandatory tenant filter vs. schema-per-tenant) is a **macro architecture
  decision** recorded in `.specs/STATE.md` and settled in the tenancy feature's design phase.
- Default recommendation: **shared database, shared schema, global query filter on `BibliotecaId`**
  resolved from the authenticated user's tenant — simplest correct isolation for the expected scale,
  with authz tests as the safety gate.

## Open modeling decisions (resolve in design phase, do not assume)

1. Score recovery: on-time-only vs. time-decay of penalties.
2. Categoria: flat list vs. hierarchy vs. free tags.
3. Tenancy isolation: shared-schema filter vs. schema-per-tenant.
4. AjusteManual: time-bounded (auto-expires) vs. permanent until removed; any cap on the bonus.
5. ListaDeAtenção trigger: exactly `< 0`, or a per-tenant configurable threshold.
6. WhatsApp provider: official Cloud API vs. a BSP (e.g., Twilio/Z-API) — affects cost and setup.
