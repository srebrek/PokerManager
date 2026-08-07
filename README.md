# PokerManager

.NET modular monolith: Aspire + Minimal API (Wolverine) + Blazor WASM PWA (MudBlazor) + PostgreSQL + cookie auth.

## Getting started

Prereqs: .NET SDK pinned in `global.json`, Docker (or Podman), [Aspire CLI](https://aspire.dev), a Chromium browser (for WASM debugging).

```bash
aspire run        # or: F5 in VS Code ("Aspire: AppHost")
```

## Tests

```bash
dotnet test --filter "FullyQualifiedName!~E2E"   # what CI runs
dotnet test tests/E2ETests                       # full AppHost smoke test (local only)
```
