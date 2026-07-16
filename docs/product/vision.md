# Product Vision — biblioteca-api

> **Mode:** Product (confirmed direction) · **Status:** planning artifact, nothing here is built yet
> unless a roadmap phase says so. This is the product contract that the phased
> [roadmap](roadmap.md) slices into spec-driven features, and the
> [domain model](domain-model.md) formalizes.

## One-liner

A multi-library (SaaS) system for **physical libraries across Brazil**: catalog with physical copies,
free lending with due dates, and a **reputation score** — instead of money — that penalizes late
returns and drives per-reader borrowing privileges, backed by **Email + WhatsApp** reminders and an
operator UI. Built to be **extended, not boxed in**: notification channels, scoring rules, and
privileges are pluggable/configurable so new libraries and new features fit without a rewrite.

## Problem & context

Small and mid-size physical libraries (schools, community, corporate, church, neighborhood) still
run on spreadsheets or paper. They lose track of who has which copy, when it is due, and who is
chronically late. Commercial library software is heavy, expensive, and assumes a paid-rental or
fine-based model that many of these libraries neither want nor are allowed to operate.

We replace **money as the enforcement mechanism** with **reputation**: a reader who returns on time
keeps a high score and unlocks better privileges (more concurrent loans, longer periods, renewals);
a reader who is chronically late loses score and privileges, with a transparent history. No payment
integration, no fines to collect — the incentive is trust and access.

## Target users (personas)

| Persona | Role in system | Primary needs |
| --- | --- | --- |
| **Library Admin** (`Admin`) | Owns one tenant (one library) | Configure library, categories, loan rules & score bands, manage staff, see reports |
| **Attendant / Librarian** (`Atendente`) | Front-desk operator | Fast checkout/check-in, find a member and a copy, see who is overdue |
| **Reader** (`Leitor`) | Library member | Browse catalog, see own loans & due dates, renew, see own score & history |

Multi-tenant: each **Biblioteca** (tenant) has fully isolated data. Staff and readers belong to
exactly one tenant.

## Value proposition

- **No money, still accountable.** A reputation score + saved late-return history enforces good
  behavior without payments, fines, or a gateway to integrate.
- **Privileges that adapt — with a human escape hatch.** Borrowing limits, loan length, and renewals
  are derived from the reader's profile level (from the score); an attendant can still grant a
  specific reader more slots or more days (**AjusteManual**) without changing global rules.
- **Deadlines that don't slip silently.** Reminders over **Email and WhatsApp** — the channel
  Brazilians actually read — before and after the due date, reduce overdue rates.
- **Problem readers get a human, not just a ban.** Readers whose score goes below zero surface on an
  **attention list** for the attendant to reach out and understand the case.
- **Runs a real physical desk.** Copy-level (Exemplar) tracking, not just titles, so the system
  matches the shelf.

## Design principle — extensible by default

This is not a closed, rigid product. Concretely: notification **channels** are pluggable (Email,
WhatsApp today; SMS/push later with no change to reminder logic); scoring parameters and profile-level
bands are **per-tenant configuration**, not hardcoded; per-reader **AjusteManual** overrides handle
the exceptions rules can't. New capabilities attach at these seams instead of forcing rewrites.

## Goals

- G1 — A librarian can complete a checkout and a check-in in under 30 seconds at the desk.
- G2 — Late returns automatically adjust the reader's score and record a history entry — no manual
  bookkeeping.
- G3 — A reader's borrowing privileges always reflect their current profile level, with zero manual
  intervention.
- G4 — Readers receive timely email reminders (before due, on due date, and while overdue).
- G5 — Multiple libraries run on one deployment with strict data isolation.

## Non-goals (explicit)

- **No payments / fines / billing.** No Stripe/PIX/gateway, no invoices. Penalty is score only.
- **No public-internet e-commerce.** This is not a bookstore; nothing is sold.
- **No inter-library federation** in the core (a tenant's data does not leak to another).
- **No mobile native apps** initially — responsive web UI only.
- **No physical hardware requirement** (barcode scanners are a nice-to-have, not required).

## Success metrics (to instrument once live)

- Overdue rate (open loans past due ÷ open loans) trending down after reminders ship.
- Median desk checkout time.
- % of loans returned on time.
- Active tenants and active readers per tenant.

## Constraints & assumptions

- Stack is set: ASP.NET Core 8 Minimal API + EF Core 9 + PostgreSQL (see `.specs/STATE.md`).
- Data privacy: readers' emails and borrowing history are personal data — **LGPD** applies
  (consent, export, deletion). Treated as a cross-cutting concern in the roadmap.
- Every Usuario has **Email and Celular** (both required) — the celular enables WhatsApp reminders.
- Delivery depends on external providers per tenant: **SMTP** (email) and a **WhatsApp API** (Cloud
  API or a BSP such as Twilio/Z-API — provider choice is an open decision). Failures are logged and retried.
- **Brazil-first:** PT-BR UI, LGPD compliance, and WhatsApp as a first-class channel are baseline,
  not add-ons.
- Reputation rules and score bands are **per-tenant configurable** with sane defaults, because
  different libraries tolerate lateness differently.

## Top risks

| Risk | Impact | Mitigation |
| --- | --- | --- |
| Multi-tenant data leak (cross-tenant read) | Severe / trust-ending | Tenant isolation enforced at the data layer + authz tests as a gate (see security cross-cut) |
| Score model feels unfair / opaque to readers | Adoption | Transparent history, configurable bands, visible rules in reader portal |
| Email deliverability (spam, bounces) | Reminders don't land | Per-tenant verified sender, delivery log, retries, dashboard of failures |
| Scope creep into payments/fines | Dilutes the differentiator | Non-goal is explicit; revisit only with confirmed demand |

## Where this goes next

1. [domain-model.md](domain-model.md) — ubiquitous language, entities, the reputation/level model,
   and the loan lifecycle.
2. [roadmap.md](roadmap.md) — phased slices, each mapping to a `.specs/features/<slug>/` spec built
   through the spec-driven pipeline.
