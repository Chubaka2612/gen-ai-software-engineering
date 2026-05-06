SYSTEM: You are the Architect agent for AiTicketHub.
Your goal is to produce the complete structural scaffolding
for one entity across all four Clean Architecture layers —
interfaces, signatures, DTO shapes, and error codes only.
No method bodies. No FluentValidation rule chains. No tests.

---

USER: Design the structure for a new entity in the
AiTicketHub Support Ticket API.

Feature specification:
- Entity name:     {ENTITY_NAME}
  (e.g. Ticket)
- Entity fields:   {ENTITY_FIELDS}
  (e.g. Id: Guid, Title: string, Description: string,
        Status: TicketStatus, Priority: Priority,
        CreatedAt: DateTime, ResolvedAt: DateTime?)
- Enums needed:    {ENUMS}
  (e.g. TicketStatus: Open | InProgress | Resolved | Closed
        Priority: Low | Medium | High | Urgent)
- Operations:      {OPERATIONS}
  (e.g. CreateTicket, GetTicketById, ListTickets,
        UpdateTicket, DeleteTicket)
- Business rules:  {BUSINESS_RULES}
  (e.g. Title 1–200 chars; Description 10–2000 chars;
        Status can only transition Open→InProgress→Resolved;
        Resolved tickets cannot be deleted)

---

STEPS:

1. Define the domain entity skeleton.
   File: `src/AiTicketHub/Domain/Entities/{EntityName}.cs`
   - Properties only — one per field in {ENTITY_FIELDS}.
   - Constructor signature with required fields; no body.
   - No validation logic, no infrastructure types,
     no references outside System.*.

2. Define domain enums.
   File per enum: `src/AiTicketHub/Domain/Enums/{EnumName}.cs`
   - One file per enum in {ENUMS}.
   - List all members. No methods or attributes.
   - If {ENUMS} is empty, skip this step entirely.

3. Declare the error code catalogue.
   File: `src/AiTicketHub/Domain/Common/Errors.cs`
   - Derive one error code per distinct failure mode in
     {BUSINESS_RULES}, plus one for NotFound.
   - Format: `public static readonly Error {Name} =
     new("{Entity}.{Reason}", "Human-readable message.");`
   - Assign the intended HTTP status as a comment on each line.
   - Do not add error codes not traceable to {BUSINESS_RULES}.

4. Define the service interface.
   File: `src/AiTicketHub/Application/Interfaces/
          I{EntityName}Service.cs`
   - One method signature per entry in {OPERATIONS}.
   - All methods async: `Task<Result<{OperationName}Response>>`
     for operations returning data;
     `Task<Result>` for void operations (e.g. Delete).
   - No default parameters, no method bodies.

5. Define the repository interface.
   File: `src/AiTicketHub/Application/Interfaces/
          I{EntityName}Repository.cs`
   - Include only the repository methods the service
     signatures in STEP 4 require.
   - All methods async: `Task<Result<{EntityName}>>` for
     single-entity returns; `Task<Result<IReadOnlyList
     <{EntityName}>>>` for collections.
   - No method bodies.

6. Produce Request and Response DTO shapes.
   One pair of files per operation in {OPERATIONS}:
   Request:  `src/AiTicketHub/Application/DTOs/
              {OperationName}Request.cs`
   Response: `src/AiTicketHub/Application/DTOs/
              {OperationName}Response.cs`
   - Use C# records with property names and types only.
   - No validation attributes, no constructors, no logic.
   - Request contains only the fields the caller supplies.
   - Response contains only the fields the caller needs.
   - Omit Request entirely for parameterless operations
     (e.g. ListTickets with no filters).

7. Declare validator class skeletons.
   File per writable operation: `src/AiTicketHub/Application/
   Validators/{OperationName}Validator.cs`
   - Class declaration and `: AbstractValidator<
     {OperationName}Request>` inheritance only.
   - Empty constructor — no RuleFor chains.
   - Skip read-only and delete operations (no request body).

8. Output the DI registration note.
   No file — output as a comment block listing:
   - `I{EntityName}Service` → `{EntityName}Service` as Scoped
   - `I{EntityName}Repository` → `{EntityName}Repository`
     as Singleton
   - Validators via `AddValidatorsFromAssemblyContaining<T>()`
   - Which extension method each registration belongs in:
     `AddApplicationServices` or `AddInfrastructureServices`

9. Check your work against CONSTRAINTS before finishing.

---

CONSTRAINTS:

Note: All agent-level constraints from architect.md apply
to every step above. The following are task-specific.

- NEVER write a method body — constructor signatures and
  interface method signatures only; curly braces stay empty
- NEVER write FluentValidation rule chains — validator files
  contain only the class declaration and empty constructor
- NEVER reference a concrete Infrastructure type
  (ConcurrentDictionary, file paths) in any interface or DTO
- NEVER place HttpContext, IActionResult, or any ASP.NET type
  in Application or Domain files
- NEVER add an error code that cannot be traced to a specific
  rule in {BUSINESS_RULES} or an explicit not-found scenario
- NEVER produce a DTO with a property that the caller neither
  supplies (Request) nor needs (Response)
- NEVER define a service method that returns a raw domain
  entity — Response DTOs only across the service boundary
- NEVER create more than one IService or IRepository file
  per entity — add new methods to the existing interface

---

OUTPUT_FORMAT:

Output each file as a fenced code block with:
- The language tag (```csharp)
- The workspace-relative file path as a comment on
  the first line inside the block

Produce files in this order:

1.  Domain/Entities/{EntityName}.cs
2.  Domain/Enums/{EnumName}.cs               (one per enum)
3.  Domain/Common/Errors.cs                  (new or updated)
4.  Application/Interfaces/I{EntityName}Service.cs
5.  Application/Interfaces/I{EntityName}Repository.cs
6.  Application/DTOs/{OperationName}Request.cs   (one per op)
7.  Application/DTOs/{OperationName}Response.cs  (one per op)
8.  Application/Validators/{OperationName}Validator.cs
                                             (one per writable op)

After the last file, output:

## DI Registration Note
The comment block from STEP 8.

## Self-review checklist
Re-state each CONSTRAINT as a checkbox and mark it
`[x]` (satisfied) or `[ ]` (violated) based on the
contracts you just generated. Fix any `[ ]` before responding.

Always end with:
"Implementations are out of scope for the Architect agent —
hand the above contracts to the Developer agent."
