# AiTicketHub Support Ticket API

A .NET 9 REST API for managing customer support tickets with keyword-based auto-classification and bulk import.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

## Getting Started

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd homework-2
   ```

2. Build the solution:
   ```bash
   dotnet build AiTicketHub.sln
   ```
   Expected output: `Build succeeded.`

3. Run the API:
   ```bash
   dotnet run --project src/AiTicketHub/API/AiTicketHub.API.csproj
   ```
   Expected output: `Now listening on: http://localhost:5000`

4. Open the Swagger UI at `http://localhost:5000/swagger` to browse and try all endpoints interactively.

## Running Tests

Run the full test suite:

```bash
dotnet test AiTicketHub.sln
```

Expected output: `143 passed, 0 failed.`

Run with code coverage:

```bash
dotnet test AiTicketHub.sln --collect:"XPlat Code Coverage"
```

## Project Structure

```
AiTicketHub.sln
├── src/
│   └── AiTicketHub/
│       ├── Domain/          # Entities, enums, Result<T>, error catalogue
│       ├── Application/     # Service interfaces, DTOs, validators, services
│       ├── Infrastructure/  # Repository, parsers, keyword classifier
│       └── API/             # Controllers, Program.cs, Swagger config
├── tests/
│   └── AiTicketHub.Tests/
│       ├── Application/     # Service and validator unit tests
│       ├── Infrastructure/  # Repository, parser, and classifier unit tests
│       └── API/             # Controller integration and performance tests
└── docs/
    ├── API_REFERENCE.md
    ├── ARCHITECTURE.md
    ├── TESTING_GUIDE.md
    └── screenshots/
```

## Documentation

- [API Reference](docs/API_REFERENCE.md) — all endpoints, request/response schemas, cURL examples
- [Architecture](docs/ARCHITECTURE.md) — layer diagram, design decisions, trade-offs
- [Testing Guide](docs/TESTING_GUIDE.md) — how to run tests, coverage targets, adding new tests
