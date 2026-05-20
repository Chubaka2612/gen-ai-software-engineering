# Homework 3 — Virtual Card Lifecycle Specification

---

## 1. Student & Task Summary

**Author:** Viktoriia Skirko

This project produces a complete, regulation-grounded feature specification for a
virtual card lifecycle in an EU-regulated neobank environment. The domain covers five
self-service operations — create, freeze/unfreeze, set spend limits, view transactions,
and cancel — for virtual cards issued via Stripe Issuing, denominated exclusively in EUR,
and governed by PSD2, GDPR, PCI-DSS v4.0, and 4AMLD.

Scope was locked before any content was written through a structured ten-question
discovery session. Decisions included: card types (single-use and recurring), spend limit
dimensions (per-transaction and per-month only), transaction access (read-only, 13-month
PSD2 window, no disputes), actor model (cardholder, read-only analyst, compliance officer,
SYSTEM), idempotency TTL (24 hours, assumed), tech context (Stripe Issuing, OAuth 2.0 +
JWT, optimistic locking), and nine explicit exclusions (physical cards, 3DS/SCA,
chargebacks, FX, rewards, KYC onboarding, renewal/re-issue, PIN, push-to-wallet).

The deliverable package contains ten files:

```
homework-3/
├── CLAUDE.md                          # Project memory and AI rules (pre-existing)
├── agents.md                          # AI agent operating guide (this course's deliverable)
├── README.md                          # This file
└── specification/
    ├── 01-objective.md                # §1–2: high-level objective and scope boundary
    ├── 02-actors.md                   # §3: actor roles and authorization boundaries
    ├── 03-state-machine.md            # §4: card states, transitions, type behavior
    ├── 04-objectives.md               # §5: seven mid-level objectives (OBJ-1–7)
    ├── 05-nonfunctional.md            # §6–8: NFRs, data model conventions, regulatory refs
    ├── 06-implementation.md           # §9–11: guardrails, pre-conditions, post-conditions
    ├── 07-tasks.md                    # §12: 24 low-level tasks with DoD checklists
    └── 08-edge-cases.md               # §13: cross-cutting edge case summary table
```

---

## 2. How I Built This

### Discovery Phase

Before any specification content was written, ten clarifying questions were posed and
answered to lock scope. The questions covered: which card operations to include, card
creation flavors, spend limit dimensions and currency, transaction access model,
compliance strictness level, actor model and sub-roles, idempotency TTL, technology
context, SLO targets, and the explicit exclusion list. Every answer was confirmed in a
decision log before the first line of specification was drafted.

This matters for spec quality because ambiguous scope produces ambiguous requirements.
A task that says "freeze the card quickly" is untestable. A task that says "transition
card to `FROZEN`, return `200 OK` within P95 ≤ 500 ms, emit `CARD_FROZEN` audit event
within P95 ≤ 2 s" is not. The discovery phase is what makes that specificity possible
without mid-draft rework.

The opening prompt used to start the session is stored verbatim in
[.claude/prompts/initial-session.md](.claude/prompts/initial-session.md).

### Document Order and Rationale

**Specification first.** Every other document in this package derives from the
specification. Agents cannot be given rules about task IDs they cannot reference.
A README cannot explain practices that have not been decided. Writing anything else
first would mean writing against a moving target, producing documents that are either
ahead of or behind the spec they describe.

**agents.md second.** The agent operating guide encodes the specification's conventions
as behavioral rules: money format, PAN/CVV prohibition, audit event atomicity, idempotency
coverage, state machine integrity. These rules are only meaningful once the specification
has locked the conventions they enforce. Writing agents.md before the spec was approved
would require either guessing the conventions or re-editing the agent guide every time
the spec changed.

**README last.** This document summarizes a finished package. The industry practices
table in §3 cites actual section numbers; the rationale paragraphs in §4 explain
decisions that were made, not decisions that are pending. A README written first is
a project plan. A README written last is honest documentation.

**Specification decomposed into eight files.** The specification was initially produced
as a single 1,222-line monolithic file. After approval, it was split into eight
section-scoped files under `specification/` with navigation headers (Prev / Next links
and a one-line description per file). The motivation was navigability and
maintainability: a reader following a task cross-reference should land on a focused
file, not scroll a 1,200-line document. Each file maps to a coherent layer of the spec
(scope, actors, state machine, objectives, NFRs, guardrails, tasks, edge cases), so
changes to one layer do not require scanning the whole document. All content was
preserved exactly — no summarizing, no compression.

### Tools and Workflow

- **Model:** claude-sonnet-4-6 via Claude Code (VSCode extension)
- **Workflow:** single linear session — discovery, then specification, then agents.md,
  then README.md. No parallel drafts, no branching revisions.
- **CLAUDE.md** was the only manually created file. It defined domain conventions
  (money representation, PAN/CVV rules, audit trail fields, idempotency behavior,
  SLO defaults, edge cases to always consider) and served as the persistent project
  memory carried across sessions.
- **Prompting:** direct user prompts, no slash commands or skills. The initial prompt
  ([.claude/prompts/initial-session.md](.claude/prompts/initial-session.md)) specified
  document order, layer structure, cross-cutting requirements, and the instruction to
  ask before writing — not after.

---

## 3. Industry Best Practices

| Practice | Where it appears | File / Section |
|---|---|---|
| PAN never stored post-authorization | Sensitive card data table; implementation guardrail #1; TASK-1.2 and TASK-1.5 DoD Compliant checkboxes | [specification/05-nonfunctional.md §7.2](specification/05-nonfunctional.md) · [specification/06-implementation.md §9 rule 1](specification/06-implementation.md) |
| Audit event atomicity with state change | Implementation guardrail #10; every task's Compliant checkbox; TASK-1.5 edge case (audit write fails → rollback) | [specification/06-implementation.md §9 rule 10](specification/06-implementation.md) · [specification/07-tasks.md TASK-1.5](specification/07-tasks.md) |
| Optimistic locking for concurrent writes | Implementation guardrail #2; TASK-2.5 full task; §13 edge case row "Concurrent freeze/unfreeze" | [specification/06-implementation.md §9 rule 2](specification/06-implementation.md) · [specification/07-tasks.md TASK-2.5](specification/07-tasks.md) · [specification/08-edge-cases.md](specification/08-edge-cases.md) |
| Idempotency on all write operations | NFR idempotency behavior table (4 scenarios); every task description requires `Idempotency-Key` header | [specification/05-nonfunctional.md §7.4](specification/05-nonfunctional.md) · [specification/07-tasks.md](specification/07-tasks.md) throughout |
| GDPR Art. 17(3)(b) vs PSD2 retention conflict | TASK-6.2 edge case (pseudonymize, do not delete); §13 summary row "GDPR erasure vs. PSD2 / 4AMLD retention conflict" | [specification/07-tasks.md TASK-6.2](specification/07-tasks.md) · [specification/08-edge-cases.md](specification/08-edge-cases.md) |
| 13-month transaction history (PSD2 Art. 67(1)) | OBJ-5 verification paragraph; TASK-5.1, TASK-5.4 DoD; data retention table | [specification/04-objectives.md OBJ-5](specification/04-objectives.md) · [specification/05-nonfunctional.md §6.3](specification/05-nonfunctional.md) · [specification/07-tasks.md TASK-5.4](specification/07-tasks.md) |
| Zero-limit as distinct state from no-limit | Implementation guardrail #6; TASK-4.1 edge case (`amount = 0` is valid and enforced); TASK-4.4 evaluation order | [specification/06-implementation.md §9 rule 6](specification/06-implementation.md) · [specification/07-tasks.md TASK-4.1, TASK-4.4](specification/07-tasks.md) |
| `reason_code` mandatory for compliance actions | Audit event schema `reason_code` field (conditional); TASK-2.2, TASK-2.4, TASK-3.2, TASK-7.4 DoD | [specification/05-nonfunctional.md §7.3](specification/05-nonfunctional.md) · [specification/07-tasks.md TASK-2.2](specification/07-tasks.md) |
| Out-of-PCI-CDE by design via Stripe Issuing | High-level objective statement; implementation guardrail #1; `stripe_card_id` as internal-only reference | [specification/01-objective.md §1](specification/01-objective.md) · [specification/06-implementation.md §9 rule 1](specification/06-implementation.md) · [specification/05-nonfunctional.md §7.2](specification/05-nonfunctional.md) |
| Assumed SLO targets with justification | NFR latency table with a Justification column for each target | [specification/05-nonfunctional.md §6.1](specification/05-nonfunctional.md) |

---

## 4. Rationale

### Why were performance targets chosen at these values?

The 200 ms P95 read target derives from Nielsen's (1993) foundational UX research:
responses within 100 ms feel instantaneous; beyond 1 second, users begin to lose the
sense of direct manipulation. 200 ms sits comfortably inside that window and is
the standard neobank API SLO for read operations that serve live UI state. The 500 ms
write target accounts for the optimistic lock round-trip while remaining within
Nielsen's 1-second flow-of-thought threshold. The 3-second card creation target is
set by Stripe Issuing provisioning latency: a synchronous Stripe API call followed by
a webhook acknowledgment cannot reliably complete faster, and 3 seconds is the outer
bound before users interpret a delay as an error. The 5-second transaction sync target
covers normal Stripe webhook delivery variance (typically under 1 second, but the
budget absorbs spikes) without requiring architectural complexity like a push
notification layer. All five targets are labeled "assumed" in the specification because
they are informed estimates, not contracted SLAs — a builder inheriting this spec should
validate them against their actual Stripe account's observed latency before treating them
as commitments.

### Why is the compliance layer structured as an overlay on product mechanics rather than a separate document?

Separating compliance into its own section or document creates a reading path where
engineers study the product spec, mark tasks complete, and read the compliance annex
only when a legal review flags a gap. In regulated systems that sequence is backwards:
the freeze operation exists because PSD2 Art. 68 requires it; the 13-month transaction
window is the product because PSD2 Art. 67(1) mandates it; the audit trail is not
a logging feature but a PSD2 Art. 82 and PCI-DSS Req. 10 obligation. Embedding the
regulation citation inside the task's Definition of Done Compliant checkbox means a
builder cannot close the task without acknowledging the specific article that motivated
it. The compliance layer is also structurally non-separable for some requirements:
the GDPR Art. 17(3)(b) override of the erasure right appears as an edge case inside
TASK-6.2, not in a separate GDPR section, because it only makes sense in the context
of the audit immutability guarantee it qualifies.

### Why does the spec treat edge cases as first-class requirements with their own tables rather than prose footnotes?

In a regulated payment domain, edge cases are not rare code paths — they are the
scenarios auditors probe first and the conditions under which a compliance defect
surfaces. The GDPR erasure vs. PSD2 retention conflict, the concurrent freeze/unfreeze
race condition, the in-flight transaction surviving a freeze or cancellation: none of
these have an obvious correct answer derivable from the happy path. Making them
first-class by embedding an edge-case table in each task achieves three things that
prose footnotes cannot. First, they are unavoidable: a reader who skips footnotes
cannot skip a table that sits between the task description and the Definition of Done.
Second, they are testable: each row in the table states an input or scenario and an
expected behavior, which is exactly the structure of a test case. Third, they are
traceable: the §13 cross-cutting summary table ([specification/08-edge-cases.md](specification/08-edge-cases.md))
indexes every edge case back to the task that owns it, so a reviewer auditing the
concurrent-write behavior can navigate directly to TASK-2.5 rather than searching
prose for the word "concurrent."
