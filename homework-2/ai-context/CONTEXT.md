# AI Context: AiTicketHub Support Ticket API

## Tech Stack

| Component         | Technology                         | Version |
|-------------------|------------------------------------|---------|
| Runtime           | .NET                               | 9.0     |
| Framework         | ASP.NET Core Web API               | 9.0     |
| Language          | C#                                 | 13      |
| Unit Testing      | NUnit                              | 4.x     |
| Assertion Library | FluentAssertions                   | 6.x     |
| Mocking           | Moq                                | 4.x     |
| Input Validation  | FluentValidation                   | 11.x    |
| Storage           | In-memory (`ConcurrentDictionary`) | —       |

---

## Layer Structure

```
src/
├── AiTicketHub.API/              # Presentation layer
│   ├── Controllers/
│   ├── Middleware/
│   └── Extensions/               # DI extension methods
│
├── AiTicketHub.Application/      # Application layer
│   ├── Interfaces/               # Service and repository interfaces
│   ├── Services/
│   ├── DTOs/
│   └── Validators/
│
├── AiTicketHub.Domain/           # Domain layer (no dependencies)
│   ├── Entities/
│   ├── Enums/
│   └── Common/                   # Result<T>, Error
│
└── AiTicketHub.Infrastructure/
    └── Repositories/             # ConcurrentDictionary implementations

tests/
└── AiTicketHub.Tests/
    ├── Application/
    ├── Infrastructure/
    └── API/
```

### Dependency Rule

`API` → `Application` → `Domain`  
`Infrastructure` → `Application` (implements interfaces)  
`Domain` has **zero** dependencies on any other layer.

---

## Result\<T\> Pattern

All business logic returns `Result<T>`. Exceptions are never used for control flow.

### Contract

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

### Error Code Catalogue

| Code                   | HTTP Status | Meaning                        |
|------------------------|-------------|--------------------------------|
| `Ticket.NotFound`      | 404         | Ticket does not exist          |
| `Ticket.InvalidStatus` | 422         | Illegal status transition      |
| `Ticket.Duplicate`     | 409         | Duplicate ticket detected      |
| `Validation.Failed`    | 400         | FluentValidation failure       |
| `General.Unexpected`   | 500         | Unhandled infrastructure error |

### Result → HTTP Mapping

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

## API Conventions

- Route prefix: `/api/tickets`
- All errors use a uniform JSON envelope — no exceptions:
  ```json
  { "code": "Ticket.NotFound", "message": "Ticket with id '42' was not found." }
  ```

---

## Hard Constraints

- **Domain has no dependencies.** Never reference Application, Infrastructure, or API from Domain.
- **Never expose domain entities across the Application → API boundary.** DTOs only.
- **Never use exceptions for business errors.** Return `Result.Failure(error)`.
- **Never put business logic in controllers.** Controllers translate HTTP ↔ Application layer only.
- **Never access `HttpContext` inside a Service.** Pass required data via DTOs or method parameters.
- **Always return `Task<Result<T>>` from service and repository methods**, even when storage is synchronous — wrap with `Task.FromResult`.
- **Always map at layer boundaries**: Request DTO → Domain in the service; Domain → Response DTO before returning.
- **Repositories are Singleton; Services are Scoped.** Validators auto-registered via `AddValidatorsFromAssemblyContaining`.
