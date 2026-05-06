SYSTEM: You are the Technical Writer agent for AiTicketHub.
Your goal is to produce four documentation files in one pass,
each written for a different reader persona with no content
crossing between them.

---

USER: Generate the full documentation suite for the
AiTicketHub Support Ticket API.

Project specification:
- Project name:     {PROJECT_NAME}
  (e.g. AiTicketHub Support Ticket API)
- Base URL:         {BASE_URL}
  (e.g. http://localhost:5000)
- .NET SDK version: {DOTNET_SDK_VERSION}
  (e.g. 9.0.x)
- Endpoints:        {ENDPOINTS}
  (e.g.
    POST   /api/tickets           Create a new ticket
    GET    /api/tickets           List all tickets
    GET    /api/tickets/{id}      Get ticket by ID
    PUT    /api/tickets/{id}      Update a ticket
    DELETE /api/tickets/{id}      Delete a ticket
    POST   /api/tickets/{id}/
           auto-classify          Auto-classify ticket)
- Error codes:      {ERROR_CODES}
  (e.g.
    Validation.Failed    | 400 | Invalid request fields
    Ticket.NotFound      | 404 | No ticket with given ID
    Ticket.Duplicate     | 409 | Duplicate subject
    Ticket.InvalidStatus | 422 | Invalid status transition)
- Test counts:      {TEST_COUNTS}
  (e.g.
    Unit / Services:     24 tests
    Unit / Validators:   18 tests
    Unit / Repositories: 10 tests
    Integration / API:   12 tests)
- Design decisions: {DESIGN_DECISIONS}
  (e.g.
    Result<T> pattern instead of exceptions
    ConcurrentDictionary for in-memory storage
    FluentValidation in Application layer
    Clean Architecture four-layer dependency rule)

---

STEPS:

1. Build a fact sheet before writing any markdown.
   - Read every controller in
     src/AiTicketHub/API/Controllers/ and list:
     HTTP verb, route, success status, request fields,
     response fields.
   - Read every file in
     src/AiTicketHub/Application/Interfaces/ and
     confirm each endpoint has a service method.
   - Count test methods per folder in
     tests/AiTicketHub.Tests/ and confirm against
     {TEST_COUNTS}.
   - Flag any fact that cannot be verified as
     "unverified" — leave that doc section blank with:
     "Not yet implemented — to be completed after
     Developer handoff."
   - Output fact sheet as plain table before any
     markdown block.

2. Write README.md.
   Sections in this exact order:
   a. Title + one-sentence description
   b. Prerequisites — .NET SDK {DOTNET_SDK_VERSION} only
   c. Getting Started — numbered imperative steps:
      1. Clone the repository
      2. dotnet build src/AiTicketHub.sln
         → expected: "Build succeeded."
      3. dotnet run --project
         src/AiTicketHub/API/AiTicketHub.API.csproj
         → expected: "Now listening on: {BASE_URL}"
   d. Running Tests — exact dotnet test command +
      expected: "{total} passed, 0 failed."
      Derive total by summing {TEST_COUNTS}.
   e. Project Structure — directory tree, one-line
      description per folder, src/ tests/ docs/ only
   f. Documentation — links to the three docs/ files

   Do NOT include: error tables, architecture diagrams,
   design decisions, test strategy rationale.

3. Write docs/API_REFERENCE.md.
   Sections in this exact order:
   a. Title: # API Reference
   b. Base URL as a code span
   c. Endpoints summary table:
      Method | Route | Description
   d. One ## <METHOD> <Route> section per endpoint:
      ### Description — one sentence
      ### Request Body — JSON block with field types
        and constraints (omit for GET and DELETE)
      ### Response — Success — status code + JSON block
      ### Response — Errors — table:
        Error Code | HTTP Status | When It Occurs
        (only codes this endpoint can return)
      ### Example — one curl block using {BASE_URL},
        realistic values, GUID:
        3fa85f64-5717-4562-b3fc-2c963f66afa6
   e. Error Response Format — JSON shape every error uses
   f. Error Code Catalogue — table:
      Code | HTTP Status | Meaning
      Every entry from {ERROR_CODES}, none added or omitted

   Do NOT include: Result<T>, ConcurrentDictionary,
   layer names, class names, or any internal detail.

4. Write docs/ARCHITECTURE.md.
   Sections in this exact order:
   a. Title: # Architecture
   b. Overview — exactly three sentences:
      (1) what it does, (2) what stack, (3) what style
      and what constraint that style enforces
   c. Layer Dependency Diagram — Mermaid graph TD:
      graph TD
          API[AiTicketHub.API] --> APP[AiTicketHub.Application]
          INF[AiTicketHub.Infrastructure] --> APP
          APP --> DOM[AiTicketHub.Domain]
      No extra nodes or edges.
   d. Layer Responsibilities — one paragraph per layer:
      Domain → Application → Infrastructure → API
      Each answers: what does it own, what does it
      explicitly not do?
   e. Request Lifecycle — Mermaid sequenceDiagram using
      POST /api/tickets as example:
      Client → Controller → Service → Repository →
      Service → Controller → Client
      Solid arrows for requests, dashed for responses.
      Show Result<T> on response arrows.
   f. Key Design Decisions — one ### per {DESIGN_DECISIONS}
      entry, exactly three sentences each:
      (1) what was decided, (2) why, (3) trade-off accepted
   g. Constraints and Trade-offs — two bullet lists:
      "This design optimises for:" (3-5 bullets)
      "This design sacrifices:" (2-3 bullets)

   Do NOT include: cURL examples, setup instructions,
   NuGet lists, test counts, endpoint field tables.

5. Write docs/TESTING_GUIDE.md.
   Sections in this exact order:
   a. Title: # Testing Guide
   b. Test Pyramid — Mermaid graph BT:
      graph BT
          INT["Integration Tests (N)
               Controller endpoints — real DI stack"]
          UNIT["Unit Tests (N)
                Services · Validators · Repositories"]
          INT --> UNIT
      Replace N with actual counts from {TEST_COUNTS}.
   c. Running Tests — four dotnet test commands:
      All tests, unit only, integration only,
      with coverage report. Each as imperative.
      Expected: "{total} passed, 0 failed."
   d. Test Structure — directory tree of
      tests/AiTicketHub.Tests/, one-line description
      per folder, derived from STEP 1
   e. Naming Convention — pattern:
      MethodName_StateUnderTest_ExpectedBehavior
      Plus three concrete examples from actual
      test method names found in STEP 1
   f. Test Categories and What They Cover — table:
      Category | Files | What Is Tested |
      Dependencies Mocked
      One row per test folder from STEP 1
   g. Adding a New Test — two numbered checklists:
      (1) Unit test for new service method (5 steps)
      (2) Integration test for new endpoint (4 steps)
      Every step is an imperative verb phrase
   h. Coverage Expectations — table:
      Layer | Target | Notes
      Overall target >85%, per-layer from {TEST_COUNTS}

   Do NOT include: architecture diagrams, endpoint
   definitions, request/response schemas, or code.
   Link to ARCHITECTURE.md and API_REFERENCE.md.

6. Check your work against CONSTRAINTS before finishing.

---

CONSTRAINTS:

Note: All agent-level constraints from
technical-writer.md apply to every step above.
The following are task-specific.

- NEVER invent an error code, HTTP verb, route path,
  or response field not in {ENDPOINTS} or {ERROR_CODES}
  — every value must trace to the specification or
  source files read in STEP 1
- NEVER write a cURL example with malformed JSON,
  incorrect URL, or placeholder like {id} — every
  example must be copy-pasteable against a running API
- NEVER include setup instructions in ARCHITECTURE.md
  or TESTING_GUIDE.md — those live in README.md only
- NEVER include endpoint request/response field tables
  in ARCHITECTURE.md — those live in API_REFERENCE.md
- NEVER add a Mermaid node, edge, or participant that
  does not correspond to a real layer or flow confirmed
  in STEP 1
- NEVER use passive voice in numbered instructional
  steps — write "Run dotnet build" not "The project
  can be built by running"
- NEVER add technical lead content to README.md —
  design decisions and trade-offs link to
  ARCHITECTURE.md instead

---

OUTPUT_FORMAT:

Fact sheet from STEP 1 as a plain Markdown table first.

Then each file as a fenced markdown block with the
file path as a comment on the first line:
<!-- README.md -->
<!-- docs/API_REFERENCE.md -->
<!-- docs/ARCHITECTURE.md -->
<!-- docs/TESTING_GUIDE.md -->

Produce files in this order:
1. README.md
2. docs/API_REFERENCE.md
3. docs/ARCHITECTURE.md
4. docs/TESTING_GUIDE.md

After the last file add ## Self-review checklist.
Re-state each task-specific CONSTRAINT as a checkbox:
[x] satisfied or [ ] violated.
Fix any [ ] items before responding.