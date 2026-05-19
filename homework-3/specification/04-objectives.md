> **Virtual Card Lifecycle — Feature Specification**
> *Seven mid-level objectives (OBJ-1 through OBJ-7), each observable, verifiable, and regulation-cited.*
> [README](../README.md) | **← Prev** [03-state-machine.md](03-state-machine.md) | **Next →** [05-nonfunctional.md](05-nonfunctional.md)

---

# §5 Mid-Level Objectives

---

## 5. Mid-Level Objectives

### OBJ-1 — Card Creation

A cardholder can create a virtual card (single-use or recurring-use) and receive
a card in `ACTIVE` state within P95 ≤ 3 seconds of the request.

**Verification:** An API read of the created card returns state `ACTIVE`, `last4`,
`brand`, `card_type`, and the configured limits (or null if none set at creation).
The Stripe dashboard reflects a provisioned card matching the returned internal
`stripe_card_id`.

**Regulation:** PCI-DSS Req. 3.3 — PAN must not be stored or logged post-provisioning.
PAN is delivered once via Stripe's PCI-compliant vault and is never written to
application-layer persistence.

---

### OBJ-2 — Freeze / Unfreeze

A cardholder or compliance officer can reversibly suspend and restore a virtual card.
A frozen card rejects all new authorization requests. Unfreezing restores the card
to `ACTIVE` with no data loss.

**Verification:** After freeze, any new authorization attempt returns a decline with
code `CARD_FROZEN`. After unfreeze, authorization attempts proceed normally. The
audit log contains a correctly paired freeze and unfreeze event, each with matching
`request_id` lineage and correct `before_state` / `after_state` values.

**Regulation:** PSD2 Art. 68 — issuer must be able to block a payment instrument
immediately upon request.

---

### OBJ-3 — Card Cancellation

A cardholder or compliance officer can permanently cancel a virtual card. No further
state changes are permitted after cancellation. In-flight authorized transactions
are not voided by cancellation; they settle normally.

**Verification:** After cancellation, card state reads `CANCELLED`. Any attempt to
freeze, unfreeze, or update limits on the card returns `CARD_CANCELLED` error. New
authorization attempts return a decline with code `CARD_CANCELLED`. Audit log
contains the cancellation event with `after_state: "CANCELLED"`.

**Regulation:** PSD2 Art. 68 — same immediate block obligation applies to cancellation.

---

### OBJ-4 — Spend Limits

A cardholder can set a per-transaction EUR limit and/or a per-month EUR limit on any
of their `ACTIVE` or `FROZEN` cards. Limits are enforced at authorization time.
A zero-value limit is a distinct and valid configuration that causes all authorization
attempts to be declined.

**Verification:** After setting `per_transaction_limit: { amount: 1000, currency: "EUR" }`,
an authorization for `{ amount: 1001, currency: "EUR" }` is declined with
`SPEND_LIMIT_EXCEEDED`; an authorization for `{ amount: 1000, currency: "EUR" }` is
approved (all other conditions met). Audit log contains the limit-change event with
`before_state` and `after_state` limit values. The per-month accumulator reads
`{ amount: 0, currency: "EUR" }` at 00:00 UTC on the first calendar day of each month.

**Regulation:** PSD2 Art. 68 — cardholder must have the ability to set spending limits
on their payment instrument.

---

### OBJ-5 — Transaction Viewing

A cardholder can retrieve a paginated list of transactions on their own cards and fetch
the detail of any individual transaction. Coverage includes both `AUTHORIZED` and
`SETTLED` statuses for up to 13 months of history. No PAN, CVV, or full card number
appears in any transaction response.

**Verification:** A request returns entries with status `AUTHORIZED` and/or `SETTLED`
for events up to 13 months prior to the request date. A transaction older than 13 months
is not returned. No response field contains a PAN or CVV. Transaction detail includes:
`transaction_id`, `card_id`, `amount` (BIGINT minor units + currency), `merchant_name`,
`status`, `authorized_at`, `settled_at` (nullable), and — for ops roles only —
`stripe_transaction_id`.

**Regulation:** PSD2 Art. 67(1) — account information must be accessible for at least
13 months. GDPR Art. 5(1)(e) — retention limited to what is necessary; 13 months is
the defined window.

---

### OBJ-6 — Audit Trail

Every state change (card creation, freeze, unfreeze, cancellation, limit update)
produces an immutable audit event delivered within P95 ≤ 2 seconds of the triggering
action. Audit events are append-only and cannot be modified or deleted within the
retention window.

**Verification:** For any write operation, querying the audit log within 2 seconds
returns exactly one event with the correct `action`, `actor_id`, `before_state`,
`after_state`, `request_id`, and `timestamp`. An attempt to modify or delete an audit
event fails at the storage layer with an authorization error.

**Regulation:** PSD2 Art. 82 — payment service providers must maintain records.
GDPR Art. 30 — records of processing activities. PCI-DSS Req. 10 — track and monitor
all access to cardholder data.

---

### OBJ-7 — Access Control

All operations enforce role-based authorization. A cardholder cannot act on another
cardholder's cards. Ops roles cannot create cards. Role claims are derived exclusively
from the JWT; no operation proceeds without a valid, unexpired token.

**Verification:** An authenticated cardholder's request to read or modify a card
belonging to a different user returns `403 Forbidden`. An `ops:analyst` attempt to
freeze a card returns `403 Forbidden`. An `ops:compliance` attempt to create a card
returns `403 Forbidden`. An expired or tampered JWT returns `401 Unauthorized` on
all endpoints.

**Regulation:** PCI-DSS Req. 7 — restrict access to system components and cardholder
data by business need. GDPR Art. 25 — data protection by design and by default.
