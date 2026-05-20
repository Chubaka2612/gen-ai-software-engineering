> **Virtual Card Lifecycle — Feature Specification**
> *Consolidated cross-cutting edge cases and failure modes with task references and expected behaviors.*
> [README](../README.md) | **← Prev** [07-tasks.md](07-tasks.md)

---

# §13 Edge Cases and Failure Mode Summary

This section consolidates cross-cutting edge cases defined inline in each task.

| Edge Case | Task(s) | Expected Behavior |
|---|---|---|
| Concurrent freeze/unfreeze on same card | TASK-2.5 | Optimistic lock; loser gets `409 CONCURRENT_MODIFICATION` |
| Ops and cardholder act on same card simultaneously | TASK-2.5, TASK-2.2 | Same optimistic lock; one succeeds, one retries with fresh state |
| In-flight transaction after freeze | TASK-2.6 | Pre-freeze authorized amounts settle normally |
| In-flight transaction after cancellation | TASK-3.1 | Same as freeze: pre-cancel authorized amounts settle normally |
| Zero per-transaction limit | TASK-4.1, TASK-4.4 | All authorizations declined with `SPEND_LIMIT_EXCEEDED` |
| Monthly limit set below current accumulator | TASK-4.2 | Accepted; future authorizations declined until month resets |
| Currency ≠ EUR in limit field | TASK-4.1 | `422 UNSUPPORTED_CURRENCY` |
| Stripe provisioning failure / timeout | TASK-1.2 | Card → `PROVISIONING_FAILED`; audit event generated |
| Duplicate Stripe webhook delivery | TASK-1.3, TASK-5.3 | Idempotent on Stripe event `id`; no duplicate state change |
| Settlement webhook arrives before authorization webhook | TASK-5.3 | Upsert by `stripe_transaction_id`; record created in `SETTLED` state |
| GDPR erasure vs. PSD2 / 4AMLD retention conflict | TASK-6.2 | Pseudonymize `actor_id` in audit events; legal obligation overrides erasure per GDPR Art. 17(3)(b) |
| Single-use card: settlement after manual cancellation | TASK-3.4 | Card already `CANCELLED`; system webhook processing is idempotent |
| Single-use card: settlement while `FROZEN` | TASK-3.4 | System cancels `FROZEN` → `CANCELLED`; single-use lifecycle supersedes freeze |
| Read of transactions on a cancelled card | TASK-5.1 | Historical data accessible within 13-month window |
| Audit write fails | TASK-1.5, TASK-6.1 | Originating write rolled back; no card state change without audit event |
