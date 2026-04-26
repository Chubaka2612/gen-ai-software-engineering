# AiCraft.Banking — Implementation Plan

## 1. Project Structure

```
homework-1/
├── src/
│   └── AiCraft.Banking/
│       ├── AiCraft.Banking.csproj       # project file, no extra packages
│       ├── Program.cs                   # DI registration, middleware pipeline
│       ├── Controllers/
│       │   ├── TransactionsController.cs  # POST /transactions, GET /transactions, GET /transactions/{id}
│       │   └── AccountsController.cs      # GET /accounts/{accountId}/balance, GET /accounts/{accountId}/summary
│       ├── Models/
│       │   ├── Transaction.cs           # domain entity
│       │   ├── TransactionType.cs       # enum: Deposit | Withdrawal | Transfer
│       │   └── TransactionStatus.cs     # enum: Pending | Completed | Failed
│       ├── Services/
│       │   └── TransactionService.cs    # singleton, owns List<Transaction>, all business logic
│       └── DTOs/
│           ├── CreateTransactionRequest.cs
│           ├── TransactionResponse.cs
│           ├── TransactionFilter.cs
│           ├── AccountBalanceResponse.cs
│           └── AccountSummaryResponse.cs
├── demo/
│   ├── run.bat                          # starts the API on Windows
│   └── sample-requests.http            # VS Code REST Client test file
├── docs/
│   └── screenshots/
├── README.md
├── HOWTORUN.md
└── PLAN.md
```

**Why this layout:**  
Controllers stay in their own folder and import only DTOs and the service interface. All domain objects live in `Models`. DTOs live in `DTOs` — they are the public contract, never exposed as raw `Transaction` objects. The service is the only place that touches the in-memory list.

---

## 2. Endpoint List

| Method | Route | What it does | Success response |
|--------|-------|--------------|-----------------|
| `POST` | `/transactions` | Creates a new transaction; sets Id, Timestamp, Status=Pending server-side | `201 Created` + `Location: /transactions/{id}` + `TransactionResponse` body |
| `GET` | `/transactions` | Returns all transactions, optionally filtered by accountId, type, from, to | `200 OK` + `TransactionResponse[]` |
| `GET` | `/transactions/{id}` | Returns one transaction by Guid | `200 OK` + `TransactionResponse` |
| `GET` | `/accounts/{accountId}/balance` | Computes net balance for an account across all Completed transactions | `200 OK` + `AccountBalanceResponse` |
| `GET` | `/accounts/{accountId}/summary` | Returns aggregate stats for an account | `200 OK` + `AccountSummaryResponse` |

---

## 3. Service Methods

```
CreateTransaction(CreateTransactionRequest request) → Transaction
    Validates input, builds Transaction, appends to list, returns it.

GetTransactions(TransactionFilter filter) → IEnumerable<Transaction>
    Applies zero or more filters; returns matching transactions.

GetTransactionById(Guid id) → Transaction?
    Linear search by Id; returns null if not found.

GetAccountBalance(string accountId) → AccountBalanceResponse?
    Returns null if accountId has no transactions.
    Balance = Σ amount where toAccount == accountId (Completed)
            − Σ amount where fromAccount == accountId (Completed)

GetAccountSummary(string accountId) → AccountSummaryResponse?
    Returns null if accountId has no transactions.
    Aggregates: TotalDeposits, TotalWithdrawals, TransactionCount, MostRecentTimestamp.
```

---

## 4. DTO Shapes

**CreateTransactionRequest** (POST body)
```json
{
  "fromAccount": "ACC-12345",
  "toAccount":   "ACC-67890",
  "amount":      100.50,
  "currency":    "USD",
  "type":        "Transfer"
}
```

**TransactionResponse** (all GET and POST responses)
```json
{
  "id":          "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "fromAccount": "ACC-12345",
  "toAccount":   "ACC-67890",
  "amount":      100.50,
  "currency":    "USD",
  "type":        "Transfer",
  "timestamp":   "2024-01-15T10:30:00+00:00",
  "status":      "Pending"
}
```

**TransactionFilter** (query string on GET /transactions)
```
?accountId=ACC-12345   — matches fromAccount OR toAccount
?type=Transfer
?from=2024-01-01
?to=2024-01-31
```
All parameters are optional and combinable.

**AccountBalanceResponse**
```json
{
  "accountId": "ACC-12345",
  "balance":   250.00,
  "currency":  "USD"
}
```
Note: balance is currency-agnostic for now (sums across all currencies). If multi-currency is needed later, this is the place to extend.

**AccountSummaryResponse**
```json
{
  "accountId":          "ACC-12345",
  "totalDeposits":      500.00,
  "totalWithdrawals":   250.00,
  "transactionCount":   4,
  "mostRecentTimestamp":"2024-01-15T10:30:00+00:00"
}
```

---

## 5. Validation Rules

- `Amount` must be greater than 0.
- `Amount` must have at most 2 decimal places.
- `FromAccount` must match `^ACC-[A-Z0-9]{5}$` (required for Withdrawal and Transfer; optional for Deposit).
- `ToAccount` must match `^ACC-[A-Z0-9]{5}$` (required for Deposit and Transfer; optional for Withdrawal).
- `FromAccount` and `ToAccount` must not be equal when `Type` is `Transfer`.
- `Currency` must be a member of the supported ISO 4217 allowlist (USD, EUR, GBP, JPY, CAD, CHF, AUD, CNY).
- `Type` must be a valid `TransactionType` enum value.
- All string fields (`FromAccount`, `ToAccount`, `Currency`) must not be null or whitespace when required.

---

## 6. Potential Edge Cases

- **Unknown account on balance/summary**: no transactions exist for that accountId — return `404` rather than a zero balance, to distinguish "account exists but empty" from "account never seen."
- **Multi-currency balance**: summing USD and EUR into one decimal is misleading — plan notes this; for now, sum all currencies together and document the limitation.
- **Deposit with no fromAccount / Withdrawal with no toAccount**: these fields are logically optional per type — validation must be type-aware, not just null-checking.
- **Transfer to self**: `fromAccount == toAccount` — must be rejected with `400`.
- **Amount precision**: `decimal` arithmetic is exact, but input deserialization from JSON `number` could introduce floating-point noise — parse and round to 2 decimal places on ingestion.
- **Invalid Guid in route**: `GET /transactions/not-a-guid` — ASP.NET Core model binding returns `400` automatically if the parameter is typed as `Guid`.
- **Filter date parsing**: `?from=not-a-date` — bind as `DateTimeOffset?` so the framework rejects malformed values before the service is called.
- **Concurrent writes**: `List<Transaction>` is not thread-safe — wrap mutations in a `lock` inside the service to prevent race conditions under concurrent requests.
- **Empty filter result**: `GET /transactions?type=Transfer` with no matches — return `200` with an empty array, not `404`.
- **Case sensitivity on Type/Status**: JSON deserialization should be case-insensitive for enum strings to accept `"transfer"` and `"Transfer"` equally.
