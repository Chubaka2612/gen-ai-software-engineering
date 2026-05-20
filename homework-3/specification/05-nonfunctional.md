> **Virtual Card Lifecycle — Feature Specification**
> *Non-functional requirements (latency, consistency, retention), data model conventions, and regulatory reference table.*
> [README](../README.md) | **← Prev** [04-objectives.md](04-objectives.md) | **Next →** [06-implementation.md](06-implementation.md)

---

# §6–8 Non-Functional Requirements, Data Model, and Regulatory References

---

## 6. Non-Functional Requirements

All targets below are **assumed** unless noted. Label as assumed in any derived
implementation plan.

### 6.1 Latency (P95)

| Operation | Target | Justification |
|---|---|---|
| Read — card state, transaction list/detail | ≤ 200 ms | Standard neobank API SLO |
| Write — freeze, unfreeze, cancel, set limit | ≤ 500 ms | Includes optimistic lock round-trip |
| Card creation end-to-end (request → ACTIVE) | ≤ 3 s | Stripe Issuing provisioning latency |
| Transaction sync (Stripe webhook → API-visible) | ≤ 5 s | Stripe webhook delivery + processing |
| Audit event delivery (write → queryable) | ≤ 2 s | PSD2 operational expectation |

### 6.2 Consistency

| Property | Target |
|---|---|
| Read-after-write consistency | Eventual, within 500 ms |
| Concurrent write conflict detection | Via optimistic locking; losing writer receives `409 Conflict` |
| Idempotent write TTL | 24 hours (assumed) |

### 6.3 Data Retention

| Data | Retention | Authority |
|---|---|---|
| Transaction records | 13 months from `authorized_at` | PSD2 Art. 67(1) |
| Audit events | 5 years (assumed; aligns with AML record-keeping) | PSD2 Art. 82, 4AMLD Art. 40 |
| PAN / CVV | Not stored in application layer; delivered once via Stripe vault | PCI-DSS Req. 3.3 |

---

## 7. Data Model Conventions

### 7.1 Money

All monetary amounts are represented as BIGINT in the minor unit of the currency,
accompanied by an ISO 4217 currency code. Floats are forbidden in all representations.

```
{ "amount": 1500, "currency": "EUR" }  →  €15.00
{ "amount": 0,    "currency": "EUR" }  →  €0.00  (valid zero-limit)
```

### 7.2 Sensitive Card Data

| Field | Stored in application layer? | Notes |
|---|---|---|
| PAN (full 16-digit) | Never | Delivered once by Stripe vault; not persisted |
| CVV / CVC | Never | Not persisted post-authorization |
| `last4` | Yes | Display and logging permitted |
| `brand` | Yes | e.g., `"Visa"`, `"Mastercard"` |
| `stripe_card_id` | Yes (internal only) | Opaque Stripe reference; not surfaced to cardholder |

### 7.3 Audit Event Schema

Every state-changing operation produces an event containing at minimum:

| Field | Type | Required | Notes |
|---|---|---|---|
| `event_id` | UUID | Yes | Globally unique |
| `actor_id` | string | Yes | `user_id`, `ops_user_id`, or `"SYSTEM"` |
| `actor_role` | enum | Yes | `CARDHOLDER` \| `OPS_ANALYST` \| `OPS_COMPLIANCE` \| `SYSTEM` |
| `action` | enum | Yes | e.g., `CARD_CREATED`, `CARD_FROZEN`, `LIMIT_UPDATED` |
| `card_id` | UUID | Yes | Internal card identifier |
| `before_state` | object | Yes | State snapshot before action |
| `after_state` | object | Yes | State snapshot after action |
| `request_id` | UUID | Yes | Correlates to the originating API request |
| `timestamp` | ISO-8601 UTC | Yes | e.g., `2026-05-19T10:00:00.000Z` |
| `reason_code` | string | Conditional | Required for all compliance-officer actions; e.g., `"AML_REVIEW"` |

### 7.4 Idempotency

All write operations must accept an `Idempotency-Key` request header (UUID).

| Scenario | Behavior |
|---|---|
| Duplicate key within 24-hour TTL, same payload | Return original response; no second operation executed |
| Duplicate key within 24-hour TTL, different payload | `422 Unprocessable Entity`, code `IDEMPOTENCY_KEY_CONFLICT` |
| Key presented after 24-hour TTL | Treat as new request; prior cached result is expired |
| No key provided | `400 Bad Request`, code `IDEMPOTENCY_KEY_REQUIRED` |

---

## 8. Regulatory References

| Regulation | Article | Constraint |
|---|---|---|
| PSD2 (EU 2015/2366) | Art. 67(1) | Account information must be accessible for at least 13 months |
| PSD2 (EU 2015/2366) | Art. 68 | Issuer must immediately block a payment instrument on cardholder request; cardholder must be able to set spending limits |
| PSD2 (EU 2015/2366) | Art. 82 | Payment service providers must maintain records sufficient to demonstrate compliance |
| GDPR (EU 2016/679) | Art. 5(1)(e) | Personal data must not be retained longer than necessary for the specified purpose |
| GDPR (EU 2016/679) | Art. 17(3)(b) | Right to erasure does not apply where retention is required to comply with a legal obligation |
| GDPR (EU 2016/679) | Art. 25 | Data protection by design and by default |
| GDPR (EU 2016/679) | Art. 30 | Records of processing activities must be maintained |
| PCI-DSS v4.0 | Req. 3.3 | PAN must not be stored after authorization |
| PCI-DSS v4.0 | Req. 7 | Access to system components and cardholder data must be restricted by business need |
| PCI-DSS v4.0 | Req. 10 | All access to network resources and cardholder data must be logged and monitored |
| 4AMLD (EU 2015/849) | Art. 40 | AML records must be retained for 5 years |
