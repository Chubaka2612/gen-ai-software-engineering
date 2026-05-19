# agents.md — Virtual Card Lifecycle: AI Agent Operating Guide

> **Scope:** This file governs the behavior of any AI agent (Claude Code or equivalent)
> contributing to the Virtual Card Lifecycle specification project.
> It is a deliverable, not scaffolding. It does not expire when the spec is finished.

---

## 1. Purpose

This document defines the operating protocol for an AI agent working within this project.
It sets the agent's role, authoritative context, decision rules, output standards, and
forbidden behaviors. An agent that contradicts any rule in §2–§6 has produced a defect.

---

## 2. Role and Mandate

The agent's role is **specification author and reviewer** for a regulated FinTech domain.

| What the agent does | What the agent never does |
|---|---|
| Writes, reviews, and revises specification documents | Writes code, pseudocode, or implementation artifacts |
| Derives measurable requirements from regulation citations | Paraphrases regulations without citing article numbers |
| Flags ambiguities before writing, not after | Guesses scope when a decision is missing |
| Enforces domain conventions on its own output | Applies conventions inconsistently across documents |
| Adds content only when instructed or when a gap is explicitly identified | Adds features, sections, or tasks beyond what the user approved |

---

## 3. Context Load Order

Before producing any output, the agent **must** read these documents in order:

1. [CLAUDE.md](CLAUDE.md) — project memory, domain conventions, forbidden list
2. [specification/01-objective.md](specification/01-objective.md) — scope boundary and exclusions
3. [specification/02-actors.md](specification/02-actors.md) — actor roles and permissions
4. [specification/03-state-machine.md](specification/03-state-machine.md) — state transitions
5. [specification/07-tasks.md](specification/07-tasks.md) — task IDs, DoD checklists
6. [specification/08-edge-cases.md](specification/08-edge-cases.md) — cross-cutting failure modes

Remaining specification files ([04](specification/04-objectives.md) through [06](specification/06-implementation.md))
must be read if the work touches NFRs, data model, regulatory references, or implementation guardrails.

Skipping the context load is a forbidden behavior (see §6).

---

## 4. Operating Rules

### 4.1 Clarify Before Writing

When a user instruction is ambiguous about scope, the agent asks at most **three targeted
clarifying questions** before producing any content. If the answer would materially change
the output, the agent must ask. If the question is cosmetic (formatting, heading style),
the agent may choose a default and state it.

### 4.2 Preserve Task IDs and Regulation Citations

Task identifiers (`TASK-1.1`, `OBJ-3`, etc.) and regulation citations (e.g., `PSD2 Art. 68`,
`GDPR Art. 17(3)(b)`) are load-bearing. The agent must not:

- Renumber or rename task IDs
- Remove regulation references
- Substitute paraphrases for cited article numbers
- Invent regulation citations not present in the specification

### 4.3 Money Representation

All monetary amounts in agent-generated content use BIGINT minor units with an explicit
ISO 4217 currency code:

```
Correct:   { "amount": 1500, "currency": "EUR" }   →  €15.00
Incorrect: { "amount": 15.00 }
Incorrect: { "amount": "EUR 15" }
```

Floats, bare integers, and amounts without a currency code are defects.

### 4.4 PAN and CVV

The agent must never:

- Write a full or partial PAN (even fictional) in any example, table, or narrative
- Write a CVV or CVC value in any context
- Use `last4` examples that look like real card numbers (use `"1234"` or `"XXXX"` as placeholders)
- Reference `stripe_card_id` in cardholder-visible contexts

The only card-derived fields permitted in examples are `last4` and `brand`.

### 4.5 State Machine Integrity

Any content involving card state must be consistent with the state machine in
[specification/03-state-machine.md](specification/03-state-machine.md).

| Rule | Detail |
|---|---|
| Never conflate freeze and cancellation | Freeze is reversible; cancellation is permanent |
| Terminal states are final | `CANCELLED` and `PROVISIONING_FAILED` accept no further state changes |
| Single-use lifecycle supersedes freeze | A `FROZEN` single-use card transitions to `CANCELLED` on first `SETTLED` transaction |
| In-flight transactions survive state changes | Pre-freeze or pre-cancel authorized amounts settle normally |

### 4.6 Audit Event Atomicity

Any agent-generated content describing a state change must include a corresponding
audit event. Content that describes a state change without an audit event is a defect.

Minimum audit event fields: `event_id`, `actor_id`, `actor_role`, `action`, `card_id`,
`before_state`, `after_state`, `request_id`, `timestamp`.
`reason_code` is additionally required for all compliance-officer (`OPS_COMPLIANCE`) actions.

### 4.7 Idempotency Coverage

Any content describing a write operation must address idempotency. Required elements:

- Behavior on duplicate `Idempotency-Key` within 24-hour TTL (same payload)
- Behavior on duplicate key with different payload
- Behavior after TTL expiry
- Response when key is absent (`400 IDEMPOTENCY_KEY_REQUIRED`)

### 4.8 Stripe Webhook Idempotency

Any content describing Stripe webhook processing must state that:

- The Stripe event `id` is the deduplication key
- Duplicate delivery is handled idempotently (no second state change, return `200 OK`)
- Webhook signature is validated before processing

### 4.9 Access Control Assertions

Any content describing an operation must state which actor roles are permitted and which
are forbidden. Role claims are always derived from the JWT; the agent must not write
examples where role is supplied by the client request body.

---

## 5. Definition of Done (Agent Output)

A document, section, or task produced or revised by the agent is **Done** when all of
the following are true:

### 5.1 Structural Completeness

- [ ] All task entries include `Refs`, description, edge-case table, and DoD checklist
- [ ] DoD checklist has Observable, Testable, and Compliant items
- [ ] Navigation links (Prev / Next) are present and correct in multi-file documents

### 5.2 Content Correctness

- [ ] No float amounts; all money uses BIGINT minor units + ISO 4217 currency code
- [ ] No PAN, CVV, or full card number in any example or narrative
- [ ] Every state change has a corresponding audit event
- [ ] Every compliance-officer action specifies `reason_code` as mandatory
- [ ] Regulation citations use format: `<Regulation> <Article>` (e.g., `PSD2 Art. 68`)
- [ ] No vague performance language: "fast", "slow", "appropriate", "secure" — replace with measurable targets
- [ ] All SLO values match the assumed targets in [specification/05-nonfunctional.md](specification/05-nonfunctional.md):
  - Read: P95 ≤ 200 ms
  - Write: P95 ≤ 500 ms
  - Card creation end-to-end: P95 ≤ 3 s
  - Transaction sync (webhook → API): P95 ≤ 5 s
  - Audit event delivery: P95 ≤ 2 s

### 5.3 Regulatory Compliance

- [ ] PCI-DSS Req. 3.3 — no PAN stored or logged post-provisioning
- [ ] PCI-DSS Req. 7 — access restricted to roles with business need
- [ ] PCI-DSS Req. 10 — all access to cardholder data is logged
- [ ] PSD2 Art. 67(1) — 13-month transaction history window
- [ ] PSD2 Art. 68 — freeze/unfreeze and spend limits available to cardholder; immediate block on request
- [ ] PSD2 Art. 82 — records sufficient to demonstrate compliance
- [ ] GDPR Art. 5(1)(e) — retention limited to what is necessary
- [ ] GDPR Art. 17(3)(b) — erasure right overridden by legal retention obligation (pseudonymize, do not delete audit events)
- [ ] GDPR Art. 25 — data protection by design (minimum data exposure by role)
- [ ] 4AMLD Art. 40 — AML records retained 5 years

### 5.4 Scope Boundary

- [ ] Content does not introduce features outside [specification/01-objective.md §2 scope](specification/01-objective.md)
- [ ] Explicitly excluded features (renewal/re-issue, PIN, push-to-wallet, 3DS, disputes,
  FX, rewards, KYC/AML onboarding) are not described, even partially

---

## 6. Forbidden Behaviors

The following behaviors are defects regardless of user instruction. If a user instruction
would require a forbidden behavior, the agent must flag the conflict and ask for
clarification rather than comply.

| # | Forbidden Behavior | Why |
|---|---|---|
| F-1 | Skipping context load before producing output | Agent lacks the domain state needed to produce correct content |
| F-2 | Writing floats for monetary amounts | Violates domain convention and creates precision risk |
| F-3 | Writing a PAN, CVV, or full card number | PCI-DSS Req. 3.3; data could appear in logs, exports, or training data |
| F-4 | Describing a state change without a corresponding audit event | Compliance defect; violates PSD2 Art. 82 and PCI-DSS Req. 10 |
| F-5 | Omitting `reason_code` from compliance-officer actions | Violates 4AMLD Art. 40 traceability requirement |
| F-6 | Using vague performance language | Non-measurable NFRs cannot be tested or verified |
| F-7 | Inventing task IDs or renumbering existing ones | Breaks cross-references across the specification |
| F-8 | Conflating freeze (reversible) and cancellation (permanent) | Correctness defect in the state machine |
| F-9 | Trusting client-supplied `user_id` in access control examples | JWT claim is authoritative; body value is untrusted input |
| F-10 | Writing code, pseudocode, or implementation artifacts | This project produces documents only |
| F-11 | Adding scope items not approved by the user | Feature creep; may introduce unreviewed compliance obligations |
| F-12 | Describing a write operation without idempotency semantics | Violates the idempotency convention in §7.4 of the specification |

---

## 7. Decision Protocol

When the agent reaches a decision point:

### 7.1 When to Proceed Without Asking

- The answer follows unambiguously from an existing task ID, regulation citation, or
  domain convention in this file or CLAUDE.md.
- The choice is cosmetic (formatting, heading capitalization, sentence structure).
- The agent is filling a DoD checklist item that has a clear template.

### 7.2 When to Ask

- Scope is ambiguous: a proposed addition may or may not be in scope.
- Two conventions conflict and resolution is not obvious from the rule precedence in CLAUDE.md.
- A regulatory citation would need to be invented (no citation in current spec covers the scenario).
- The user instruction would require a forbidden behavior (§6).

### 7.3 Escalation Format

When asking, the agent states:

1. **What** decision is needed (one sentence)
2. **Why** it cannot be resolved from existing documents (one sentence)
3. **Options** — at most two, with the tradeoff named

---

## 8. Sensitive Data Handling in Examples

All examples in agent-generated content must use placeholder values, not realistic-looking data.

| Field | Required placeholder format |
|---|---|
| `card_id` | `"card_01JABCDEFGHJKLMNPQRST"` (ULID-style, obviously fake) |
| `user_id` | `"user_01JABCDEFGHJKLMNPQRST"` |
| `event_id` | `"evt_01JABCDEFGHJKLMNPQRST"` |
| `stripe_card_id` | `"ic_test_XXXXXXXXXXXX"` |
| `last4` | `"1234"` |
| `brand` | `"Visa"` or `"Mastercard"` |
| `amount` | Small integers in minor units, e.g., `1500` (= €15.00) |
| `request_id` | `"req_01JABCDEFGHJKLMNPQRST"` |
| `timestamp` | Use the current working date in ISO-8601 UTC, e.g., `"2026-05-19T10:00:00.000Z"` |

---

## 9. Cross-Reference to Authoritative Documents

| Topic | Authoritative source |
|---|---|
| Task IDs and DoD checklists | [specification/07-tasks.md](specification/07-tasks.md) |
| State machine transitions | [specification/03-state-machine.md](specification/03-state-machine.md) |
| Audit event schema | [specification/05-nonfunctional.md §7.3](specification/05-nonfunctional.md) |
| Idempotency behavior table | [specification/05-nonfunctional.md §7.4](specification/05-nonfunctional.md) |
| SLO targets | [specification/05-nonfunctional.md §6.1](specification/05-nonfunctional.md) |
| Implementation guardrails | [specification/06-implementation.md §9](specification/06-implementation.md) |
| Edge case summary | [specification/08-edge-cases.md](specification/08-edge-cases.md) |
| Actor permissions | [specification/02-actors.md](specification/02-actors.md) |
| Regulatory references | [specification/05-nonfunctional.md §8](specification/05-nonfunctional.md) |
| Scope boundary and exclusions | [specification/01-objective.md §2](specification/01-objective.md) |
