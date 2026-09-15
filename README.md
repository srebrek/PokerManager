# PokerManager

.NET modular monolith: Aspire + Minimal API (Wolverine) + Blazor WASM PWA (MudBlazor) + PostgreSQL + cookie auth.

Live at: https://poker.zlotekmikolaj.com

## Showcase

### Backend
- ASP.NET Core 10
- Aspire (with deployment)
- EF Core
- Wolverine for messaging
- Modular Monolith
- DDD
- Vertical Slices
- SignalR

### Frontend
- Blazor WASM
- MudBlazor
- Vertical Slices
- SignalR

### Tests
- Diamond Structure
- E2E tests with Aspire
- Parallel and isolated integration tests
- bUnit component tests

### Deployment
- Deploy via Aspire
- Github actions
- Azure Container Apps
- Postgres database

## Getting started

Prereqs: .NET SDK pinned in `global.json`, Docker (or Podman), [Aspire CLI](https://aspire.dev), a Chromium browser (for WASM debugging).

```bash
aspire run        # or: F5 in VS Code ("Aspire: AppHost")
```

## Tests

```bash
# what CI runs — every test project, E2E included
dotnet test
# same, plus a coverage summary and HTML report
dotnet run scripts/coverage.cs
# skip the slow AppHost smoke test
dotnet test --filter-not-namespace E2ETests --ignore-exit-code 8
# only the AppHost smoke test
dotnet test --project tests/E2ETests
```
