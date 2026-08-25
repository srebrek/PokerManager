# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## What this is

PokerManager: .NET 10 modular monolith for tracking friendly poker games (pot/stack sizes, settlement,
stats). Aspire orchestration + Minimal API (Wolverine) backend + Blazor WASM (MudBlazor) frontend +
PostgreSQL + cookie auth (backend only — the frontend has no auth UI yet). Functional requirements live
in `docs/requirements.md`.

## Commands

```bash
aspire run # run everything
dotnet build
dotnet format # fix whitespace errors
dotnet run scripts/build.cs -- --no-restore --configuration Release # production build
dotnet test # run all tests (we use MTP)
dotnet test --filter-not-namespace E2ETests --ignore-exit-code 8 # skip E2E tests
dotnet test --project tests/Identity.UnitTests --filter-method SomeMethod # single test
dotnet run scripts/coverage.cs # run tests and show coverage
# generate migration (in dev migrations are applied on startup)
dotnet ef migrations add <Name> --project src/Modules/<Module> --output-dir Infrastructure/Data/Migrations
```

## Architecture

**Modular monolith, vertical-slice style, one deployable ASP.NET Core process.** Each business module
(`src/Modules/*`) is its own assembly wired into `Api.Bootstrapper`. Modules never reference each other —
cross-module communication only via `Contracts` DTOs and Wolverine messages. Enforced by
`tests/ArchitectureTests` (ArchUnitNET). **When adding a module, register its assembly in
`BaseArchitectureTest.ModuleAssemblies`** or it is invisible to every architecture rule. Treat a failing
architecture test as a design signal, not an obstacle to suppress.

### Layout

- `src/Api.Bootstrapper` — the ASP.NET Core host: composes modules (`Add<Name>Module()`), configures
  Wolverine, serves the WASM frontend under `/` with API routes under `/api` (single origin so the
  HttpOnly auth cookie works for both).
- `src/Modules/<Name>` — one project per capability (`Identity`, `Gameplay`). Internal structure:
  - `Features/<FeatureName>/` — vertical slice: `<Feature>Command` + `CommandHandler` + `CommandValidator` + `Endpoint`
    or only `<Feature>Domain/IntegrationEventHandler` or other that can be counted as a feature colocated in one folder.
  - `Domain/` — entities, value objects, domain services (Gameplay onward; Identity keeps its model in
    `Infrastructure` due to ASP.NET Core Identity coupling).
  - `Infrastructure/` — `DbContext` (in `Infrastructure.Data`), EF configurations, migrations, services.
  - `Infrastructure` must not depend on `Features` (arch-enforced).
  - One static `<Name>Module.cs` with `Add<Name>Module(...)` / `Use<Name>Module(...)` extensions.
- `src/Shared` — cross-module kernel layered as `Domain`/`Application`/`Infrastructure`/`Presentation`
  with Clean Architecture dependency direction (arch-enforced). Contains `Entity`, `AggregateRoot`,
  `Result`/`Error` (result pattern — no exceptions for domain failures), `IEndpoint`, validation
  middleware, global exception handler, anti-CSRF/request-context middleware.
- `src/Contracts` — request/response DTOs and integration events shared between frontend and backend.
- `src/aspire/*` — Aspire AppHost + ServiceDefaults (OTel, health checks, service discovery).
- `src/Web.Frontend` — Blazor WASM. **No authentication yet**

### Conventions (arch-enforced)

- Commands/Queries: sealed **internal** records named `<Feature>Command`/`Query`, in their
  `Features.<Feature>` namespace; handlers `<Feature>CommandHandler`/`QueryHandler`, also internal. They
  never travel as Wolverine messages — the endpoint resolves the handler from DI and calls it, so nothing
  generated names them. Event types and event handlers are the ones that must stay public.
- Validators: `AbstractValidator<T>`, named `<Feature>CommandValidator`, internal — resolved via DI
  (`IEnumerable<IValidator<T>>` injected into the endpoint, registered with `includeInternalTypes: true`).
- Endpoints: implement `IEndpoint`, named `<Feature>Endpoint`, internal, colocated with their command.
- `DbContext` subclasses live in `<Module>.Infrastructure.Data`, named `*DbContext`, public (injected
  into handlers). Keep `DbSet` properties internal so the domain model doesn't leak outside the module.
- Domain events: implement `IDomainEvent`, end in `DomainEvent`. Integration events: `IIntegrationEvent`,
  end in `IntegrationEvent` (both public).

### Visibility policy (arch-enforced)

Rule of thumb: **public only what Wolverine's generated code must reference; everything else internal.**
Wolverine compiles handler-chain adapters into a separate generated assembly — internal types are
invisible to it (discovery skips non-public handlers silently; internal scoped/transient dependencies
force a service-locator fallback).

Must be **public** — only what an *event* chain names, since commands never reach the pipeline:

- Domain and integration event types.
- `*EventHandler` types and their `Handle` methods.
- `DbContext` subclasses (the type argument of `IDbContextOutbox<T>` in an event handler's signature) —
  but their `DbSet` properties stay internal.
- Service implementations behind `Abstractions` interfaces injected into an event handler (e.g.
  `UserAccountService`) — also eases testing. Prefer Singleton where the service is stateless.
- Types pulled into a public signature by the above (e.g. Identity `User`: base type arg of
  `IdentityDbContext`, ctor arg of `UserAccountService`) — each one is an explicit entry in
  `ModuleTests.PublicInfrastructureExceptions`.

Everything else **internal**: commands, queries, command/query handlers, endpoints, validators, the whole
`Domain` model (entities, value objects, errors — modules never see each other's domain), EF
configurations, design-time factories, remaining `Infrastructure` (migrations excepted). Enforced by
`ModuleTests` + `ModifierTests`. That all generated code compiles and loads statically is proven by
`dotnet run scripts/build.cs`, which CI runs on every push — see Production build.

### Messaging / persistence `docs/wolverine-transactions-and-events.md`

### Build settings

The project uses central package management `Directory.Packages.props` and `Directory.Build.props`

## Meta

- This project doubles as a test of the project template — if you find code that should not be here,
  flag it so the template can be fixed too.
- Follow best practices by Milan Jovanovic and Zoran Horvat.
- generate migrations only via `dotnet ef` and don't modify them (if you have to ask user for approval).
- do not modify `CLAUDE.md` unless asked to.
