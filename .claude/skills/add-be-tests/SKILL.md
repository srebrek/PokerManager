---
name: add-be-tests
description: Backfill missing tests for existing use cases in the BE — domain unit tests, FluentValidation validator unit tests, and HTTP integration tests. Use when the user asks to add, improve, or backfill test coverage.
argument-hint: <feature to cover, e.g. "CreateGame">
---

# Add Tests for an Existing Use Case

Backfill the three test types this template expects for every slice. Read the target handler/validator/endpoint first, then mirror the structure of the closest existing test class.

## What each layer tests (no overlap)

- **Domain unit tests** - domain logic: guards, invariants, state transitions, each distinct domain error.
- **Validator tests** - validation rules.
- **Integration tests** - handler orchestration only: the branches visible in the handler code.
- Validation short-circuit and Error -> HTTP status mapping are tested once app-wide, never per feature.
  If that app-wide test is missing, flag it to the user instead of adding it to the feature.

## Workflow

1. **Locate the slice.** Find the command/query, handler, validator, endpoint and the domain methods the handler calls.
2. **Check what already exists (in `tests/{Module}.UnitTests/` and `tests/IntegrationTests/{Module}/`)** - extend existing classes, don't duplicate.
3. **Write domain unit tests (in `tests/{Module}.UnitTests/`)**
4. **Write validator tests (in `tests/IntegrationTests/{Module}/`)**
5. **Write integration tests (in `tests/IntegrationTests/{Module}/`)**
6. **Run** `dotnet test --filter-not-namespace E2ETests --ignore-exit-code 8` integration tests use testcontainers, so Docker/Podman must be running. Fix failures before finishing.

## Conventions

- **Frameworks:** xUnit + Shouldly
- **Integration Tests:**
    - Global usings already cover `Xunit`, `Shouldly`, `System.Net` and `System.Net.Http.Json`.
    - each test starts with a clean database (Respawn)
    - one test per handler branch, e.g.:
      ```csharp
      if (game is null) return GameErrors.GameNotFound;    // test: not found
      if (result.TryGetError(out var error)) return error; // test: ONE domain failure, whichever error
      return Result.Success();                             // test: happy path
      ```
    - a handler without branches gets the happy path plus meaningful data variants (e.g. empty result)
    - no tests for invalid input (400) — validator tests cover it
    - Act calls the tested endpoint over HTTP; Arrange and Assert use helpers that call handlers directly
    - happy path asserts state via a query handler (if none exists assert via a database check and if one is planned leave "TODO: replace with query handler assertion")
    - see `BaseIntegrationTest` for utility functions
    - naming:
        - file - `{Module}/{Feature}IntegrationTests.cs`
        - methods - `{Feature}_State_ExpectedOutcome`
- **Validator Tests:**
    - plain unit tests; they live in the integration tests project only to sit with other application-layer tests — do not inherit `BaseIntegrationTest`
    - use `FluentValidation.TestHelper`
    - 2 tests per validator (one [Theory] to group invalid inputs (one [InlineData] per rule) and one [Fact] for valid input)
    - naming:
        - file `{Module}/{ValidatorName}Tests.cs`
        - methods `{ValidatorName}_InvalidInput_Fails` or `{ValidatorName}_ValidInput_Succeeds`
- **Domain Unit Tests**
    - one test per branch of a method that has logic
    - skip members without logic: factories/constructors that only assign fields, strongly typed ids, getters
    - naming:
        - file: `{Class}Tests/{Method}Tests.cs`
        - method: `{Method}_State_ExpectedOutcome`
- **Structure:** `// Arrange` / `// Act` / `// Assert` comments in every test.
