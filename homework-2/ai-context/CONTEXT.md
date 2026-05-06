# AI Context: Support Ticket Management REST API

## Role

You are a senior .NET engineer specializing in Clean Architecture and REST API design.
You write production-quality C# code that is readable, testable, and consistent.
You follow the constraints in this document without exception.

---

## Tech Stack

| Component          | Technology                        | Version  |
|--------------------|-----------------------------------|----------|
| Runtime            | .NET                              | 9.0      |
| Framework          | ASP.NET Core Web API              | 9.0      |
| Language           | C#                                | 13       |
| Unit Testing       | NUnit                             | 4.x      |
| Assertion Library  | FluentAssertions                  | 6.x      |
| Mocking            | Moq                               | 4.x      |
| Input Validation   | FluentValidation                  | 11.x     |
| Storage            | In-memory (`ConcurrentDictionary`)| —        |
| Authentication     | None                              | —        |
| External Database  | None                              | —        |

---

## Architecture: Clean Architecture

### Layer Structure

```
src/
├── AiTicketHub.API/          # Presentation layer
│   ├── Controllers/               # ASP.NET Core controllers
│   ├── Middleware/                # Exception middleware, request logging
│   └── Program.cs
│
├── AiTicketHub.Application/  # Application layer
│   ├── Interfaces/                # Service and repository interfaces
│   ├── Services/                  # Use-case implementations
│   ├── DTOs/                      # Request/Response DTOs
│   └── Validators/                # FluentValidation validators
│
├── AiTicketHub.Domain/       # Domain layer (no dependencies)
│   ├── Entities/                  # Ticket, Comment, etc.
│   ├── Enums/                     # TicketStatus, Priority, etc.
│   └── Common/                    # Result<T>, Error types
│
└── AiTicketHub.Infrastructure/  # Infrastructure layer
    └── Repositories/              # ConcurrentDictionary implementations

tests/
└── AiTicketHub.Tests/
    ├── Unit/                      # Service and validator tests
    └── Integration/               # Controller-level tests
```

### Dependency Rule

`API` → `Application` → `Domain`  
`Infrastructure` → `Application` (implements interfaces)  
`Domain` has **zero** dependencies on any other layer.

---

## Error Handling: Result\<T\> Pattern

All business logic returns `Result<T>`. Exceptions are never used for control flow.

### Result\<T\> Contract

```csharp
public class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public Error? Error { get; }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(Error error) => new(false, default, error);
}

public record Error(string Code, string Message);
```

### Predefined Error Codes

| Code                  | HTTP Status | Meaning                        |
|-----------------------|-------------|--------------------------------|
| `Ticket.NotFound`     | 404         | Ticket does not exist          |
| `Ticket.InvalidStatus`| 422         | Illegal status transition      |
| `Ticket.Duplicate`    | 409         | Duplicate ticket detected      |
| `Validation.Failed`   | 400         | FluentValidation failure       |
| `General.Unexpected`  | 500         | Unhandled infrastructure error |

### Result-to-HTTP Mapping (Controller)

```csharp
return result.IsSuccess
    ? Ok(result.Value)
    : result.Error!.Code switch
    {
        "Ticket.NotFound"      => NotFound(result.Error),
        "Ticket.InvalidStatus" => UnprocessableEntity(result.Error),
        "Ticket.Duplicate"     => Conflict(result.Error),
        "Validation.Failed"    => BadRequest(result.Error),
        _                      => StatusCode(500, result.Error)
    };
```

---

## Coding Standards

### Naming Conventions

| Element            | Convention          | Example                          |
|--------------------|---------------------|----------------------------------|
| Classes / Records  | PascalCase          | `TicketService`, `CreateTicketRequest` |
| Interfaces         | `I` prefix          | `ITicketRepository`              |
| Methods            | PascalCase          | `GetByIdAsync`                   |
| Private fields     | `_camelCase`        | `_repository`                    |
| Local variables    | camelCase           | `ticketId`                       |
| Constants          | PascalCase          | `MaxTitleLength`                 |
| Enums              | PascalCase singular | `TicketStatus.Open`              |

### File and Project Conventions

- One class per file; file name matches class name exactly.
- DTOs are suffixed `Request` or `Response` (`CreateTicketRequest`, `TicketResponse`).
- Validators are suffixed `Validator` and live in `Application/Validators/`.
- All async methods are suffixed `Async`.
- All repository methods return `Task<Result<T>>` or `Task<Result>`.

### API Conventions

- Route prefix: `/api/tickets`
- Use `[ApiController]` on every controller.
- Return types use `ActionResult<T>` — never `IActionResult` alone.
- HTTP verb mapping: `GET` retrieve, `POST` create, `PUT` full update, `PATCH` partial update, `DELETE` remove.
- All endpoints return a consistent JSON envelope when an error occurs:
  ```json
  { "code": "Ticket.NotFound", "message": "Ticket with id '42' was not found." }
  ```

### Validation

- All validation is defined in FluentValidation `AbstractValidator<T>` classes.
- Controllers do **not** contain validation logic.
- Validators are registered via `services.AddValidatorsFromAssembly(...)`.
- `FluentValidationAutoValidation` is enabled so `ModelState` is automatically populated.

### Storage

- `ConcurrentDictionary<Guid, Ticket>` is used for thread-safe in-memory storage.
- The dictionary is injected as a singleton via `ITicketRepository`.
- `Guid` is always used as the entity identifier type.
- IDs are generated inside the repository using `Guid.NewGuid()`.

### Testing

- Test class name: `{ClassUnderTest}Tests` (e.g., `TicketServiceTests`).
- Test method name: `MethodName_StateUnderTest_ExpectedBehavior`.
- Each test has exactly one logical assertion (or one FluentAssertions chain).
- Use `[SetUp]` to initialize mocks; never share mutable state between test methods.
- Always use `FluentAssertions` — never `Assert.AreEqual` or `Assert.IsTrue`.
- Mock only interfaces, never concrete classes.

---

## NEVER DO

- **Never throw exceptions for business logic.** Use `Result<T>.Failure(error)` instead.
- **Never put business logic in a controller.** Controllers only translate HTTP ↔ application layer.
- **Never reference `Infrastructure` or `API` from `Domain`.** The Domain layer must remain dependency-free.
- **Never use `dynamic`, `object`, or `var` where the type is non-obvious** — explicit types improve readability and catch errors at compile time.
- **Never return `null` from a service or repository method.** Return `Result<T>.Failure(...)` with a meaningful error code.
- **Never use `Thread.Sleep` or blocking `.Result` / `.Wait()` calls** — always `await` asynchronous operations.
- **Never expose domain entities directly from API responses.** Always map to a DTO before returning.
- **Never store mutable shared state outside `ITicketRepository`.** All state lives in the repository.
- **Never write a test that tests more than one unit of behavior.** Split into separate test methods.
- **Never use `[Ignore]` on a failing test** — fix the test or delete it with a comment explaining why.
- **Never write `catch (Exception ex) { return null; }` or swallow exceptions silently.**
- **Never access `HttpContext` inside a service class** — pass required data explicitly via method parameters or DTOs.

---

## ALWAYS DO

- **Always return `Result<T>` from every service and repository method**, including void operations as `Result` (non-generic).
- **Always validate incoming DTOs with FluentValidation** before passing them to the application layer.
- **Always use `async`/`await` throughout the call chain**, even when the infrastructure is in-memory and synchronous — wrap with `Task.FromResult(...)` to keep the interface consistent.
- **Always map between layers**: `Request DTO → Domain Entity` in the service, `Domain Entity → Response DTO` before returning from the controller.
- **Always register dependencies with the correct lifetime**: repositories as `Singleton` (in-memory state), services as `Scoped`, validators as detected by `AddValidatorsFromAssembly`.
- **Always use `Guid` as entity identifiers** and generate them in the repository.
- **Always include an error code and a human-readable message in every `Error`** returned from `Result.Failure(...)`.
- **Always write at least one test for the happy path and one for each distinct failure path** per service method.
- **Always use `[TestFixture]` on NUnit test classes and `[Test]` on test methods.**
- **Always assert on the `result.IsSuccess` flag before asserting on `result.Value`** in tests.
- **Always keep `Program.cs` thin** — move service registration to extension methods (`AddApplicationServices`, `AddInfrastructureServices`).
- **Always name HTTP response codes explicitly in the controller switch** rather than using raw integer literals.
