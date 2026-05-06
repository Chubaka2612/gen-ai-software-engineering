SYSTEM: You are the Test Engineer agent for AiTicketHub.
Your goal is to generate a complete test suite for one
operation covering all four test categories in one pass.

---

USER: Generate the full test suite for one operation
in the AiTicketHub Support Ticket API.

Operation specification:
- Entity:         {ENTITY_NAME}
  (e.g. Ticket)
- Operation name: {OPERATION_NAME}
  (e.g. CreateTicket)
- Service method: {SERVICE_METHOD_SIGNATURE}
  (e.g. CreateAsync(CreateTicketRequest request):
        Task<Result<CreateTicketResponse>>)
- Endpoint:       {HTTP_METHOD} {ROUTE}
  (e.g. POST /api/tickets)
- Success status: {SUCCESS_STATUS}
  (e.g. 201 Created | 200 OK)
- Happy path:     {HAPPY_PATH}
  (e.g. Valid request → ticket persisted,
        response contains Id and CreatedAt)
- Failure paths:  {FAILURE_PATHS}
  (e.g.
    Validation.Failed → 400 → invalid request fields
    Ticket.Duplicate  → 409 → duplicate subject
    Ticket.NotFound   → 404 → no ticket with given Id)
- Validator rules: {VALIDATOR_RULES}
  (e.g.
    Subject:     NotEmpty; MaximumLength(200)
    Description: NotEmpty; MinimumLength(10);
                 MaximumLength(2000)
    Priority:    IsInEnum)
- Repository methods to unit-test: {REPOSITORY_METHODS}
  (e.g. AddAsync(Ticket): Task<Result<Ticket>>
        GetByIdAsync(Guid): Task<Result<Ticket>>)

---

STEPS:

1. Derive the test plan before writing any code.
   - Read service:    Application/Services/
                      {EntityName}Service.cs
   - Read validator:  Application/Validators/
                      {OperationName}Validator.cs
   - Read interface:  Application/Interfaces/
                      I{EntityName}Repository.cs
   - Output numbered list grouped by file:
       Service tests:     one line per code path
       Validator tests:   one line per field × rule
       Repository tests:  one line per method × outcome
       Integration tests: one line per HTTP scenario
   - Do not write any C# until this list is complete.

2. Write service unit tests.
   File: tests/AiTicketHub.Tests/Application/
         {EntityName}ServiceTests.cs
   - [TestFixture] class: {EntityName}ServiceTests
   - [SetUp]: create Mock<I{EntityName}Repository>
     named _repositoryMock, instantiate
     {EntityName}Service _sut.
   - Happy path: mock returns Result<T>.Success(entity),
     assert IsSuccess then every Value field.
   - Each failure path: mock returns Result<T>.Failure,
     assert IsFalse then Error.Code exactly.
   - Validation.Failed path: pass invalid request,
     assert Error.Code == "Validation.Failed".
   - Naming: MethodName_StateUnderTest_ExpectedBehavior

3. Write validator unit tests.
   File: tests/AiTicketHub.Tests/Application/
         {OperationName}ValidatorTests.cs
   - [TestFixture] class: {OperationName}ValidatorTests
   - [SetUp]: instantiate real validator, no mocks.
   - One [Test] for fully valid request →
     IsValid.Should().BeTrue().
   - One [Test] per rule violation — only that field
     invalid, all others valid → ContainSingle check
     on PropertyName.
   - For Min/MaxLength: test boundary (must pass)
     AND violation (must fail) separately.

4. Write repository unit tests.
   File: tests/AiTicketHub.Tests/Infrastructure/
         {EntityName}RepositoryTests.cs
   - [TestFixture] class: {EntityName}RepositoryTests
   - [SetUp]: real repository with fresh
     ConcurrentDictionary — no mocks.
   - Happy path per method: pre-seed if needed,
     assert IsSuccess and field values.
   - Failure path per method: trigger the failure
     condition, assert Error.Code exactly.
   - Concurrency test for every write method:
     two simultaneous Task.Run calls, assert
     dictionary ends in consistent state.

5. Write controller integration tests.
   File: tests/AiTicketHub.Tests/API/
         {EntityName}ControllerTests.cs
   - [TestFixture] class: {EntityName}ControllerTests
   - [SetUp]: _factory = new WebApplicationFactory
     <Program>(); _client = _factory.CreateClient();
   - [TearDown]: dispose client then factory.
   - Real DI container — do not mock any service.
   - Happy path: assert status code, deserialize body,
     assert every expected field. For 201 assert
     Location header is not null.
   - 400 path: one invalid field, assert BadRequest,
     body contains "Validation.Failed".
   - Each non-400 failure: assert correct status code,
     body contains exact error code string.
   - Use compile-time constant for base route:
     private const string BaseRoute = "/api/tickets";

6. Check your work against CONSTRAINTS before finishing.

---

CONSTRAINTS:

Note: All agent-level constraints from test-engineer.md
apply to every step above. The following are task-specific.

- NEVER write a [Test] that exercises more than one
  distinct code path — one path per test
- NEVER invent an error code — use only codes from
  {FAILURE_PATHS} or the Architect's error catalogue
- NEVER mock the service layer in integration tests —
  WebApplicationFactory uses the real DI container
- NEVER test a private method — public methods only
- NEVER omit a failure-path test — every error code
  in {FAILURE_PATHS} needs its own dedicated [Test]
- NEVER test two validator rules failing simultaneously
  — each rule violation gets its own isolated [Test]
- NEVER call .Result or .Wait() — always await async
  methods and mark test methods async Task

---

OUTPUT_FORMAT:

Test plan from STEP 1 as a numbered list first.

Then each file as a fenced csharp code block with
the file path as a comment on the first line inside:
// tests/AiTicketHub.Tests/Application/
//   TicketServiceTests.cs

Produce files in this order:
1. Application/{EntityName}ServiceTests.cs
2. Application/{OperationName}ValidatorTests.cs
3. Infrastructure/{EntityName}RepositoryTest