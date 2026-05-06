# PLAN.md — AiTicketHub Support Ticket API

## Project Overview

AI-assisted implementation of a Support Ticket Management REST API using .NET 9 and Clean Architecture. Every phase is executed by a specific AI agent using a designated skill file.

**Status markers:** `[ ]` not started · `[~]` in progress · `[x]` done · `[!]` blocked

---

## Workflow Sequence

```
create-project-layout → create-structure → implement-feature
   → create-parser + create-endpoint (import)
   → create-endpoint (auto-classify)
   → create-tests
   → create-docs
```

---

## Phase 0 — Foundation

**Goal:** Bootstrap the .NET 9 solution with all projects, project references, NuGet packages, and foundational domain types.

**Agent:** Architect (`agents/architect.md`)
**Skill:** `skills/create-project-layout.md`

### Tasks

- [x] Create `AiTicketHub.sln` with five projects (Domain, Application, Infrastructure, API, Tests)
- [x] Generate `AiTicketHub.Domain.csproj` (no external references)
- [x] Generate `AiTicketHub.Application.csproj` (references Domain; FluentValidation 11.x)
- [x] Generate `AiTicketHub.Infrastructure.csproj` (references Application)
- [x] Generate `AiTicketHub.API.csproj` (references Application + Infrastructure; FluentValidation.AspNetCore 11.x)
- [x] Generate `AiTicketHub.Tests.csproj` (references API; NUnit 4.x, FluentAssertions 6.x, Moq 4.x, Mvc.Testing)
- [x] Create all empty folders with `.gitkeep`
- [x] Create `Domain/Common/Error.cs` and `Domain/Common/Result.cs`
- [x] Create empty DI stubs: `ApplicationServiceExtensions.cs`, `InfrastructureServiceExtensions.cs`
- [x] Create `API/Program.cs` skeleton with three wiring calls

**Prompt trigger:**
```
Load agents/architect.md and skills/create-project-layout.md.

Bootstrap the AiTicketHub solution.
Solution name:    AiTicketHub
Target framework: net9.0
```

**Completion check:** `dotnet build AiTicketHub.sln` exits with code 0 and all five projects compile with no errors.

---

## Phase 1 — Ticket Entity Structure

**Goal:** Define all contracts for the Ticket entity across every Clean Architecture layer — no method bodies, no test code.

**Agent:** Architect (`agents/architect.md`)
**Skill:** `skills/create-structure.md`

### Tasks

- [ ] Define `Domain/Entities/Ticket.cs` skeleton (properties + constructor signature)
- [ ] Define `Domain/Enums/TicketCategory.cs` (account_access | technical_issue | billing_question | feature_request | bug_report | other)
- [ ] Define `Domain/Enums/TicketPriority.cs` (urgent | high | medium | low)
- [ ] Define `Domain/Enums/TicketStatus.cs` (new | in_progress | waiting_customer | resolved | closed)
- [ ] Define `Domain/Enums/TicketSource.cs` (web_form | email | api | chat | phone)
- [ ] Define `Domain/Enums/DeviceType.cs` (desktop | mobile | tablet)
- [ ] Populate `Domain/Common/Errors.cs` with all error codes (NotFound, InvalidStatus, Duplicate, ValidationFailed)
- [ ] Define `Application/Interfaces/ITicketService.cs` (5 method signatures: Create, GetById, List, Update, Delete)
- [ ] Define `Application/Interfaces/ITicketRepository.cs` (matching repository signatures)
- [ ] Create Request + Response DTO pairs for each operation (10 files)
- [ ] Create validator class skeletons for writable operations (Create, Update — empty constructors)
- [ ] Output DI registration note

**Prompt trigger:**
```
Load agents/architect.md and skills/create-structure.md.

Design the structure for the Ticket entity.

Entity name:    Ticket
Entity fields:  Id: Guid, CustomerId: string, CustomerEmail: string,
                CustomerName: string, Subject: string, Description: string,
                Category: TicketCategory, Priority: TicketPriority,
                Status: TicketStatus, CreatedAt: DateTime, UpdatedAt: DateTime,
                ResolvedAt: DateTime?, AssignedTo: string?,
                Tags: List<string>, Source: TicketSource,
                Browser: string?, DeviceType: DeviceType
Enums needed:   TicketCategory, TicketPriority, TicketStatus, TicketSource, DeviceType
Operations:     CreateTicket, GetTicketById, ListTickets, UpdateTicket, DeleteTicket
Business rules: Subject 1–200 chars; Description 10–2000 chars;
                CustomerEmail must be valid email format;
                Status transitions: new→in_progress→waiting_customer→resolved→closed only;
                Resolved or closed tickets cannot be deleted
```

**Completion check:** All 20+ files exist; no method body contains logic; `dotnet build` still passes.

---

## Phase 2 — Core CRUD Implementation

**Goal:** Fill every method body for the five CRUD operations across Domain, Repository, Validators, Service, and Controller layers.

**Agent:** Implementer (`agents/developer.md`)
**Skill:** `skills/implement-feature.md`

### Tasks

- [ ] Implement `Ticket.cs` constructor body and status-transition methods
- [ ] Implement `TicketRepository.cs` (ConcurrentDictionary — all 5 methods)
- [ ] Fill `CreateTicketValidator.cs` rule chains (subject, description, email)
- [ ] Fill `UpdateTicketValidator.cs` rule chains
- [ ] Implement `TicketService.cs` (all 5 methods: validate → repository → map DTO → return Result)
- [ ] Implement `TicketController.cs` (all 5 actions with uniform `{"code":"...","message":"..."}` error envelope)
- [ ] Wire DI: `ITicketService → TicketService` (Scoped) in `AddApplicationServices`
- [ ] Wire DI: `ITicketRepository → TicketRepository` (Singleton) in `AddInfrastructureServices`

**Prompt trigger:**
```
Load agents/developer.md and skills/implement-feature.md.

Implement all method bodies for the Ticket entity.

Entity name:   Ticket
Operations:    CreateTicket, GetTicketById, ListTickets, UpdateTicket, DeleteTicket
Contracts at:  src/AiTicketHub
```

**Completion check:** `dotnet build` passes; manual `curl -X POST /tickets` with valid body returns `201 Created` with ticket JSON.

---

## Phase 3 — Bulk Import Endpoint

**Goal:** Add `POST /tickets/import` supporting CSV, JSON, and XML file uploads with bulk-import summary response.

**Agent:** Implementer (`agents/developer.md`)
**Skills:** `skills/create-parser.md` then `skills/create-endpoint.md`

### Tasks

**Parser layer (create-parser.md):**
- [ ] Implement `CsvTicketParser.cs` with `ParseResult<Ticket>` pattern
- [ ] Implement `JsonTicketParser.cs` with `ParseResult<Ticket>` pattern
- [ ] Implement `XmlTicketParser.cs` with `ParseResult<Ticket>` pattern
- [ ] Implement `TicketImportService.cs` coordinating all three parsers

**Endpoint layer (create-endpoint.md):**
- [ ] Add `ImportRequest.cs` DTO (IFormFile, optional format hint)
- [ ] Add `ImportResponse.cs` DTO (Total: int, Successful: int, Failed: int, Errors: list)
- [ ] Add `ImportTicketsValidator.cs` (file not null, size limit, allowed MIME types)
- [ ] Add `ImportTickets` method signature to `ITicketService.cs`
- [ ] Implement `ImportTickets` in `TicketService.cs` (delegate to import service, accumulate results)
- [ ] Add `IImportTicketRepository` method to `ITicketRepository.cs` (BulkAdd)
- [ ] Implement `BulkAdd` in `TicketRepository.cs`
- [ ] Add `POST /tickets/import` action to `TicketController.cs`
- [ ] Register `ITicketImportService → TicketImportService` in DI

**Prompt trigger (step 1 — parsers):**
```
Load agents/developer.md and skills/create-parser.md.

Create parsers for the Ticket entity.
Formats:      CSV, JSON, XML
Entity:       Ticket
Output type:  ParseResult<Ticket>
Source path:  src/AiTicketHub/Infrastructure/Parsers/
```

**Prompt trigger (step 2 — endpoint):**
```
Load agents/developer.md and skills/create-endpoint.md.

Create a new endpoint for the AiTicketHub Support Ticket API.
Method:          POST
Route:           /tickets/import
Operation name:  ImportTickets
Entity:          Ticket
Request fields:  File: IFormFile, AutoClassify: bool (optional, default false)
Response fields: Total: int, Successful: int, Failed: int,
                 Errors: List<ImportError> (RowNumber: int, Message: string)
Business rules:  File must not be null; allowed formats CSV/JSON/XML;
                 max file size 10 MB; partial success allowed (non-atomic);
                 return summary even when all records fail
```

**Completion check:** `POST /tickets/import` with a valid CSV returns `200 OK` with `{"total":N,"successful":M,"failed":K,"errors":[]}`.

---

## Phase 4 — Auto-Classification Endpoint

**Goal:** Implement `POST /tickets/:id/auto-classify` that assigns category and priority from keyword analysis and returns confidence score with reasoning.

**Agent:** Implementer (`agents/developer.md`)
**Skill:** `skills/create-endpoint.md`

### Tasks

- [ ] Implement `KeywordClassifier.cs` in Infrastructure (category + priority rules from TASKS.md §Task 2)
- [ ] Define `IClassificationService.cs` in Application/Interfaces
- [ ] Add `AutoClassifyRequest.cs` DTO (optional override flags)
- [ ] Add `AutoClassifyResponse.cs` DTO (Category, Priority, Confidence: double, Reasoning: string, KeywordsFound: List<string>)
- [ ] Add `AutoClassifyValidator.cs`
- [ ] Add `AutoClassify` method to `ITicketService.cs`
- [ ] Implement `AutoClassify` in `TicketService.cs` (fetch ticket → classify → persist category/priority → return response)
- [ ] Add `UpdateClassification` method to `ITicketRepository.cs`
- [ ] Implement `UpdateClassification` in `TicketRepository.cs`
- [ ] Add `POST /tickets/{id}/auto-classify` action to `TicketController.cs`
- [ ] Register `IClassificationService → KeywordClassifier` in DI (Singleton)
- [ ] Add `AutoClassify: bool` flag to `CreateTicketRequest.cs` (optional, triggers auto-classify on creation)

**Prompt trigger:**
```
Load agents/developer.md and skills/create-endpoint.md.

Create a new endpoint for the AiTicketHub Support Ticket API.
Method:          POST
Route:           /tickets/{id}/auto-classify
Operation name:  AutoClassify
Entity:          Ticket
Request fields:  (none required — id comes from route)
Response fields: Category: TicketCategory, Priority: TicketPriority,
                 Confidence: double, Reasoning: string,
                 KeywordsFound: List<string>
Business rules:  Ticket must exist (Ticket.NotFound if not);
                 Urgent keywords: "can't access","critical","production down","security";
                 High keywords: "important","blocking","asap";
                 Low keywords: "minor","cosmetic","suggestion";
                 Medium is default priority;
                 Category matched by login/password/2FA → account_access,
                 payment/invoice/refund → billing_question,
                 bug/error/crash + reproduction → bug_report,
                 enhancement/suggestion → feature_request,
                 other errors → technical_issue, else → other;
                 Confidence 0.0–1.0 (keyword-hit ratio);
                 Log every classification decision
```

**Completion check:** `POST /tickets/{id}/auto-classify` on an existing ticket returns `200 OK` with confidence score ≥ 0 and ≤ 1 and a non-empty `reasoning` field.

---

## Phase 5 — Test Suite

**Goal:** Generate a comprehensive test suite achieving >85% code coverage across all layers.

**Agent:** Test Engineer (`agents/test-engineer.md`)
**Skill:** `skills/create-tests.md`

### Tasks

**Unit tests — Application layer:**
- [ ] `tests/Application/TicketServiceTests.cs` — CreateTicket (happy path, validation fail, duplicate)
- [ ] `tests/Application/TicketServiceTests.cs` — GetTicketById (found, not found)
- [ ] `tests/Application/TicketServiceTests.cs` — ListTickets (empty, filtered, paged)
- [ ] `tests/Application/TicketServiceTests.cs` — UpdateTicket (valid, not found, invalid status transition)
- [ ] `tests/Application/TicketServiceTests.cs` — DeleteTicket (valid, not found, resolved/closed rejection)
- [ ] `tests/Application/TicketServiceTests.cs` — ImportTickets (all success, partial failure, all failure)
- [ ] `tests/Application/TicketServiceTests.cs` — AutoClassify (each category, each priority, not found)
- [ ] `tests/Application/CreateTicketValidatorTests.cs` — subject bounds, description bounds, email format
- [ ] `tests/Application/UpdateTicketValidatorTests.cs` — same field rules

**Unit tests — Infrastructure layer:**
- [ ] `tests/Infrastructure/TicketRepositoryTests.cs` — Add, GetById, GetAll, Update, Delete concurrency
- [ ] `tests/Infrastructure/CsvParserTests.cs` — valid file, malformed rows, empty file, encoding edge cases
- [ ] `tests/Infrastructure/JsonParserTests.cs` — valid array, missing fields, wrong types, empty array
- [ ] `tests/Infrastructure/XmlParserTests.cs` — valid document, malformed XML, missing elements
- [ ] `tests/Infrastructure/KeywordClassifierTests.cs` — each category path, each priority path, confidence bounds

**Integration tests — API layer:**
- [ ] `tests/API/TicketControllerTests.cs` — POST /tickets (201, 400, 409)
- [ ] `tests/API/TicketControllerTests.cs` — GET /tickets/:id (200, 404)
- [ ] `tests/API/TicketControllerTests.cs` — GET /tickets with filters (200, empty list)
- [ ] `tests/API/TicketControllerTests.cs` — PUT /tickets/:id (200, 400, 404, 422)
- [ ] `tests/API/TicketControllerTests.cs` — DELETE /tickets/:id (200, 404, 422)
- [ ] `tests/API/TicketControllerTests.cs` — POST /tickets/import (200 all-success, 200 partial, 400 bad file)
- [ ] `tests/API/TicketControllerTests.cs` — POST /tickets/:id/auto-classify (200, 404)

**Integration + performance tests:**
- [ ] `tests/API/IntegrationTests.cs` — full lifecycle: create → classify → update → resolve → delete
- [ ] `tests/API/IntegrationTests.cs` — bulk import → auto-classify all imported tickets
- [ ] `tests/API/IntegrationTests.cs` — 20 concurrent CreateTicket requests (no data race)
- [ ] `tests/API/IntegrationTests.cs` — combined filter by category AND priority
- [ ] `tests/API/PerformanceTests.cs` — ListTickets benchmark (1000 tickets, p95 < 200 ms)

**Coverage:**
- [ ] Run `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Verify overall line coverage > 85%
- [ ] Save coverage report; take screenshot to `docs/screenshots/test_coverage.png`

**Prompt trigger:**
```
Load agents/test-engineer.md and skills/create-tests.md.

Generate the full test suite for the Ticket entity.

Entity:                  Ticket
Service method:          Task<Result<CreateTicketResponse>> CreateTicketAsync(CreateTicketRequest)
Contracts at:            src/AiTicketHub
Coverage target:         >85%
Test framework:          NUnit 4.x + FluentAssertions 6.x + Moq 4.x
```
*(Repeat for each operation or run one pass covering all operations.)*

**Completion check:** `dotnet test` shows all tests green; coverage report shows line coverage ≥ 85%.

---

## Phase 6 — Documentation

**Goal:** Produce four audience-targeted documentation files each containing at least one Mermaid diagram.

**Agent:** Technical Writer (`agents/technical-writer.md`)
**Skill:** `skills/create-docs.md`

### Tasks

- [ ] `README.md` — project overview, Mermaid architecture diagram, setup/run/test instructions, project structure tree
- [ ] `docs/API_REFERENCE.md` — all 7 endpoints with request/response JSON examples, cURL samples, error envelope format, data model schemas
- [ ] `docs/ARCHITECTURE.md` — Mermaid component diagram, Mermaid sequence diagram (ticket lifecycle + import flow), dependency rule explanation, design decisions, security notes
- [ ] `docs/TESTING_GUIDE.md` — Mermaid test-pyramid diagram, how to run tests, sample data locations, manual testing checklist, performance benchmark table

**Prompt trigger:**
```
Load agents/technical-writer.md and skills/create-docs.md.

Generate all documentation for AiTicketHub.

Solution name:    AiTicketHub
API base URL:     http://localhost:5000
Endpoints:        POST /tickets, GET /tickets, GET /tickets/{id},
                  PUT /tickets/{id}, DELETE /tickets/{id},
                  POST /tickets/import, POST /tickets/{id}/auto-classify
Tech stack:       .NET 9, NUnit 4.x, FluentAssertions, Moq
Diagrams needed:  architecture (README), component+sequence (ARCHITECTURE),
                  test-pyramid (TESTING_GUIDE)
```

**Completion check:** All four files exist; each contains at least one fenced Mermaid block; API_REFERENCE.md includes a cURL example for every endpoint.

---

## Phase 7 — Delivery

**Goal:** Produce all sample data files, verify deliverables, and package the submission.

**Agent:** Implementer (`agents/developer.md`) for sample data; manual for final review.
**Skill:** None (manual tasks).

### Tasks

**Sample data:**
- [ ] Generate `tests/fixtures/sample_tickets.csv` — 50 valid tickets covering all enum values
- [ ] Generate `tests/fixtures/sample_tickets.json` — 20 valid tickets as JSON array
- [ ] Generate `tests/fixtures/sample_tickets.xml` — 30 valid tickets as XML document
- [ ] Generate `tests/fixtures/invalid_tickets.csv` — intentionally malformed rows for negative tests
- [ ] Generate `tests/fixtures/invalid_tickets.json` — missing required fields, wrong types
- [ ] Generate `tests/fixtures/invalid_tickets.xml` — malformed XML for negative tests

**Prompt trigger (sample data):**
```
Load agents/developer.md.

Generate sample data files for AiTicketHub testing.
Formats:    CSV (50 tickets), JSON (20 tickets), XML (30 tickets)
Also generate invalid variants for each format (5–10 malformed records each).
Cover all enum values across files: all categories, all priorities, all statuses.
Output path: tests/fixtures/
```

**Final checklist:**
- [ ] `dotnet build AiTicketHub.sln` exits 0
- [ ] `dotnet test` exits 0 with ≥ 85% coverage
- [ ] All 7 REST endpoints respond correctly to valid inputs
- [ ] All four documentation files are present and valid Markdown
- [ ] `docs/screenshots/test_coverage.png` exists
- [ ] All six sample data files are present under `tests/fixtures/`
- [ ] No production code references `NUnit`, `Moq`, or `FluentAssertions`
- [ ] No test code references `ConcurrentDictionary` directly

**Completion check:** All boxes above are ticked; repository is clean (`git status` shows no untracked or modified files outside expected paths).

---

## Decision Log

| Date | Phase | Decision | Rationale | Made By |
|------|-------|----------|-----------|---------|
| 2026-05-06 | 0 | Added non-generic `Result` class alongside `Result<T>` | Enables void-return operations (e.g. Delete) without boxing a dummy value | Architect agent |
| 2026-05-06 | 0 | `Program.cs` exposes `public partial class Program` | Required by `WebApplicationFactory<Program>` in the Tests project | Architect agent |

---

## Open Questions

The following questions **must be answered before Phase 2 coding starts**. Unresolved questions become assumptions that must be logged in the Decision Log.

| # | Question | Impact | Default assumption if unanswered |
|---|----------|--------|----------------------------------|
| Q1 | Should `POST /tickets` with `AutoClassify: true` fail if classification fails, or succeed and return the ticket without classification? | Phase 2 + Phase 4 | Succeed without classification; classification is best-effort |
| Q2 | What exact status transitions are allowed? (Can a ticket move from `waiting_customer` back to `in_progress`?) | Phase 1 Errors.cs, Phase 2 domain entity | Only forward transitions; no backward movement |
| Q3 | Is `POST /tickets/import` atomic (all-or-nothing) or partial-success? | Phase 3 repository + response DTO | Partial-success; return summary with per-row errors |
| Q4 | Does `GET /tickets` support pagination? If so, what parameters (page/size vs cursor)? | Phase 1 DTOs, Phase 2 ListTickets | Yes, page-number + page-size (`?page=1&size=20`) |
| Q5 | What filtering fields does `GET /tickets` support? | Phase 1 `ListTicketsRequest.cs` | Category, Priority, Status, AssignedTo |
| Q6 | Is the confidence score a keyword-hit ratio, TF-IDF, or something else? | Phase 4 `KeywordClassifier` | Simple keyword-hit ratio: matched keywords / total keywords checked |
| Q7 | Should deleted tickets be hard-deleted from the in-memory store, or soft-deleted with a `DeletedAt` timestamp? | Phase 1 entity fields, Phase 2 repository | Hard delete (remove from ConcurrentDictionary) |
| Q8 | What is the maximum file size for `/tickets/import`? | Phase 3 validator | 10 MB |
| Q9 | Should classification decisions be logged to console, a file, or a separate in-memory store? | Phase 4 `KeywordClassifier` | Console (`ILogger<T>`) only |
| Q10 | Are `tags` free-form strings or constrained to a defined set? | Phase 1 entity + validator | Free-form strings, no constraint beyond non-null |

---

## Coverage Checklist (TASKS.md → Plan mapping)

| TASKS.md requirement | Covered in phase |
|----------------------|-----------------|
| POST /tickets | Phase 2 |
| POST /tickets/import (CSV/JSON/XML) | Phase 3 |
| GET /tickets (with filtering) | Phase 2 |
| GET /tickets/:id | Phase 2 |
| PUT /tickets/:id | Phase 2 |
| DELETE /tickets/:id | Phase 2 |
| Bulk import summary (total/successful/failed/errors) | Phase 3 |
| Malformed file handling with meaningful errors | Phase 3 |
| Appropriate HTTP status codes | Phase 2–4 |
| Auto-classification POST /tickets/:id/auto-classify | Phase 4 |
| Classification: category rules | Phase 4 |
| Classification: priority keyword rules | Phase 4 |
| Confidence score (0–1), reasoning, keywords found | Phase 4 |
| Auto-run on ticket creation (optional flag) | Phase 4 |
| Store classification confidence | Phase 4 |
| Allow manual override | Phase 4 (UpdateTicket) |
| Log all classification decisions | Phase 4 |
| test_ticket_api (11 tests) | Phase 5 |
| test_ticket_model / validator tests (9 tests) | Phase 5 |
| test_import_csv (6 tests) | Phase 5 |
| test_import_json (5 tests) | Phase 5 |
| test_import_xml (5 tests) | Phase 5 |
| test_categorization (10 tests) | Phase 5 |
| test_integration end-to-end (5 tests) | Phase 5 |
| test_performance benchmarks (5 tests) | Phase 5 |
| Overall coverage >85% | Phase 5 |
| README.md (developer audience) | Phase 6 |
| API_REFERENCE.md (API consumer audience) | Phase 6 |
| ARCHITECTURE.md (technical lead audience) | Phase 6 |
| TESTING_GUIDE.md (QA audience) | Phase 6 |
| ≥3 Mermaid diagrams across docs | Phase 6 |
| sample_tickets.csv (50 tickets) | Phase 7 |
| sample_tickets.json (20 tickets) | Phase 7 |
| sample_tickets.xml (30 tickets) | Phase 7 |
| Invalid data files for negative tests | Phase 7 |
| Coverage screenshot docs/screenshots/test_coverage.png | Phase 5 |
