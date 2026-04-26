# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**AiCraft.Banking** — a REST API for banking transactions built with .NET 8 Web API (controller-based).  
Source lives in `homework-1/src/AiCraft.Banking/`.

## Build & Run

```bash
cd homework-1/src/AiCraft.Banking
dotnet build
dotnet run
```

## Architecture

The project follows a strict layered separation. Read [PLAN.md](../homework-1/PLAN.md) for the full structure, endpoint list, service signatures, DTO shapes, validation rules, and edge cases before making changes.

### Layer rules

| Layer | Responsibility | Must NOT |
|-------|---------------|----------|
| `Controllers/` | HTTP wiring: routing, status codes, returning results | Contain any business logic |
| `Services/TransactionService.cs` | All business logic, validation, in-memory storage | Know about HTTP types |
| `DTOs/` | Public API contract (request/response shapes) | Reference domain models directly in responses |
| `Models/` | Internal domain types | Leak into controller responses |

### TransactionService is a singleton

It owns a `private readonly List<Transaction> _transactions` and is registered once in `Program.cs`. All mutations must be wrapped in a `lock(_transactions)` — `List<T>` is not thread-safe under concurrent requests.

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

## Reference Reading

- [Clean Architecture (Microsoft docs)](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures) — [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture)
- [Controller-based APIs in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/) — [dotnet/aspnetcore](https://github.com/dotnet/aspnetcore)
- [ProblemDetails in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors) — [dotnet/aspnetcore (ProblemDetails source)](https://github.com/dotnet/aspnetcore/blob/main/src/Mvc/Mvc.Core/src/ProblemDetails.cs)
- [eShopOnWeb — Microsoft reference app for ASP.NET Core architecture](https://github.com/dotnet-architecture/eShopOnWeb)
- [ISO 4217 currency codes](https://www.iso.org/iso-4217-currency-codes.html)