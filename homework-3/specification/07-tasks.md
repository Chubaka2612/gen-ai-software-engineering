> **Virtual Card Lifecycle — Feature Specification**
> *24 low-level tasks across 7 objectives, each with edge-case tables and Observable / Testable / Compliant DoD checklists.*
> [README](../README.md) | **← Prev** [06-implementation.md](06-implementation.md) | **Next →** [08-edge-cases.md](08-edge-cases.md)

---

# §12 Low-Level Tasks

Each task includes: objective reference, description, edge cases, and a Definition of Done
(Observable / Testable / Compliant).

---

### OBJ-1: Card Creation

---

#### TASK-1.1 — Validate card creation request

**Refs:** OBJ-1

Accept a card creation request from an authenticated cardholder. Required fields:
`card_type` (`SINGLE_USE` | `RECURRING`), `Idempotency-Key` header. Optional fields:
`per_transaction_limit` (money object), `per_month_limit` (money object). The
cardholder `user_id` is extracted from the JWT — it is not a client-supplied field.

**Edge cases:**

| Input | Expected response |
|---|---|
| Missing `Idempotency-Key` | `400 Bad Request`, code `IDEMPOTENCY_KEY_REQUIRED` |
| Invalid `card_type` value | `400 Bad Request`, code `INVALID_CARD_TYPE` |
| Limit `currency` ≠ `"EUR"` | `422 Unprocessable Entity`, code `UNSUPPORTED_CURRENCY` |
| Limit `amount` as float or negative | `400 Bad Request`, code `INVALID_AMOUNT` |
| Unauthenticated request | `401 Unauthorized`, code `TOKEN_MISSING` |

**Definition of Done:**
- [ ] **Observable:** Valid request returns `201 Created` with `card_id`, `card_type`,
  `state: "PENDING_PROVISIONING"`, `created_at`. Invalid requests return the
  appropriate 4xx error code and machine-readable `code` field.
- [ ] **Testable:** Unit — field validation rejects each invalid case listed above.
  Integration — valid authenticated request reaches Stripe provisioning call.
- [ ] **Compliant:** No PAN or CVV in request, response, or logs. PCI-DSS Req. 3.3.

---

#### TASK-1.2 — Provision card via Stripe Issuing

**Refs:** OBJ-1

After persisting the card record in `PENDING_PROVISIONING` state, call the Stripe
Issuing card creation API. Store the returned `stripe_card_id` internally. Populate
`last4` and `brand` from the Stripe response.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Stripe API returns an error | Card remains `PENDING_PROVISIONING`; error logged without PAN |
| No Stripe confirmation within 30 s | Card transitions to `PROVISIONING_FAILED`; audit event with `actor_id: "SYSTEM"`, `reason_code: "PROVISIONING_TIMEOUT"` |

**Definition of Done:**
- [ ] **Observable:** On Stripe success, card state transitions `PENDING_PROVISIONING` →
  `ACTIVE`; `last4` and `brand` populated. On failure, card reaches `PROVISIONING_FAILED`
  with a corresponding audit event.
- [ ] **Testable:** Integration — mock Stripe success → card reaches `ACTIVE`. Mock Stripe
  failure → card reaches `PROVISIONING_FAILED` with audit event.
- [ ] **Compliant:** Full PAN from Stripe response is never written to application-layer
  logs or database. PCI-DSS Req. 3.3.

---

#### TASK-1.3 — Handle Stripe `issuing_card.created` webhook

**Refs:** OBJ-1

Receive and validate the Stripe `issuing_card.created` webhook. On successful validation,
transition the card from `PENDING_PROVISIONING` to `ACTIVE`. Use the Stripe event `id`
as the deduplication key to handle duplicate webhook delivery.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Duplicate Stripe event `id` | Idempotent: no second state transition; return `200 OK` |
| Unknown `stripe_card_id` | Log warning; return `200 OK` (4xx would trigger infinite Stripe retries) |
| Invalid webhook signature | Reject with `400 Bad Request`; do not process |

**Definition of Done:**
- [ ] **Observable:** Card state reads `ACTIVE` after webhook. Duplicate delivery does not
  produce duplicate audit events.
- [ ] **Testable:** Integration — send duplicate webhook payloads; verify single audit event
  and single state transition.
- [ ] **Compliant:** Signature validated on every delivery. PCI-DSS Req. 10.

---

#### TASK-1.4 — Store initial spend limits atomically

**Refs:** OBJ-1

If `per_transaction_limit` or `per_month_limit` is included in the creation request,
store these atomically with the card record. If not provided, the card is created with
no limit (no cap beyond Stripe defaults).

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Card reaches `PROVISIONING_FAILED` | Limit record rolled back with the card |
| Zero-value limit at creation | Stored as-is; first authorization attempt will be declined |

**Definition of Done:**
- [ ] **Observable:** Card read includes a `limits` object with configured values, or `null`.
  Audit event for card creation includes `after_state` with limit values.
- [ ] **Testable:** Unit — limit record created transactionally with card. Integration — zero-limit
  card declines first authorization attempt.
- [ ] **Compliant:** Limit values captured in creation audit event. PSD2 Art. 68.

---

#### TASK-1.5 — Emit `CARD_CREATED` audit event

**Refs:** OBJ-1, OBJ-6

Emit an audit event with `action: "CARD_CREATED"` atomically with (or via outbox from)
the card creation write. Fields: `actor_id` (cardholder `user_id`), `card_type`,
`after_state: { state: "PENDING_PROVISIONING" }`, `request_id`.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Audit write fails | Entire card creation transaction rolls back; no card record without audit event |

**Definition of Done:**
- [ ] **Observable:** Audit log contains exactly one `CARD_CREATED` event per card, queryable
  within 2 s of the creation request.
- [ ] **Testable:** Integration — query audit log immediately after card creation; verify all
  fields match the schema in §7.3.
- [ ] **Compliant:** No PAN or CVV in audit event. PCI-DSS Req. 10, PSD2 Art. 82.

---

### OBJ-2: Freeze / Unfreeze

---

#### TASK-2.1 — Cardholder freezes their own card

**Refs:** OBJ-2

An authenticated cardholder can freeze their own card if its current state is `ACTIVE`.
Requires `Idempotency-Key`. Transitions card to `FROZEN`. Emits audit event.

**Edge cases:**

| Input / state | Expected response |
|---|---|
| Card already `FROZEN` | `409 Conflict`, code `CARD_ALREADY_FROZEN`; no audit event |
| Card is `CANCELLED` or `PROVISIONING_FAILED` | `422 Unprocessable Entity`, code `CARD_NOT_ACTIVE` |
| Card belongs to a different cardholder | `403 Forbidden` |
| Optimistic lock conflict | `409 Conflict`, code `CONCURRENT_MODIFICATION`; client must retry |

**Definition of Done:**
- [ ] **Observable:** Card state reads `FROZEN`. New authorization attempts are declined with
  `CARD_FROZEN`. Audit event shows `action: "CARD_FROZEN"`, `before_state: "ACTIVE"`,
  `after_state: "FROZEN"`.
- [ ] **Testable:** Integration — freeze card; attempt authorization; verify decline. Unit —
  ownership check rejects cross-user access.
- [ ] **Compliant:** Audit event present. PSD2 Art. 68, PSD2 Art. 82.

---

#### TASK-2.2 — Compliance officer freezes a card (AML / compliance action)

**Refs:** OBJ-2

An authenticated compliance officer can freeze any `ACTIVE` card. `reason_code` is
mandatory (e.g., `"AML_REVIEW"`, `"FRAUD_INVESTIGATION"`). Requires `Idempotency-Key`.
Emits audit event with `reason_code` populated.

**Edge cases:**

| Scenario | Expected response |
|---|---|
| Missing `reason_code` | `422 Unprocessable Entity`, code `REASON_CODE_REQUIRED` |
| Card already `FROZEN` | `409 Conflict`, code `CARD_ALREADY_FROZEN` |
| Caller has role `ops:analyst` | `403 Forbidden` |

**Definition of Done:**
- [ ] **Observable:** Card state reads `FROZEN`. Audit event has `actor_role: "OPS_COMPLIANCE"`
  and non-null `reason_code`.
- [ ] **Testable:** Integration — compliance officer freezes with valid `reason_code`; verify
  audit. Integration — analyst attempt returns `403`.
- [ ] **Compliant:** `reason_code` required and logged. 4AMLD Art. 40, PCI-DSS Req. 10.

---

#### TASK-2.3 — Cardholder unfreezes their own card

**Refs:** OBJ-2

An authenticated cardholder can unfreeze their own card if its current state is `FROZEN`.
Requires `Idempotency-Key`. Transitions card to `ACTIVE`. Emits audit event.

**Edge cases:**

| State | Expected response |
|---|---|
| Card already `ACTIVE` | `409 Conflict`, code `CARD_NOT_FROZEN` |
| Card is `CANCELLED` | `422 Unprocessable Entity`, code `CARD_NOT_ACTIVE` |
| Card frozen by compliance officer | Cardholder may still unfreeze (no compliance lock in this spec) |

**Definition of Done:**
- [ ] **Observable:** Card state reads `ACTIVE`. New authorization attempts proceed normally.
  Audit event shows `action: "CARD_UNFROZEN"`, `before_state: "FROZEN"`, `after_state: "ACTIVE"`.
- [ ] **Testable:** Integration — freeze then unfreeze; verify authorization succeeds post-unfreeze.
- [ ] **Compliant:** Audit event present. PSD2 Art. 68, PSD2 Art. 82.

---

#### TASK-2.4 — Compliance officer unfreezes a card

**Refs:** OBJ-2

An authenticated compliance officer can unfreeze any `FROZEN` card. `reason_code` is
mandatory. Requires `Idempotency-Key`. Emits audit event.

**Edge cases:** As TASK-2.2, substituting `CARD_NOT_FROZEN` for `CARD_ALREADY_FROZEN`
on an already-active card.

**Definition of Done:**
- [ ] **Observable:** Card state reads `ACTIVE`. Audit event has `actor_role: "OPS_COMPLIANCE"`,
  non-null `reason_code`.
- [ ] **Testable:** Integration — officer unfreezes; verify state and audit. Analyst attempt
  returns `403`.
- [ ] **Compliant:** Audit event with reason code. 4AMLD Art. 40.

---

#### TASK-2.5 — Detect and resolve concurrent freeze/unfreeze

**Refs:** OBJ-2

When two actors submit freeze or unfreeze operations within the same optimistic-lock window,
exactly one succeeds. The losing request receives `409 Conflict`, code `CONCURRENT_MODIFICATION`,
and must retry with fresh state.

**Edge cases:**

| Scenario | Expected outcome |
|---|---|
| Both actors freeze an `ACTIVE` card simultaneously | One succeeds → `FROZEN`; other gets `409`. Net state: `FROZEN`. One audit event. |
| One actor freezes, other unfreezes simultaneously | Version check determines winner; loser retries from fresh state |

**Definition of Done:**
- [ ] **Observable:** Exactly one audit event for the winning operation. Card is in a consistent
  state after both requests resolve.
- [ ] **Testable:** Integration — submit concurrent requests with matching `card_version`; verify
  one `200`, one `409`, one audit event.
- [ ] **Compliant:** No silent data corruption. PCI-DSS Req. 10.

---

#### TASK-2.6 — Preserve in-flight transactions during freeze

**Refs:** OBJ-2

An authorization approved before a freeze was applied settles normally after the freeze.
The freeze does not retroactively void previously authorized transactions.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Authorization request arrives within milliseconds of a freeze write | Stripe makes the authorization decision; the system must not attempt to reverse any resulting authorization as part of the freeze |
| Settlement webhook for in-flight tx arrives while card is `FROZEN` | Transaction settles normally; card state is unaffected |

**Definition of Done:**
- [ ] **Observable:** Transaction authorized before freeze shows `status: "SETTLED"` after
  settlement, regardless of card state at settlement time.
- [ ] **Testable:** Integration — authorize transaction; freeze card; simulate Stripe settlement
  webhook; verify transaction reaches `SETTLED`.
- [ ] **Compliant:** Freeze scope is prospective only. PSD2 Art. 68.

---

### OBJ-3: Card Cancellation

---

#### TASK-3.1 — Cardholder cancels their own card

**Refs:** OBJ-3

An authenticated cardholder can cancel their own card if its current state is `ACTIVE`
or `FROZEN`. Cancellation is permanent. Requires `Idempotency-Key`. Transitions card to
`CANCELLED`. Emits audit event.

**Edge cases:**

| State / input | Expected response |
|---|---|
| Card already `CANCELLED` | `409 Conflict`, code `CARD_ALREADY_CANCELLED` |
| Card in `PENDING_PROVISIONING` | `422 Unprocessable Entity`, code `CARD_NOT_CANCELLABLE` |
| Card belongs to a different cardholder | `403 Forbidden` |

**Definition of Done:**
- [ ] **Observable:** Card state reads `CANCELLED`. All subsequent write operations on the card
  return `422 CARD_CANCELLED`. Audit event shows `after_state: "CANCELLED"`.
- [ ] **Testable:** Integration — cancel card; attempt freeze and set-limit; verify both return
  `422`. Unit — ownership check.
- [ ] **Compliant:** Audit event present; cancellation is permanent. PSD2 Art. 68.

---

#### TASK-3.2 — Compliance officer cancels a card

**Refs:** OBJ-3

An authenticated compliance officer can cancel any `ACTIVE` or `FROZEN` card.
`reason_code` is mandatory (e.g., `"AML_TERMINATION"`, `"FRAUD_CONFIRMED"`).
Requires `Idempotency-Key`. Emits audit event.

**Edge cases:** Same as TASK-3.1 plus: missing `reason_code` → `422 REASON_CODE_REQUIRED`.
`ops:analyst` attempt → `403 Forbidden`.

**Definition of Done:**
- [ ] **Observable:** Card state `CANCELLED`. Audit event has `actor_role: "OPS_COMPLIANCE"`,
  non-null `reason_code`.
- [ ] **Testable:** Integration — compliance officer cancels with reason code; verify. Analyst
  attempt returns `403`.
- [ ] **Compliant:** AML-motivated cancellation traceable in audit. 4AMLD Art. 40, PSD2 Art. 82.

---

#### TASK-3.3 — Block all writes on a terminal-state card

**Refs:** OBJ-3

Any attempt to freeze, unfreeze, or set a spend limit on a card in `CANCELLED` or
`PROVISIONING_FAILED` state must be rejected before any business logic executes.

**Edge cases:** The terminal-state guard must read the current card record from the
datastore (not a cached value) before evaluating the guard.

**Definition of Done:**
- [ ] **Observable:** Each write endpoint returns `422`, code `CARD_CANCELLED` or
  `CARD_PROVISIONING_FAILED`, when the card is in a terminal state.
- [ ] **Testable:** Unit — state guard tested for each write operation across both terminal states.
- [ ] **Compliant:** Terminal state integrity maintained. PSD2 Art. 68.

---

#### TASK-3.4 — System auto-cancels single-use card after first settled transaction

**Refs:** OBJ-3, OBJ-1

Upon receipt of a Stripe `issuing_transaction.created` webhook with `status: settled`
for a `SINGLE_USE` card in `ACTIVE` or `FROZEN` state, the system transitions the card
to `CANCELLED`. Audit event: `actor_id: "SYSTEM"`, `reason_code: "SINGLE_USE_CONSUMED"`.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Card was already `CANCELLED` before webhook arrives | No further transition; idempotent |
| Card was `FROZEN` at settlement | System cancels `FROZEN` → `CANCELLED`; single-use lifecycle supersedes freeze |
| Duplicate Stripe settlement webhook | Idempotent on Stripe event `id`; no second cancellation audit event |

**Definition of Done:**
- [ ] **Observable:** After settlement webhook, single-use card reads `CANCELLED`. Audit event
  shows `actor_id: "SYSTEM"`, `reason_code: "SINGLE_USE_CONSUMED"`.
- [ ] **Testable:** Integration — simulate Stripe settlement webhook for single-use card; verify
  state and audit. Send duplicate webhook; verify single audit event.
- [ ] **Compliant:** System-initiated cancellation is auditable. PSD2 Art. 82, PCI-DSS Req. 10.

---

### OBJ-4: Spend Limits

---

#### TASK-4.1 — Set per-transaction spend limit

**Refs:** OBJ-4

An authenticated cardholder can set or update the per-transaction limit on their own
card in `ACTIVE` or `FROZEN` state. Limit: `{ amount: BIGINT, currency: "EUR" }`.
Requires `Idempotency-Key`. Emits audit event with `before_state` and `after_state`.

**Edge cases:**

| Input | Expected response |
|---|---|
| `amount < 0` | `400 Bad Request`, code `INVALID_AMOUNT` |
| `amount = 0` | Valid; stored and enforced — all authorizations will be declined |
| `currency ≠ "EUR"` | `422 Unprocessable Entity`, code `UNSUPPORTED_CURRENCY` |
| `amount` is a float | `400 Bad Request`, code `INVALID_AMOUNT` |
| Card is `CANCELLED` or `PROVISIONING_FAILED` | `422`, code `CARD_CANCELLED` / `CARD_PROVISIONING_FAILED` |

**Definition of Done:**
- [ ] **Observable:** Card read returns updated `per_transaction_limit`. Authorization for amount
  above limit is declined with `SPEND_LIMIT_EXCEEDED`. Authorization at exact limit is approved
  (all other conditions met). Audit event present with old and new limit values.
- [ ] **Testable:** Integration — set limit to 1000; attempt authorization for 1001 (declined)
  and 1000 (approved). Unit — reject float, negative, non-EUR inputs.
- [ ] **Compliant:** Limit change audited. PSD2 Art. 68.

---

#### TASK-4.2 — Set per-month spend limit

**Refs:** OBJ-4

An authenticated cardholder can set or update the per-month limit on their own card.
Semantics identical to TASK-4.1 except the limit applies to the cumulative spend
within the current calendar month.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| New limit set below current month's accumulated spend | Limit is accepted and stored; existing settled transactions are unaffected; all new authorizations declined until accumulator resets |
| Limit set to zero mid-month | All new authorizations declined immediately |

**Definition of Done:**
- [ ] **Observable:** Card read returns updated `per_month_limit` and current
  `month_spend_accumulated`. Authorization causing accumulated spend to exceed monthly limit
  is declined with `MONTHLY_LIMIT_EXCEEDED`. Audit event present.
- [ ] **Testable:** Integration — set monthly limit; simulate authorizations approaching and
  exceeding limit; verify declines.
- [ ] **Compliant:** Limit change audited. PSD2 Art. 68.

---

#### TASK-4.3 — Reset monthly spend accumulator

**Refs:** OBJ-4

At 00:00 UTC on the first calendar day of each month, the per-month spend accumulator
for all non-cancelled, non-failed cards resets to `{ amount: 0, currency: "EUR" }`.
This is a system-initiated operation.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Card transitions to `CANCELLED` before reset | Reset skipped for that card |
| Reset job fails partway through | Job must be resumable without double-resetting any card (idempotent per card) |

**Definition of Done:**
- [ ] **Observable:** After reset, `month_spend_accumulated` reads `{ amount: 0, currency: "EUR" }`
  for all eligible cards. Authorizations previously declined for monthly limit are approved
  after reset (other conditions met).
- [ ] **Testable:** Integration — exhaust monthly limit; simulate month boundary; verify
  accumulator zeroed and new authorization approved.
- [ ] **Compliant:** Internal observability event recommended (not a card state-change audit event).

---

#### TASK-4.4 — Enforce spend limits at Stripe authorization time

**Refs:** OBJ-4

When Stripe calls the authorization webhook, the system evaluates per-transaction and
per-month limits in order and returns an approval or decline decision to Stripe.

**Evaluation order:**

1. Card state check — if `FROZEN` or `CANCELLED`, decline before limit evaluation.
2. Per-transaction limit — if `authorization.amount > per_transaction_limit.amount`, decline.
3. Per-month limit — if `(accumulated + authorization.amount) > per_month_limit.amount`, decline.
4. No limit configured for a dimension — that dimension is not a constraint.

**Edge cases:**

| Scenario | Decline code |
|---|---|
| Card is `FROZEN` | `CARD_FROZEN` |
| Authorization amount exceeds per-transaction limit | `SPEND_LIMIT_EXCEEDED` |
| Authorization would exceed per-month limit | `MONTHLY_LIMIT_EXCEEDED` |
| Zero per-transaction limit (including zero-value authorization) | `SPEND_LIMIT_EXCEEDED` |
| No limit configured | Approved (subject to card state check) |

**Definition of Done:**
- [ ] **Observable:** Stripe authorization webhook responses reflect correct decision codes for
  each scenario. Decline events are logged (without PAN) for observability.
- [ ] **Testable:** Integration — configure limits; simulate Stripe authorization webhooks at
  boundary values; verify response codes.
- [ ] **Compliant:** Limit enforcement observable and auditable. PSD2 Art. 68, PCI-DSS Req. 10.

---

### OBJ-5: Transaction Viewing

---

#### TASK-5.1 — List transactions for a card (paginated)

**Refs:** OBJ-5

An authenticated cardholder or ops role can retrieve a paginated list of transactions
for a given card. Default page size: 50. Maximum page size: 200. Ordered by
`authorized_at` descending. Only transactions within the 13-month retention window
are returned.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Card belongs to a different cardholder | `403 Forbidden` |
| Card has no transactions | Return empty array `[]` |
| `page_size` > 200 | `400 Bad Request`, code `PAGE_SIZE_EXCEEDED` |
| Card is `CANCELLED` | Historical data still accessible within retention window |

**Definition of Done:**
- [ ] **Observable:** Response contains `data: [...]`, `pagination: { next_cursor, total_count }`.
  Items include `transaction_id`, `amount`, `currency: "EUR"`, `status`, `merchant_name`,
  `authorized_at`. No PAN or CVV in any field.
- [ ] **Testable:** Integration — create transactions; paginate; verify ordering, field presence,
  PAN absence.
- [ ] **Compliant:** 13-month window enforced. PSD2 Art. 67(1). No PAN exposed. PCI-DSS Req. 3.3.

---

#### TASK-5.2 — Get transaction detail

**Refs:** OBJ-5

An authenticated cardholder or ops role can retrieve full detail of a single transaction
by `transaction_id`. The `stripe_transaction_id` field is visible to ops roles only;
it is omitted from cardholder responses.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| `transaction_id` not found | `404 Not Found` |
| `transaction_id` belongs to another cardholder's card | `403 Forbidden` (not `404` — avoids confirming existence to unauthorized caller) |
| Transaction older than 13 months | `404 Not Found` (expired from retention) |

**Definition of Done:**
- [ ] **Observable:** Cardholder response omits `stripe_transaction_id`. Ops response includes it.
  `settled_at` is `null` for `AUTHORIZED` transactions.
- [ ] **Testable:** Unit — verify field redaction by role. Integration — cross-cardholder access
  returns `403`.
- [ ] **Compliant:** Minimum data exposure by role. GDPR Art. 25, PCI-DSS Req. 7.

---

#### TASK-5.3 — Sync transactions from Stripe webhooks

**Refs:** OBJ-5

Process Stripe `issuing_transaction.created` and `issuing_transaction.updated` webhooks
to create or update transaction records. New `AUTHORIZED` transactions are written
immediately. Settlement updates `status` to `SETTLED` and populates `settled_at`.
Stripe event `id` is the deduplication key.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Duplicate webhook delivery | Idempotent on Stripe event `id`; no duplicate record |
| Webhook for unknown `stripe_card_id` | Log warning; return `200 OK` to Stripe |
| Settlement webhook for a `CANCELLED` card | Transaction still written; settled amounts are not voided by cancellation |
| Settlement webhook arrives before authorization webhook (out-of-order) | Upsert by `stripe_transaction_id`; create record directly in `SETTLED` state |

**Definition of Done:**
- [ ] **Observable:** Transaction appears in the list API within P95 ≤ 5 s of Stripe webhook
  delivery. `status` reflects `AUTHORIZED` or `SETTLED` correctly.
- [ ] **Testable:** Integration — send mock Stripe webhook; poll list API; measure latency.
  Send duplicate; verify single record.
- [ ] **Compliant:** Webhook signature validated. PCI-DSS Req. 10.

---

#### TASK-5.4 — Enforce 13-month transaction retention boundary

**Refs:** OBJ-5

Transactions with `authorized_at` older than 13 calendar months from the request date
must not be returned by the list or detail endpoints.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Transaction `authorized_at` = 13 months + 1 day ago | Not returned by list or detail |
| Transaction `authorized_at` exactly 13 months ago | Returned (boundary is inclusive of the 13th month) |

**Definition of Done:**
- [ ] **Observable:** List endpoint returns no transactions outside the 13-month window. Detail
  endpoint returns `404` for expired transactions.
- [ ] **Testable:** Integration — insert transaction with `authorized_at` = 13 months + 1 day ago;
  verify excluded from list and detail.
- [ ] **Compliant:** 13-month minimum met (PSD2 Art. 67(1)); expiry aligns with storage limitation
  principle (GDPR Art. 5(1)(e)).

---

### OBJ-6: Audit Trail

---

#### TASK-6.1 — Define and enforce audit event schema

**Refs:** OBJ-6

All audit events must conform to the schema in §7.3. The audit event writer validates
required fields before persisting. Any event failing validation causes the originating
write operation to be rolled back.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| `reason_code` omitted on a compliance-officer action | Schema validation fails; write rejected; `422 REASON_CODE_REQUIRED` returned to caller |
| `actor_id` not extractable from JWT | Write rejected; operation does not proceed |

**Definition of Done:**
- [ ] **Observable:** Every audit event in the log has all required fields populated. No event
  with null `actor_id` or null `timestamp` exists.
- [ ] **Testable:** Unit — schema validator rejects each missing-required-field case. Integration —
  validation failure rolls back the originating write.
- [ ] **Compliant:** Audit events meet minimum record fields. PSD2 Art. 82, PCI-DSS Req. 10.

---

#### TASK-6.2 — Guarantee audit event immutability

**Refs:** OBJ-6

Audit events, once written, must not be modifiable or deletable by application-layer
business logic or any role within the card service. Immutability is enforced at the
storage layer (no `UPDATE` / `DELETE` grants for the card service's database user).

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Attempted `UPDATE` on audit event | Storage-layer authorization error; error logged as security alert |
| GDPR Art. 17 erasure request for a user | `actor_id` is pseudonymized (replaced with a stable hash); event is not deleted. Legal obligation (PSD2 Art. 82, 4AMLD Art. 40) overrides erasure right per GDPR Art. 17(3)(b) |

**Definition of Done:**
- [ ] **Observable:** No audit event has a modified `timestamp`, `action`, or `actor_id` after
  initial write. Storage access logs show no `UPDATE` or `DELETE` on the audit table.
- [ ] **Testable:** Integration — attempt direct `UPDATE` on audit table as card-service DB user;
  verify rejection.
- [ ] **Compliant:** Immutability satisfies PSD2 Art. 82, PCI-DSS Req. 10.3.

---

#### TASK-6.3 — Deliver audit events within SLO

**Refs:** OBJ-6

Audit events must be queryable via the internal audit API within P95 ≤ 2 seconds of
the triggering write operation.

**Edge cases:**

| Scenario | Expected behavior |
|---|---|
| Transactional write | Event available immediately on transaction commit; 2 s SLO is achievable |
| Outbox-based delivery | Outbox processor must consume and publish within the SLO window |

**Definition of Done:**
- [ ] **Observable:** Monitoring shows P95 audit event delivery latency ≤ 2 s over a rolling
  1-hour window.
- [ ] **Testable:** Performance — generate 100 write operations under load; measure time from
  write request to audit event queryable; verify P95 ≤ 2 s.
- [ ] **Compliant:** SLO supports operational responsiveness. PSD2 Art. 82.

---

### OBJ-7: Access Control

---

#### TASK-7.1 — Validate JWT on every request

**Refs:** OBJ-7

Every API request must present a valid JWT in `Authorization: Bearer <token>`. Validate
`exp`, `iss`, `aud`, and `role` claims. Reject before any business logic executes.

**Edge cases:**

| Scenario | Expected response |
|---|---|
| Missing `Authorization` header | `401 Unauthorized`, code `TOKEN_MISSING` |
| Expired token | `401 Unauthorized`, code `TOKEN_EXPIRED` |
| Invalid signature | `401 Unauthorized`, code `TOKEN_INVALID` |
| Unknown or missing `role` claim | `403 Forbidden`, code `ROLE_UNKNOWN` |

**Definition of Done:**
- [ ] **Observable:** All invalid token scenarios return the correct 401/403 codes before any
  card data is accessed.
- [ ] **Testable:** Unit — JWT validation tested for each invalid case. Integration — verify no
  card data present in error responses.
- [ ] **Compliant:** Authentication enforced at every entry point. PCI-DSS Req. 7, GDPR Art. 25.

---

#### TASK-7.2 — Enforce cardholder ownership on card operations

**Refs:** OBJ-7

A cardholder may only read or mutate cards where `card.user_id = JWT.user_id`. This
check must occur after authentication and before any business logic. Never trust a
client-supplied `user_id` in the request body; always derive from JWT.

**Edge cases:**

| Scenario | Expected response |
|---|---|
| Cardholder provides a valid `card_id` belonging to another user | `403 Forbidden` (not `404` — avoids confirming existence) |
| `user_id` in request body differs from JWT claim | Ignore body value; use JWT claim |

**Definition of Done:**
- [ ] **Observable:** Cross-cardholder access attempt on any endpoint returns `403`. No data
  from another user's card is present in the response.
- [ ] **Testable:** Integration — create two cardholders; cardholder A attempts read/mutate on
  cardholder B's card; verify `403` on all endpoints.
- [ ] **Compliant:** Ownership check satisfies minimum access control. PCI-DSS Req. 7, GDPR Art. 25.

---

#### TASK-7.3 — Enforce read-only analyst permissions

**Refs:** OBJ-7

A user with role `ops:analyst` may call read endpoints (card state, transaction list,
transaction detail) but must receive `403 Forbidden` on all write endpoints.

**Definition of Done:**
- [ ] **Observable:** Analyst can retrieve card state and transactions. All write attempts
  return `403`.
- [ ] **Testable:** Integration — authenticate as analyst; attempt each write operation; verify
  `403`. Verify read operations succeed.
- [ ] **Compliant:** Least-privilege access enforced. PCI-DSS Req. 7.

---

#### TASK-7.4 — Enforce compliance officer permissions

**Refs:** OBJ-7

A user with role `ops:compliance` may freeze, unfreeze, and cancel cards (all require
`reason_code`). May read card state and transactions. Cannot create cards. Cannot set
spend limits.

**Edge cases:**

| Operation | Expected response |
|---|---|
| Create card | `403 Forbidden` |
| Set spend limit | `403 Forbidden` |
| Freeze / unfreeze / cancel without `reason_code` | `422 REASON_CODE_REQUIRED` |

**Definition of Done:**
- [ ] **Observable:** Freeze, unfreeze, and cancel succeed with `reason_code`. Create and
  set-limit attempts return `403`.
- [ ] **Testable:** Integration — authenticate as compliance officer; test each permitted and
  forbidden operation.
- [ ] **Compliant:** Role boundary enforced. PCI-DSS Req. 7, 4AMLD Art. 40.
