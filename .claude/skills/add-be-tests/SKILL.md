---
name: add-be-tests
description: Backfill missing tests for existing use cases in the BE — domain unit tests, FluentValidation validator unit tests, and HTTP integration tests. Use when the user asks to add, improve, or backfill test coverage.
argument-hint: <feature to cover, e.g. "CreateGame">
---

# Add Tests for an Existing Use Case

Backfill the three test types this template expects for every slice. Read the target handler/validator/endpoint first, then mirror the structure of the closest existing test class.

## Workflow

1. **Locate the slice.** Find the command/query, handler, validator, and endpoint for the target use case. List every distinct outcome (each behavioral branch)
2. **Check what already exists (in `tests/{Module}.UnitTests/` and `tests/IntegrationTests/{Module}/`)** - extend existing classes, don't duplicate.
3. **Write domain unit tests (in `tests/{Module}.UnitTests/`)** - test for every branch.
4. **Write validator tests (in `tests/IntegrationTests/{Module}/`)**
5. **Write integration tests (in `tests/IntegrationTests/{Module}/`)**
6. **Run** `dotnet test --filter "FullyQualifiedName!~E2E"` integration tests use testcontainers, so Docker/Podman must be running. Fix failures before finishing.

## Conventions

- **Frameworks:** xUnit + Shouldly
- **Integration Tests:**
    - Global usings already cover `Xunit`, `Shouldly`, `System.Net` and `System.Net.Http.Json`.
    - each test starts with a clean database (Respawn)
    - test for every failing branch with state asserted via a database check (unless there is no need to)
    - if a call in the handler may return multiple error types but the handler handles them the same way e.g. `if (joinGameResult.IsFailure)return Result.Failure<JoinGameResponse>(joinGameResult.Error);` then this is only one behavioral branch
    - happy path with state asserted via a follow-up GET (if GET is not present assert via a database check and if GET is planned leave "TODO: replace with GET assertion")
    - all tests over real HTTP
    - see `BaseIntegrationTest` for utility functions
    - naming:
        - file - `{Module}/{Feature}IntegrationTests.cs`
        - methods - `{Feature}_State_ExpectedOutcome`
- **Validator Tests:**
    - validator tests are in the integration tests project but do not inherit `BaseIntegrationTest`
    - use `FluentValidation.TestHelper`
    - 2 tests per validator (one [Theory] to group invalid inputs (one [InlineData] per rule) and one [Fact] for valid input)
    - naming:
        - file `{Module}/{ValidatorName}Tests.cs`
        - methods `{ValidatorName}_InvalidInput_Fails` or `{ValidatorName}_ValidInput_Succeeds`
- **Structure:** `// Arrange` / `// Act` / `// Assert` comments in every test.
