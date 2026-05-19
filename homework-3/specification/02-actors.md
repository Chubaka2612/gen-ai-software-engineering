> **Virtual Card Lifecycle — Feature Specification**
> *Actor definitions, role claims, and authorization boundaries for all principals.*
> [README](../README.md) | **← Prev** [01-objective.md](01-objective.md) | **Next →** [03-state-machine.md](03-state-machine.md)

---

# §3 Actors and Roles

---

## 3. Actors and Roles

### 3.1 Cardholder

An authenticated end user who owns one or more virtual cards. May only read or
mutate cards linked to their own account. Authenticated via OAuth 2.0 + JWT;
the JWT `user_id` claim is used as `actor_id` in audit events.

### 3.2 Ops: Read-Only Analyst (role claim: `ops:analyst`)

An internal operator with read access to card state and transaction history.
Cannot initiate any write operation (create, freeze, cancel, or set limits).

### 3.3 Ops: Compliance Officer (role claim: `ops:compliance`)

An internal operator who may freeze, unfreeze, or cancel cards for compliance
and AML purposes. Cannot create cards. Cannot set spend limits on behalf of a
user. All compliance-officer write operations must include a `reason_code` in
the audit event.

### 3.4 System

The internal service acting on scheduled or event-driven triggers (e.g., single-use
card auto-cancellation, Stripe webhook processor, monthly accumulator reset).
`actor_id` in audit events: `"SYSTEM"`.
