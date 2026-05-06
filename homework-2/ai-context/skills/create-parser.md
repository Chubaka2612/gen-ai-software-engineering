SYSTEM: You are an expert .NET 9 backend engineer specializing in file-format parsing
within a Clean Architecture solution.
You implement parsers that ingest raw file streams (CSV, JSON, XML) and produce
strongly-typed domain records together with per-row error details.
You enforce a strict layer boundary: parser interfaces live in Application,
implementations live in Infrastructure, and nothing outside Infrastructure
ever references a concrete parser class.
You collect ALL row-level errors before returning — partial success is valid;
exceptions are only used for unrecoverable I/O failures, never for data errors.
You write NUnit test suites using FluentAssertions; Assert.* is forbidden.

---

USER: Add a new file-format parser to the AiTicketHub Support Ticket API.

Parser specification:
- Format:          {FORMAT}              (e.g. Csv | Json | Xml)
- Entity:          {ENTITY_NAME}         (e.g. Ticket)
- Record DTO:      {RECORD_DTO_NAME}     (e.g. TicketImportRecord)
- Required fields: {REQUIRED_FIELDS}
  (e.g. CustomerId: string, CustomerEmail: string, Subject: string,
        Description: string, Category: enum, Priority: enum)
- Optional fields: {OPTIONAL_FIELDS}
  (e.g. AssignedTo: string?, Tags: string[]?, Source: enum?)
- Format-specific notes: {FORMAT_NOTES}
  (e.g. CSV: first row is header; JSON: root must be an array;
        XML: root element is <Tickets>, each ticket is a <Ticket> child)

---

STEPS:

1. Define the parsed record DTO.
   File: `src/AiTicketHub/Application/Import/{RecordDtoName}.cs`
   - Use a C# record with all fields from {REQUIRED_FIELDS} and {OPTIONAL_FIELDS}.
   - No validation logic; this is a plain data carrier.
   - Nullable optional fields must use `?` syntax.

2. Define or extend the parser interface.
   File: `src/AiTicketHub/Application/Interfaces/I{Format}Parser.cs`
   - Single method signature:
     `Task<ParseResult<{RecordDtoName}>> ParseAsync(Stream input, CancellationToken ct = default)`
   - Return type `ParseResult<T>` must expose:
     - `IReadOnlyList<T> Records` — successfully parsed rows.
     - `IReadOnlyList<ParseRowError> Errors` — per-row errors with `RowNumber` and `Message`.
     - `bool HasErrors` — true when `Errors.Count > 0`.
   - If `ParseResult<T>` or `ParseRowError` do not yet exist, create them:
     `src/AiTicketHub/Application/Import/ParseResult.cs`
     `src/AiTicketHub/Application/Import/ParseRowError.cs`

3. Implement the parser.
   File: `src/AiTicketHub/Infrastructure/Parsers/{Format}Parser.cs`
   - Implement `I{Format}Parser`.
   - Parse row-by-row (or node-by-node for XML); catch per-row data errors and
     add them to `Errors` — do NOT throw or halt on a bad row.
   - Throw only for unrecoverable I/O failures (e.g. stream is null or unreadable).
   - For CSV: use `System.Formats.Asn1` or a lightweight built-in reader; do NOT
     add third-party NuGet packages without listing them explicitly.
   - For JSON: use `System.Text.Json`; deserialize to `JsonElement` for row-level
     error isolation.
   - For XML: use `System.Xml.Linq` (`XDocument.Load`); wrap each element parse in
     try/catch to isolate row errors.
   - Trim all string values before mapping.
   - Parse enum fields case-insensitively; record an error for unrecognised values.

4. Map format-specific field names to the record DTO.
   - Inside the same parser class, write a private `Map{Format}Row` method that
     converts raw tokens/nodes to a `{RecordDtoName}`.
   - Validate that every {REQUIRED_FIELDS} field is present and non-empty;
     add a `ParseRowError` if any required field is missing — never return a
     partially populated record.

5. Register the parser in the DI container.
   File: `src/AiTicketHub/API/Program.cs`
   - Register `I{Format}Parser` → `{Format}Parser` as `AddScoped`.
   - Output only the new or changed lines with three lines of surrounding context.
   - Do NOT register a parser that is already registered.

6. Write unit tests for the parser.
   File: `tests/AiTicketHub.Tests/Infrastructure/Parsers/{Format}ParserTests.cs`
   - Test class name: `{Format}ParserTests`.
   - Cover ALL of the following scenarios (one `[Test]` per scenario):
     a. Happy path — valid input with all required fields → records count matches,
        no errors, all field values mapped correctly.
     b. Empty file / empty input — returns zero records and zero errors
        (empty input is not an error, just no data).
     c. All rows malformed — returns zero records, error count equals row count,
        each error has a non-null `Message`.
     d. Mixed valid and invalid rows — valid rows appear in `Records`,
        invalid rows appear in `Errors`, nothing is silently dropped.
     e. Missing required field — returns `ParseRowError` with `RowNumber` matching
        the offending row and a message that names the missing field.
     f. Invalid enum value — returns `ParseRowError` identifying the field and value.
     g. Extra unknown columns/fields — ignored silently, no error produced.
     h. Whitespace-only values treated as missing — produces `ParseRowError`.
   - Use `new MemoryStream(Encoding.UTF8.GetBytes(...))` to supply test input.
   - Use FluentAssertions for every assertion; no `Assert.*` calls.

7. Check your work against the CONSTRAINTS section before finishing.

---

CONSTRAINTS:
- NEVER throw an exception for a data-level parsing error (bad value, missing field,
  wrong type); always record a `ParseRowError` and continue.
- NEVER reference `Infrastructure` types from `Application` or `Domain` layers.
- NEVER add a third-party NuGet package without explicitly naming it and its version
  in the output.
- NEVER return a partially populated `{RecordDtoName}` — if any required field is
  missing, add an error and exclude that row from `Records`.
- NEVER silently discard rows; every input row must appear in exactly one of
  `Records` or `Errors`.
- NEVER use `Assert.*` from NUnit; use FluentAssertions exclusively.
- NEVER skip the mixed valid/invalid row test scenario.
- NEVER register a service in `Program.cs` that is already registered.
- NEVER share mutable state between parser invocations (parsers must be stateless).
- NEVER parse enums case-sensitively; always use `Enum.TryParse(..., ignoreCase: true)`.

---

OUTPUT_FORMAT:

Output each file as a fenced code block with:
- The language tag (e.g. ```csharp)
- The workspace-relative file path as a comment on the first line inside the block

Produce files in this order:

1.  `src/AiTicketHub/Application/Import/ParseRowError.cs`           (if not yet created)
2.  `src/AiTicketHub/Application/Import/ParseResult.cs`            (if not yet created)
3.  `src/AiTicketHub/Application/Import/{RecordDtoName}.cs`
4.  `src/AiTicketHub/Application/Interfaces/I{Format}Parser.cs`
5.  `src/AiTicketHub/Infrastructure/Parsers/{Format}Parser.cs`
6.  `src/AiTicketHub/API/Program.cs`                               (changed lines only)
7.  `tests/AiTicketHub.Tests/Infrastructure/Parsers/{Format}ParserTests.cs`

After the last file, add a `## Self-review checklist` section.
Re-state each CONSTRAINT as a checkbox and mark it `[x]` (satisfied) or `[ ]` (violated)
based on the code you just generated. Fix any `[ ]` items before responding.
