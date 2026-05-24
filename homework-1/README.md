# 🏦 Homework 1: Banking Transactions API

> **Student Name**: Viktoriia Skirko
> **Date Submitted**: April 26, 2026
> **AI Tools Used**: Claude Code (claude-sonnet-4-6)

---

## 📋 Project Overview

A REST API for banking transactions built with **.NET 9 / ASP.NET Core Web API** as part of the
AI-Assisted Development workshop series. The API supports creating and querying
transactions with filtering, account balance calculation, and account summaries.
All data is stored in-memory using a thread-safe `ConcurrentDictionary`.

---

## ✅ Features Implemented

### Core Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/transactions` | Create a new transaction |
| `GET` | `/transactions` | List all transactions with optional filters |
| `GET` | `/transactions/{id}` | Get a specific transaction by ID |
| `GET` | `/accounts/{accountId}/balance` | Get account balance per currency (Completed only) |
| `GET` | `/accounts/{accountId}/summary` | Get account transaction summary |

### Filtering (Task 3)

- Filter by account: `?accountId=ACC-12345`
- Filter by type: `?type=Transfer`
- Filter by date range: `?from=2026-01-01&to=2026-04-26`
- All filters are combinable

### Validation (Task 2)

- Amount must be positive with maximum 2 decimal places
- Account numbers must follow format `ACC-XXXXX` (alphanumeric)
- Currency must be one of the supported ISO 4217 codes: `USD`, `EUR`, `GBP`, `JPY`, `CHF`, `CAD`, `AUD`, `CNY`
- Transaction type must be `Deposit`, `Withdrawal`, or `Transfer`

### Additional Feature — Task 4, Option A: Account Summary

```
GET /accounts/{accountId}/summary
```

Returns:
- Total deposits
- Total withdrawals
- Transaction count
- Most recent transaction date

---

## 🏗️ Architecture

```
AiCraft.Banking/
├── Controllers/        # HTTP layer only — thin, no business logic
├── Services/           # All business logic, in-memory store, and startup seeder
├── Models/             # Domain types: Transaction, enums
├── DTOs/               # Request, response, and filter classes
└── Validators/         # FluentValidation validators per request type
```

### Key Architectural Decisions

| Decision | Rationale |
|----------|-----------|
| Controller/Service separation | Controllers handle HTTP only — all logic in TransactionService |
| `ConcurrentDictionary` for storage | Thread-safe in-memory store without locking overhead |
| FluentValidation | Consistent validation layer, auto-integrated with ASP.NET Core model binding |
| Plain C# classes for DTOs | Simple, serializer-friendly, no boilerplate |
| Typed seed data class | Compile-time safety over runtime JSON parsing |
| No Entity Framework | Out of scope — in-memory only per homework requirements |

---

## 🤖 AI-Assisted Development Strategy

This project was built using **Claude Code** following a structured phase-based approach
designed to produce clean, reviewable output at every step.

### Phase 1 — Foundation (before any code)

- Defined project conventions and architecture upfront in a single opening prompt
- Generated `PLAN.md` and waited for explicit approval before writing any code
- Created `.claude/CLAUDE.md` to persist conventions across all Claude Code sessions automatically
- Added reference repository URLs for code style anchoring:
  - [davidfowl/TodoApi](https://github.com/davidfowl/TodoApi) — .NET 8 idioms
  - [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture) — layering

### Phase 2 — Scaffold

- Generated solution structure from `PLAN.md` in a single prompt — no re-description needed
- Configured Swagger/OpenAPI as a dedicated infrastructure step (separate from feature work)
- Added `dotnet build` hook in `.claude/settings.json` to auto-verify every file change

### Phase 3 — Build (one concern at a time)

Built in strict dependency order so every step compiled cleanly:

1. Models and enums
2. DTOs (classes)
3. TransactionService — full implementation including in-memory store
4. Controllers — wired to service
5. FluentValidation validators
6. Filter logic on `GET /transactions`
7. Account summary endpoint (Task 4)

### Prompting Techniques Applied

| Technique | How it was used |
|-----------|----------------|
| Plan before code | `PLAN.md` approval gate — no code until architecture agreed |
| Persistent context | `CLAUDE.md` — conventions applied automatically every session |
| Negative constraints | "No EF, no AutoMapper, no unnecessary packages" |
| Completion gates | `dotnet build` hook — automatic verification on every file change |
| One concern per prompt | Never mixed infrastructure with feature work |
| Reference anchoring | GitHub repo URLs in `CLAUDE.md` for consistent style decisions |

### Observations on AI Tool Usage

- Claude Code excels at generating boilerplate and wiring dependency injection correctly
- Explicit architectural constraints in `CLAUDE.md` prevented drift toward over-engineered solutions
- Step-by-step prompting produced more predictable, reviewable output than single large prompts
- The auto-build hook caught compilation errors immediately, significantly reducing debugging time
- Separating planning (`PLAN.md`) from implementation ensured Claude executed the agreed design rather than inventing its own

---

## 📁 Project Structure

```
homework-1/
├── 📄 README.md
├── 📄 HOWTORUN.md
├── 📄 PLAN.md
├── 📂 .claude/
│   ├── CLAUDE.md
│   └── settings.json
├── 📂 src/
│   └── 📂 AiCraft.Banking/
│       ├── Controllers/
│       │   ├── TransactionsController.cs
│       │   └── AccountsController.cs
│       ├── Services/
│       │   ├── ITransactionService.cs
│       │   └── TransactionService.cs
│       ├── Models/
│       │   ├── Transaction.cs
│       │   ├── TransactionType.cs
│       │   └── TransactionStatus.cs
│       ├── DTOs/
│       │   ├── CreateTransactionRequest.cs
│       │   ├── TransactionResponse.cs
│       │   ├── TransactionFilter.cs
│       │   ├── AccountBalanceResponse.cs
│       │   ├── CurrencyBalance.cs
│       │   └── AccountSummaryResponse.cs
│       └── Validators/
│           └── CreateTransactionRequestValidator.cs
├── 📂 demo/
│   ├── run.sh
│   ├── sample-requests.http
│   └── sample-data.json
└── 📂 docs/
    └── 📂 screenshots/
        ├── ai-prompt-1.png
        ├── ai-prompt-2.png
        └── api-running.png
```

---

## 🧪 Sample API Requests

```bash
# Create a transaction
curl -X POST http://localhost:5092/transactions \
  -H "Content-Type: application/json" \
  -d '{
    "fromAccount": "ACC-12345",
    "toAccount": "ACC-67890",
    "amount": 100.50,
    "currency": "USD",
    "type": "Transfer"
  }'

# Get all transactions
curl http://localhost:5092/transactions

# Filter by account
curl "http://localhost:5092/transactions?accountId=ACC-12345"

# Filter by type and date range
curl "http://localhost:5092/transactions?type=Transfer&from=2026-01-01&to=2026-04-26"

# Get transaction by ID
curl http://localhost:5092/transactions/{id}

# Get account balance
curl http://localhost:5092/accounts/ACC-12345/balance

# Get account summary
curl http://localhost:5092/accounts/ACC-12345/summary
```

---

## 🔗 References

- [davidfowl/TodoApi](https://github.com/davidfowl/TodoApi) — .NET 9 minimal API patterns
- [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture) — clean layering conventions
- [FluentValidation documentation](https://docs.fluentvalidation.net) — validation patterns
- [Conventional Commits](https://www.conventionalcommits.org) — commit message standard

---

<div align="center">

*This project was completed as part of the AI-Assisted Development course.*

</div>