# Homework 3 — Virtual Card Lifecycle Specification

## Role of this file
1. Project memory for Claude Code (context carried across sessions)
2. Editor/AI rules deliverable (#3)

## Chosen Domain
Virtual card lifecycle: create, freeze/unfreeze, set spend limits,
view transactions. EU-regulated, neobank-style, backed by Stripe Issuing,
out-of-PCI-CDE by design.

## Stakeholders
- End users (cardholders)
- Internal ops/compliance team

## What this project produces
DOCUMENTS ONLY. No code, no implementation, no APIs.
Deliverables: specification.md, agents.md, README.md, CLAUDE.md.

## Authoritative documents (read first when in doubt)
- specification.md — layered spec: vision → objectives → tasks
- agents.md — operating guide, Definition of Done, forbidden behaviors

## Rule precedence
1. Compliance / security non-negotiables (PCI-DSS, GDPR, PSD2)
2. Task IDs in specification.md
3. Domain conventions below
4. Stylistic judgement
When unsure, ask rather than guess.

## Domain conventions

### Money
- Amounts as BIGINT minor units + ISO 4217 currency code
- Example: { "amount": 1500, "currency": "EUR" } = €15.00
- Never floats, never omit currency

### Card terminology
- PAN = Primary Account Number — never log, store, or display unmasked
- CVV/CVC — never persisted post-authorization
- Freeze = reversible; Cancellation = permanent — never conflate
- last4 + brand are the only card-derived fields allowed in examples

### Audit trail
- Every state change produces an immutable audit event
- Minimum fields: actor_id, timestamp (UTC ISO-8601), action,
  before_state, after_state, request_id, reason_code (compliance actions)

### Idempotency
- All write operations must define idempotency key behavior
- State: behavior on duplicate within TTL and after TTL expiry

### Assumed SLO targets (label as assumed in spec)
- P95 read latency: ≤ 200ms
- P95 write latency: ≤ 500ms
- Audit event delivery: ≤ 2s after triggering action
- Read-after-write consistency: eventual within 500ms

### Edge cases (always consider)
- Concurrent freeze/unfreeze on the same card
- Spend limit set to zero
- In-flight transaction arriving after card freeze
- Currency mismatch between limit and transaction
- Ops and user acting on same card simultaneously

## Forbidden
- Vague language: "fast", "secure", "appropriate" → measurable targets only
- Floats for money
- Logging PAN, CVV, or raw card numbers in any example
- State changes without a corresponding audit event
- Tasks without a Definition of Done
- Mid-level objectives that aren't observable

## Verification language
Each low-level task closes with:
- [ ] Observable: what you can check in the system
- [ ] Testable: unit / integration / e2e
- [ ] Compliant: which audit or policy rule it satisfies

## File map
- specification.md — layered spec
- agents.md — AI agent rules for this domain
- README.md — rationale and industry practices
- CLAUDE.md — this file