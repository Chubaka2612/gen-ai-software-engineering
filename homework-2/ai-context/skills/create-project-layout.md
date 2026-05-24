SYSTEM: You are the Architect agent for AiTicketHub.
Your goal is to produce the one-time solution bootstrap:
solution file, four .csproj files with correct project
references and NuGet packages, foundational Domain types
(Result<T>, Error), and empty DI extension method stubs.
No entity code. No DTOs. No business logic. No tests.
Run this skill once per project, before any other skill.
After this skill, run create-structure.md for each entity.

---

USER: Bootstrap the AiTicketHub solution.

Project specification:
- Solution name:    {SOLUTION_NAME}
  (e.g. AiTicketHub)
- Target framework: {DOTNET_VERSION}
  (e.g. net9.0)
- Packages:
    Application layer:  FluentValidation 11.x
    API layer:          FluentValidation.AspNetCore 11.x
    Test project:       NUnit 4.x, NUnit3TestAdapter,
                        FluentAssertions 6.x, Moq 4.x,
                        Microsoft.AspNetCore.Mvc.Testing

---

STEPS:

1. Create the solution file and folder structure.
   File: `{SolutionName}.sln`
   - Reference all five projects (Domain, Application,
     Infrastructure, API, Tests).
   - Create these folders (empty, with .gitkeep):
       src/{SolutionName}/Domain/Entities/
       src/{SolutionName}/Domain/Enums/
       src/{SolutionName}/Domain/Common/
       src/{SolutionName}/Application/Interfaces/
       src/{SolutionName}/Application/Services/
       src/{SolutionName}/Application/DTOs/
       src/{SolutionName}/Application/Validators/
       src/{SolutionName}/Infrastructure/Repositories/
       src/{SolutionName}/API/Controllers/
       src/{SolutionName}/API/Middleware/
       src/{SolutionName}/API/Extensions/
       tests/{SolutionName}.Tests/Application/
       tests/{SolutionName}.Tests/Infrastructure/
       tests/{SolutionName}.Tests/API/

2. Create the Domain project file.
   File: `src/{SolutionName}/Domain/
          {SolutionName}.Domain.csproj`
   - Target framework: {DOTNET_VERSION}.
   - No project references.
   - No NuGet packages — only System.* is allowed here.

3. Create the Application project file.
   File: `src/{SolutionName}/Application/
          {SolutionName}.Application.csproj`
   - Target framework: {DOTNET_VERSION}.
   - Project reference: {SolutionName}.Domain.
   - NuGet packages: FluentValidation 11.x.

4. Create the Infrastructure project file.
   File: `src/{SolutionName}/Infrastructure/
          {SolutionName}.Infrastructure.csproj`
   - Target framework: {DOTNET_VERSION}.
   - Project reference: {SolutionName}.Application only.
   - No NuGet packages.

5. Create the API project file.
   File: `src/{SolutionName}/API/
          {SolutionName}.API.csproj`
   - Target framework: {DOTNET_VERSION}.
   - Project references: {SolutionName}.Application,
     {SolutionName}.Infrastructure.
   - NuGet packages: FluentValidation.AspNetCore 11.x.

6. Create the test project file.
   File: `tests/{SolutionName}.Tests/
          {SolutionName}.Tests.csproj`
   - Target framework: {DOTNET_VERSION}.
   - Project reference: {SolutionName}.API
     (needed for WebApplicationFactory).
   - NuGet packages: NUnit 4.x, NUnit3TestAdapter,
     FluentAssertions 6.x, Moq 4.x,
     Microsoft.AspNetCore.Mvc.Testing.
   - No production NuGet packages (no FluentValidation,
     no application-layer packages).

7. Create the foundational Domain types.
   File: `src/{SolutionName}/Domain/Common/Error.cs`
   - `public record Error(string Code, string Message);`
   File: `src/{SolutionName}/Domain/Common/Result.cs`
   - Generic `Result<T>` and non-generic `Result` classes.
   - Static factory methods: `Success(T value)`,
     `Failure(Error error)`.
   - Read-only properties: `IsSuccess`, `Value`, `Error`.
   - No other methods or logic.

8. Create the DI extension method stubs.
   File: `src/{SolutionName}/API/Extensions/
          ApplicationServiceExtensions.cs`
   - One public static class with one method:
     `AddApplicationServices(this IServiceCollection services)`
   - Empty body — no registrations yet.
   File: `src/{SolutionName}/API/Extensions/
          InfrastructureServiceExtensions.cs`
   - One public static class with one method:
     `AddInfrastructureServices(this IServiceCollection services)`
   - Empty body — no registrations yet.

9. Create the API entry point.
   File: `src/{SolutionName}/API/Program.cs`
   - Call `builder.Services.AddApplicationServices()`.
   - Call `builder.Services.AddInfrastructureServices()`.
   - Call `services.AddValidatorsFromAssemblyContaining
     <Program>()` with `AddFluentValidationAutoValidation()`.
   - Call `app.UseRouting()` and `app.MapControllers()`.
   - No entity registrations — those belong in the
     extension methods, filled by implement-feature.md.

10. Check your work against CONSTRAINTS before finishing.

---

CONSTRAINTS:

Note: All agent-level constraints from architect.md apply
to every step above. The following are task-specific.

- NEVER add a project reference that violates the dependency
  rule: Domain has none; Application references Domain only;
  Infrastructure references Application only; API references
  Application and Infrastructure; Tests references API only
- NEVER add a NuGet package to a layer that does not need it:
  no FluentValidation in Domain or Infrastructure; no Moq,
  NUnit, or FluentAssertions in any production project
- NEVER write a method body in the DI extension stubs —
  empty curly braces only; bodies are filled by
  implement-feature.md
- NEVER write entity classes, DTOs, interfaces, validators,
  controllers, or repository classes — those belong to
  create-structure.md and implement-feature.md
- NEVER put business logic or entity registrations in
  Program.cs — only the three wiring calls from STEP 9
- NEVER reference {SolutionName}.Infrastructure from
  {SolutionName}.Domain or {SolutionName}.Application

---

OUTPUT_FORMAT:

Output solution and project files as fenced xml blocks.
Output C# files as fenced csharp blocks.
Each block has the workspace-relative file path as a
comment on the first line inside the block.

Produce files in this order:

1.  {SolutionName}.sln
2.  src/{SolutionName}/Domain/{SolutionName}.Domain.csproj
3.  src/{SolutionName}/Application/
    {SolutionName}.Application.csproj
4.  src/{SolutionName}/Infrastructure/
    {SolutionName}.Infrastructure.csproj
5.  src/{SolutionName}/API/{SolutionName}.API.csproj
6.  tests/{SolutionName}.Tests/
    {SolutionName}.Tests.csproj
7.  src/{SolutionName}/Domain/Common/Error.cs
8.  src/{SolutionName}/Domain/Common/Result.cs
9.  src/{SolutionName}/API/Extensions/
    ApplicationServiceExtensions.cs
10. src/{SolutionName}/API/Extensions/
    InfrastructureServiceExtensions.cs
11. src/{SolutionName}/API/Program.cs

After the last file, output the directory tree from STEP 1
as a plain fenced text block.

Then add a `## Self-review checklist` section.
Re-state each CONSTRAINT as a checkbox and mark it
`[x]` (satisfied) or `[ ]` (violated). Fix any `[ ]`
before responding.

Always end with:
"Project layout is ready. Run create-structure.md to
design the first entity."
