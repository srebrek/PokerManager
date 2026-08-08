# PokerManager

.NET modular monolith: Aspire + Minimal API (Wolverine) + Blazor WASM PWA (MudBlazor) + PostgreSQL + cookie auth.

## Getting started

Prereqs: .NET SDK pinned in `global.json`, Docker (or Podman), [Aspire CLI](https://aspire.dev), a Chromium browser (for WASM debugging).

```bash
aspire run        # or: F5 in VS Code ("Aspire: AppHost")
```

## Tests

```bash
dotnet test                                      # what CI runs — every test project, E2E included
dotnet run scripts/coverage.cs                           # same, plus a coverage summary and HTML report
dotnet test --filter-not-namespace E2ETests --ignore-exit-code 8   # skip the slow AppHost smoke test
dotnet test --project tests/E2ETests             # only the AppHost smoke test
```
