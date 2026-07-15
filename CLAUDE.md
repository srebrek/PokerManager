# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## What this is

PokerManager: .NET 10 modular monolith for tracking friendly poker games (pot/stack sizes, settlement,
stats). Aspire orchestration + Minimal API (Wolverine) backend + Blazor WASM PWA (MudBlazor) frontend +
PostgreSQL + cookie auth. Functional requirements live in `docs/requirements.md`.

## Commands

```bash
aspire run                                       # run everything (or F5 "Aspire: AppHost" in VS Code)
dotnet build --configuration Release
dotnet format --verify-no-changes --no-restore   # CI formatting gate — run before committing

dotnet test --filter "FullyQualifiedName!~E2E"   # what CI runs (unit + integration + arch tests)
dotnet test tests/E2E.Tests                      # full AppHost smoke test, local only (needs Docker/Podman)
dotnet test tests/Identity.UnitTests --filter "FullyQualifiedName~SomeTestClass.SomeMethod"   # single test

# new migration — no env vars needed (each module has a design-time DbContext factory)
dotnet ef migrations add <Name> --project src/Modules/<Module> --output-dir Infrastructure/Data/Migrations
```

Migrations are applied automatically at startup in Development. Integration tests spin up Postgres via
Testcontainers and reset state with Respawn — Docker/Podman must be running. E2E tests boot the real
Aspire AppHost and are not yet stable on hosted CI (`continue-on-error` in `ci.yml`).

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
  - `Features/<FeatureName>/` — vertical slice: `<Feature>Command` + `CommandHandler` + `CommandValidator`
    + `Endpoint` colocated in one folder. Handlers use the module's `DbContext` directly (no repositories).
  - `Domain/` — entities, value objects, domain services (Gameplay onward; Identity keeps its model in
    `Infrastructure` due to ASP.NET Core Identity coupling).
  - `Infrastructure/` — `DbContext` (in `Infrastructure.Data`), EF configurations, migrations, services.
  - `Abstractions/` — interfaces the module exposes to its own Features (e.g. `IUserAccountService`).
  - `Events/` — integration events the module publishes.
  - `Infrastructure` must not depend on `Features` (arch-enforced).
  - One static `<Name>Module.cs` with `Add<Name>Module(...)` / `Use<Name>Module(...)` extensions.
- `src/Shared` — cross-module kernel layered as `Domain`/`Application`/`Infrastructure`/`Presentation`
  with Clean Architecture dependency direction (arch-enforced). Contains `Entity`, `AggregateRoot`,
  `Result`/`Error` (result pattern — no exceptions for domain failures), `IEndpoint`, validation
  middleware, global exception handler, anti-CSRF/request-context middleware.
- `src/Contracts` — request/response DTOs shared between frontend and backend.
- `src/aspire/*` — Aspire AppHost + ServiceDefaults (OTel, health checks, service discovery).
- `src/Web.Frontend` — Blazor WASM PWA, cookie auth via `CookieAuthenticationStateProvider`.

### Conventions (arch-enforced)

- Commands/Queries: sealed public records named `<Feature>Command`/`Query`, in their `Features.<Feature>`
  namespace; handlers `<Feature>CommandHandler`/`QueryHandler`, public. Public is a hard Wolverine
  requirement (discovery excludes non-public types in every codegen mode) — same for event types.
- Validators: `AbstractValidator<T>`, named `<Feature>CommandValidator`, public (Wolverine codegen
  constructs them inline; internal would force service location — throws in Wolverine 6).
- Endpoints: implement `IEndpoint`, named `<Feature>Endpoint`, internal, colocated with their command.
- `DbContext` subclasses live in `<Module>.Infrastructure.Data`, named `*DbContext`, public (injected
  into handlers). Keep `DbSet` properties internal so the domain model doesn't leak outside the module.
- Domain events: implement `IDomainEvent`, end in `DomainEvent`. Integration events: `IIntegrationEvent`,
  end in `IntegrationEvent`.

### Visibility policy (arch-enforced)

Rule of thumb: **public only what Wolverine's generated code must reference; everything else internal.**
Wolverine compiles handler-chain adapters into a separate generated assembly — internal types are
invisible to it (discovery skips non-public handlers silently; internal scoped/transient dependencies
force a service-locator fallback).

Must be **public**:

- Message types: commands, queries, domain/integration events.
- Message handlers: `*CommandHandler`, `*QueryHandler`, `*EventHandler`.
- Validators (constructed inline by the validation middleware).
- `DbContext` subclasses (scoped handler dependency) — but their `DbSet` properties stay internal.
- Service implementations behind `Abstractions` interfaces (e.g. `UserAccountService`) — also eases
  testing. Prefer Singleton lifetime where the service is stateless; Scoped is fine but must be public.
- Types pulled into a public signature by the above (e.g. Identity `User`: base type arg of
  `IdentityDbContext`, ctor arg of `UserAccountService`) — each one is an explicit entry in
  `ModuleTests.PublicInfrastructureExceptions`.

Everything else **internal**: endpoints, the whole `Domain` model (entities, value objects, errors —
modules never see each other's domain), EF configurations, design-time factories, remaining
`Infrastructure` (migrations excepted). Enforced by `ModuleTests` + `ModifierTests`; CI additionally
runs `dotnet run --project src/Api.Bootstrapper -- codegen test` to prove all generated code compiles.

### Messaging / persistence

- Wolverine is the mediator: endpoints call `bus.InvokeAsync<Result>(command, ct)`; handlers return
  `Result`/`Result<T>`, mapped to HTTP via `result.Match(...)`/`CustomResults.Problem`.
- FluentValidation runs automatically via `ValidationMiddlewarePolicy` — handlers never validate manually.
- Domain events raised on aggregates (`AggregateRoot.Raise`, exposed via `IHasDomainEvents.Events`) are
  auto-published on EF `SaveChanges`; integration events are published explicitly via
  `IMessageContext`/`IMessageBus`. Wolverine persists messages durably in Postgres and wraps handlers in
  EF transactions.
- EF Core: Npgsql, snake_case naming, one `DbContext` + schema + migrations per module, shared connection
  string (`DatabaseConstants.ConnectionStringName`).

### Build settings

`Directory.Build.props`: nullable, warnings-as-errors, SonarAnalyzer, style enforced at build. Package
versions are centrally managed in `Directory.Packages.props` — never inline `Version=` attributes.

## Meta

- This project doubles as a test of the project template — if you find code that should not be here,
  flag it so the template can be fixed too.
- Follow best practices by Milan Jovanovic and Zoran Horvat.
