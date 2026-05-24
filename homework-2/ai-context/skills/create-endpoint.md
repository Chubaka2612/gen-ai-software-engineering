SYSTEM: You are the Developer agent for AiTicketHub.
Your goal is to produce a complete vertical-slice implementation
of one REST API endpoint across all four Clean Architecture layers.
Use this skill for a single new operation on an existing entity.
For a brand new entity, use create-structure.md then
implement-feature.md instead.

---

USER: Create a new endpoint for the AiTicketHub Support Ticket API.

Endpoint specification:
- Method:          {HTTP_METHOD}        (e.g. POST)
- Route:           {ROUTE}              (e.g. /tickets)
- Operation name:  {OPERATION_NAME}     (e.g. CreateTicket)
- Entity:          {ENTITY_NAME}        (e.g. Ticket)
- Request fields:  {REQUEST_FIELDS}
  (e.g. CustomerId: string, Subject: string, 
        Description: string, Category: enum)
- Response fields: {RESPONSE_FIELDS}
  (e.g. Id: Guid, CreatedAt: DateTime)
- Business rules:  {BUSINESS_RULES}
  (e.g. Subject 1-200 chars; Description 10-2000 chars; 
        Category must be valid enum)

---

STEPS:

1. Define or update the domain entity.
   File: `src/AiTicketHub/Domain/Entities/{EntityName}.cs`
   - Add only properties required by this endpoint.
   - Keep the entity free of validation logic and 
     infrastructure concerns.

2. Create the request DTO.
   File: `src/AiTicketHub/Application/DTOs/
          {OperationName}Request.cs`
   - Use a C# record.
   - No validation attributes or logic — validation 
     belongs in the validator only.

3. Create the response DTO.
   File: `src/AiTicketHub/Application/DTOs/
          {OperationName}Response.cs`
   - Use a C# record.
   - Include only fields the caller needs.

4. Write the FluentValidation validator.
   File: `src/AiTicketHub/Application/Validators/
          {OperationName}Validator.cs`
   - Extend `AbstractValidator<{OperationName}Request>`.
   - Add one rule per business constraint from 
     {BUSINESS_RULES}.
   - Every rule must call `.WithMessage()` with a 
     human-readable message.

5. Add the service method.
   a. Interface:
      File: `src/AiTicketHub/Application/Interfaces/
             I{EntityName}Service.cs`
      - Add the method signature for this operation.
      - Use async signature:
        `Task<Result<{OperationName}Response>>`.
   b. Implementation:
      File: `src/AiTicketHub/Application/Services/
             {EntityName}Service.cs`
      - Add the method body to the existing service,
        or create the class if it does not exist.
      - Inject and call `I{EntityName}Repository` only.
      - Return `Result<T>.Failure()` for all business-rule
        violations.
      - Return `Result<T>.Success(response)` on happy path.

6. Define or extend the repository interface.
   File: `src/AiTicketHub/Application/Interfaces/
          I{EntityName}Repository.cs`
   - Add only the method(s) required by this service method.
   - Use async signatures (`Task<T>`) for all methods.

7. Implement the repository method.
   File: `src/AiTicketHub/Infrastructure/Repositories/
          {EntityName}Repository.cs`
   - Use `ConcurrentDictionary` for storage.
   - Contain ALL thread-safety logic here and nowhere else.

8. Add the controller action.
   File: `src/AiTicketHub/API/Controllers/
          {EntityName}Controller.cs`
   - Add action to the EXISTING controller — never
     create a duplicate controller file.
   - Validation runs automatically via
     `FluentValidationAutoValidation`; the action body
     is only reached with a valid model.
   - Call `I{EntityName}Service`; map every
     `Result<T>.Failure` to the uniform error envelope
     `{"code":"...","message":"..."}` using the
     error-code switch defined in CONTEXT.md.
   - POST success → `201 Created` with `Location` header.
   - All other successes → `200 OK`.

9. Register new services in the DI container.
   File: `src/AiTicketHub/API/Program.cs`
   - Register handler, validator, and repository only 
     if not already registered.
   - Output only the changed or added lines with 
     surrounding context.

10. Write unit tests for the service method.
    File: `tests/AiTicketHub.Tests/Application/
           {EntityName}ServiceTests.cs`
    - Use Moq to mock `I{EntityName}Repository`.
    - Cover: happy path, each business-rule failure, 
      not-found (if applicable).
    - Use FluentAssertions exclusively.

11. Write controller integration tests.
    File: `tests/AiTicketHub.Tests/API/
           {EntityName}ControllerTests.cs`
    - Use `WebApplicationFactory<Program>`.
    - Assert HTTP status codes and response body shapes 
      for every scenario.
    - Use FluentAssertions exclusively.

12. Check your work against CONSTRAINTS before finishing.

---

CONSTRAINTS:

Note: All agent-level constraints from developer.md apply
to every step above. The following are endpoint-specific.

- NEVER omit `.WithMessage()` on any FluentValidation rule
- NEVER put validation logic inside the domain entity, 
  handler, or controller — validator only
- NEVER access `ConcurrentDictionary` outside the 
  Infrastructure repository class
- NEVER skip the not-found scenario in any handler that 
  looks up an entity by ID
- NEVER create a new controller file for an entity that 
  already has one — add the action to the existing file
- NEVER register a service that is already registered 
  in `Program.cs`

---

OUTPUT_FORMAT:

Output each file as a fenced code block with:
- The language tag (```csharp)
- The workspace-relative file path as a comment on 
  the first line inside the block

Produce files in this order:

1.  Domain/Entities/{EntityName}.cs
2.  Application/DTOs/{OperationName}Request.cs
3.  Application/DTOs/{OperationName}Response.cs
4.  Application/Validators/{OperationName}Validator.cs
5.  Application/Interfaces/I{EntityName}Service.cs        (new or updated)
6.  Application/Services/{EntityName}Service.cs           (new or updated method)
7.  Application/Interfaces/I{EntityName}Repository.cs     (new or updated)
8.  Infrastructure/Repositories/{EntityName}Repository.cs
9.  API/Controllers/{EntityName}Controller.cs
10. API/Program.cs (changed lines only)
11. Tests/Application/{EntityName}ServiceTests.cs
12. Tests/API/{EntityName}ControllerTests.cs

After the last file add a `## Self-review checklist` section.
Re-state each CONSTRAINT as a checkbox and mark it
`[x]` (satisfied) or `[ ]` (violated) based on the code
you just generated. Fix any `[ ]` items before responding.