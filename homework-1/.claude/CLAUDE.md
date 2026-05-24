# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working in `homework-1/`.

## Project

**AiCraft.Banking** — a REST API for banking transactions built with .NET 8 Web API (controller-based).
Source lives in `homework-1/src/AiCraft.Banking/`.

## Build & Run

```bash
cd homework-1/src/AiCraft.Banking
dotnet build
dotnet run
```

## Hooks

`dotnet build` runs automatically after every file change via `.claude/settings.json`.
Do not manually run `dotnet build` unless explicitly asked.
If the build fails after an edit, fix it before proceeding to the next step.

## Architecture

The project follows a strict layered separation. Read [PLAN.md](../PLAN.md) for the full structure, endpoint list, service signatures, DTO shapes, validation rules, and edge cases before making changes.

### Layer rules

| Layer | Responsibility | Must NOT |
|-------|---------------|----------|
| `Controllers/` | HTTP wiring: routing, status codes, returning results | Contain any business logic |
| `Services/TransactionService.cs` | All business logic, validation, in-memory storage | Know about HTTP types |
| `DTOs/` | Public API contract (request/response shapes) | Reference domain models directly in responses |
| `Models/` | Internal domain types | Leak into controller responses |

### TransactionService

Registered as a singleton. Owns a `ConcurrentDictionary<Guid, Transaction>` — lock-free reads, O(1) lookup by Id. No manual locking needed.

## Coding Conventions

- `decimal` for every monetary amount — never `double` or `float`
- `Guid` for IDs, always generated server-side (never from the request body)
- `DateTimeOffset.UtcNow` for timestamps, set inside the service on creation
- Errors always use **ProblemDetails** — use `ValidationProblem()` for 400s and `NotFound()` / `Problem()` for others (built into `ControllerBase`)
- HTTP status codes: `201 + Location` on POST, `200` on GET, `400` on validation failure, `404` on not found
- JSON enum deserialization must be case-insensitive (`JsonStringEnumConverter` with `JsonNamingPolicy.CamelCase`)

## Validation Rules (enforced in TransactionService)

- `Amount` > 0 and has at most 2 decimal places
- `FromAccount` / `ToAccount` match `^ACC-[A-Z0-9]{5}$`
- `FromAccount` / `ToAccount` requirements are **type-aware**: Deposit may omit `FromAccount`; Withdrawal may omit `ToAccount`; Transfer requires both and they must differ
- `Currency` must be in the supported ISO 4217 allowlist: `USD, EUR, GBP, JPY, CAD, CHF, AUD, CNY`
- `Type` must deserialize to a valid `TransactionType` enum value

## No Unnecessary Dependencies

Do not add:
- Entity Framework or any ORM
- AutoMapper or any object-mapping library
- FluentValidation or any validation framework
- Any NuGet package that is not already in the `.csproj`

Use only what ships with .NET 8 Web API.

## Documentation

- Add XML `<summary>` comments to all public interfaces, methods, and classes
- Describe WHY or WHAT — never restate what the code already says
- Controllers: comment each action with the endpoint purpose and return codes
- Service interface: comment each method with inputs, outputs, and edge cases

```csharp
// Correct
/// <summary>
/// Returns the transaction with the given Id, or null if it does not exist.
/// </summary>

// Incorrect — do not do this
/// <summary>
/// Gets the transaction.
/// </summary>
```

## Reference Reading

- [Clean Architecture (Microsoft docs)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures) — [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)
- [Controller-based APIs in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/) — [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore)
- [ProblemDetails in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors) — [dotnet/aspnetcore (ProblemDetails source)](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Core/src/ProblemDetails.cs)
- [eShopOnWeb — Microsoft reference app for ASP.NET Core architecture](https://github.com/dotnet-architecture/eShopOnWeb)
- [ISO 4217 currency codes](https://www.iso.org/iso-4217-currency-codes.html)
