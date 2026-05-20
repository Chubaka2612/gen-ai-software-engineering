> **Virtual Card Lifecycle — Feature Specification**
> *Implementation guardrails, pre-conditions (beginning context), and post-conditions (ending context).*
> [README](../README.md) | **← Prev** [05-nonfunctional.md](05-nonfunctional.md) | **Next →** [07-tasks.md](07-tasks.md)

---

# §9–11 Implementation Notes, Beginning Context, and Ending Context

---

## 9. Implementation Notes (Guardrails for Builders)

These notes are not requirements — they are guardrails to prevent known failure
modes. They assume Stripe Issuing, OAuth 2.0 + JWT, and optimistic locking.

1. **PAN handling.** Never log, store, or transmit the full PAN through application-
   layer services. Use Stripe card object references (`stripe_card_id`) exclusively.
   Out-of-PCI-CDE by design.

2. **Optimistic locking.** Card state transitions must use a `card_version` integer.
   A write that finds `card_version` has changed since read must return `409 Conflict`,
   code `CONCURRENT_MODIFICATION`. Last-write-wins is forbidden.

3. **Stripe webhook processing.** Webhooks must be processed idempotently; the system
   must tolerate duplicate delivery. Use the Stripe event `id` as the deduplication key.
   Validate the webhook signature on every delivery before processing.

4. **Freeze and in-flight transactions.** A freeze does not void in-flight `AUTHORIZED`
   transactions. Those that reach settlement will settle normally. Only new authorization
   requests are declined on a frozen card.

5. **Single-use auto-cancellation.** The auto-cancel on first SETTLED transaction is
   triggered by the Stripe `issuing_transaction.created` webhook with `status: settled`.
   The system, not the cardholder, initiates this transition. The audit event must carry
   `actor_id: "SYSTEM"` and `reason_code: "SINGLE_USE_CONSUMED"`.

6. **Zero-limit enforcement.** A per-transaction limit of `{ amount: 0, currency: "EUR" }`
   must decline all authorization attempts, including zero-value authorizations. It is not
   the same as having no limit configured.

7. **Per-month accumulator reset.** The monthly spend accumulator resets to
   `{ amount: 0, currency: "EUR" }` at 00:00 UTC on the first calendar day of each month.
   This reset does not generate a card state-change audit event but should produce an
   internal observability event.

8. **JWT validation.** Validate `exp`, `iss`, `aud`, and role claims on every request.
   Do not cache decoded tokens beyond their `exp`. Never trust client-supplied `user_id`
   fields; always read identity from the JWT.

9. **Concurrent ops/cardholder actions.** If a compliance officer and a cardholder submit
   state-changing operations within the same optimistic-lock window, the operation that wins
   the version check succeeds; the other receives `409 Conflict` and must retry with fresh
   state.

10. **Audit event atomicity.** Audit events must be written transactionally with the state
    change or via an outbox pattern. A state change that succeeds without a corresponding
    audit event is a compliance defect and must be treated as a rollback condition.

---

## 10. Beginning Context (Pre-Conditions)

The following system state is assumed to exist before any work in this specification begins:

1. **User accounts.** A user identity and account management system exists. Each user has
   a stable `user_id` (UUID). KYC and onboarding are complete upstream; this spec does not
   implement onboarding.
2. **Authentication infrastructure.** An OAuth 2.0 authorization server is in place. JWTs
   are issued with `user_id`, `role`, `exp`, `iss`, and `aud` claims.
3. **Stripe account.** A Stripe Issuing-enabled account is configured. API keys and webhook
   endpoint signatures are provisioned and stored securely.
4. **Audit log store.** An append-only, immutable audit log datastore exists and is writable
   only by the audit event writer. Business-logic code has no direct `UPDATE` or `DELETE`
   grants on the audit table.
5. **EUR as operating currency.** The platform operates in EUR only. No multi-currency
   infrastructure is assumed.
6. **No virtual cards exist.** The card table is empty at the start of this feature build.

---

## 11. Ending Context (Post-Conditions)

After all tasks in this specification are implemented and verified:

1. A cardholder can create, freeze, unfreeze, set limits on, view transactions of, and cancel
   their own virtual cards via the API.
2. A compliance officer can freeze, unfreeze, and cancel any card, with a mandatory
   `reason_code` recorded in the audit event.
3. A read-only analyst can query card state and transaction history but cannot mutate any
   resource.
4. Every state change has a corresponding immutable audit event delivered within P95 ≤ 2 s.
5. No PAN or CVV is stored or logged anywhere in the application layer.
6. Transaction history is available for 13 months (PSD2 Art. 67(1)) and no longer
   (GDPR Art. 5(1)(e)).
7. All write operations are idempotent within a 24-hour TTL window.
8. Concurrent writes are resolved via optimistic locking with no silent data loss.
