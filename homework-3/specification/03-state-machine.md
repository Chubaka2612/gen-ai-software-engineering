> **Virtual Card Lifecycle — Feature Specification**
> *Card states, permitted transitions, and single-use vs recurring-use lifecycle rules.*
> [README](../README.md) | **← Prev** [02-actors.md](02-actors.md) | **Next →** [04-objectives.md](04-objectives.md)

---

# §4 Card State Machine

---

## 4. Card State Machine

### 4.1 States

| State | Description | Terminal? |
|---|---|---|
| `PENDING_PROVISIONING` | Card record created; awaiting Stripe confirmation | No |
| `ACTIVE` | Card is live and can authorize transactions | No |
| `FROZEN` | Card temporarily suspended; no new authorizations | No |
| `CANCELLED` | Permanently terminated; no further state changes permitted | Yes |
| `PROVISIONING_FAILED` | Stripe provisioning did not confirm within timeout | Yes |

### 4.2 Permitted Transitions

```
PENDING_PROVISIONING  →  ACTIVE               (Stripe webhook: issuing_card.created)
PENDING_PROVISIONING  →  PROVISIONING_FAILED  (Stripe webhook: card.failed OR 30s timeout)
ACTIVE                →  FROZEN               (cardholder or compliance officer)
ACTIVE                →  CANCELLED            (cardholder or compliance officer)
FROZEN                →  ACTIVE               (cardholder or compliance officer)
FROZEN                →  CANCELLED            (cardholder or compliance officer)
ACTIVE                →  CANCELLED            (SYSTEM — single-use auto-cancel after first SETTLED tx)
FROZEN                →  CANCELLED            (SYSTEM — single-use auto-cancel after first SETTLED tx)
```

All other transitions are invalid and must be rejected with a structured error
response. No silent state coercion.

### 4.3 Card Type Behavior

| Type | Behavior on first SETTLED transaction |
|---|---|
| `SINGLE_USE` | System auto-transitions card to `CANCELLED`; audit event generated with `actor_id: "SYSTEM"`, `reason_code: "SINGLE_USE_CONSUMED"` |
| `RECURRING` | No automatic state change on settlement |
