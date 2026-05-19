> **Virtual Card Lifecycle — Feature Specification**
> *High-level objective, scope boundary, and explicit exclusion list.*
> [README](../README.md) | **Next →** [02-actors.md](02-actors.md)

---

# §1–2 High-Level Objective and Scope

---

## 1. High-Level Objective

Enable EU cardholders to self-manage the full lifecycle of virtual payment cards —
create, freeze/unfreeze, set spend limits, view transactions, and cancel — within
a PSD2- and GDPR-compliant, out-of-PCI-CDE environment backed by Stripe Issuing,
denominated exclusively in EUR.

**Scope boundary:** This specification covers only the five operations named above
for virtual cards. It does not cover physical card issuance, 3DS/SCA flows,
dispute resolution, FX conversion, rewards, KYC/AML onboarding, card renewal,
PIN management, or push-to-wallet provisioning. See §2.2 for the complete
exclusion list.

---

## 2. Scope

### 2.1 In Scope

| Operation | Permitted Actor(s) | Reversible? |
|---|---|---|
| Create virtual card | Cardholder | N/A |
| Freeze card | Cardholder, Compliance Officer | Yes — unfreeze restores ACTIVE |
| Unfreeze card | Cardholder, Compliance Officer | Yes |
| Set spend limit (per-transaction, per-month) | Cardholder | Yes |
| View transactions (list + detail) | Cardholder, Read-Only Analyst, Compliance Officer | N/A (read-only) |
| Cancel card | Cardholder, Compliance Officer | No — permanent |

### 2.2 Explicitly Out of Scope

The following are intentionally excluded. Any work touching these areas requires
a separate specification:

1. Physical card issuance and delivery
2. 3DS / Strong Customer Authentication (SCA) flows
3. Chargebacks and dispute resolution
4. FX conversion and multi-currency support
5. Reward points and cashback
6. KYC / AML onboarding
7. Card renewal and re-issue
8. PIN management
9. Push-to-wallet provisioning (Apple Pay, Google Pay)
