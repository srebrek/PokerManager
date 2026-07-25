# CLAUDE.md

Guidance for Claude Code (claude.ai/code) when working in this repository.

## What this is

PokerManager: .NET 10 modular monolith for tracking friendly poker games (pot/stack sizes, settlement,
stats). Aspire orchestration + Minimal API (Wolverine) backend + Blazor WASM (MudBlazor) frontend +
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
- `src/Web.Frontend` — Blazor WASM, cookie auth via `CookieAuthenticationStateProvider`.

### Conventions (arch-enforced)

- Commands/Queries: sealed public records named `<Feature>Command`/`Query`, in their `Features.<Feature>`
  namespace; handlers `<Feature>CommandHandler`/`QueryHandler`, public. Public is a hard Wolverine
  requirement (discovery excludes non-public types in every codegen mode) — same for event types.
- Validators: `AbstractValidator<T>`, named `<Feature>CommandValidator`, internal — resolved via DI
  (`IEnumerable<IValidator<T>>` injected into the endpoint, registered with `includeInternalTypes: true`),
  not by Wolverine codegen.
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
- `DbContext` subclasses (scoped handler dependency) — but their `DbSet` properties stay internal.
- Service implementations behind `Abstractions` interfaces (e.g. `UserAccountService`) — also eases
  testing. Prefer Singleton lifetime where the service is stateless; Scoped is fine but must be public.
- Types pulled into a public signature by the above (e.g. Identity `User`: base type arg of
  `IdentityDbContext`, ctor arg of `UserAccountService`) — each one is an explicit entry in
  `ModuleTests.PublicInfrastructureExceptions`.

Everything else **internal**: endpoints, validators (`*CommandValidator` — resolved by DI as
`IEnumerable<IValidator<TCommand>>`, not constructed by Wolverine codegen, see below), the whole
`Domain` model (entities, value objects, errors — modules never see each other's domain), EF
configurations, design-time factories, remaining `Infrastructure` (migrations excepted). Enforced by
`ModuleTests` + `ModifierTests`. To prove all generated code compiles, run
`dotnet run --project src/Api.Bootstrapper --no-launch-profile -- codegen test`
with `ASPNETCORE_ENVIRONMENT=Production` and a dummy `ConnectionStrings__PokerManager-db` (CI does
not run this yet — see `docs/architecture-review.md` #18).

### Messaging / persistence

- Wolverine is the mediator: endpoints call `bus.InvokeValidatedAsync(command, validators, ct)`
  (`Shared.Infrastructure.Messaging.MessageBusExtensions`, an extension on `IMessageBus`), never bare
  `bus.InvokeAsync` — handlers return `Result`/`Result<T>`, mapped to HTTP via
  `result.Match(...)`/`CustomResults.Problem`.
- FluentValidation runs at the HTTP boundary, inside `InvokeValidatedAsync` itself, before the command
  reaches the bus/DB/transaction — never inside a Wolverine handler chain, handlers never validate
  manually. Enforced at compile time: `bus.InvokeAsync` is banned via `BannedSymbols.txt` (RS0030),
  so the only way to invoke a command is `InvokeValidatedAsync`. Do **not** add per-endpoint
  integration tests asserting that validation ran — the short-circuit and `ProblemDetails` shape are
  proven once in `CreateGameIntegrationTests`.
  An ArchUnitNET rule cannot replace the ban: the call sits in a compiler-generated lambda-closure
  class rather than the endpoint type, and `InvokeAsync` is declared on `ICommandBus`, not
  `IMessageBus` — both make the obvious rule silently match nothing.
- Validators stay an explicit `IEnumerable<IValidator<TCommand>>` parameter of the endpoint lambda, not
  resolved from `IServiceProvider` inside `InvokeValidatedAsync`: the generic parameter ties the
  collection to the command type, so a copy-pasted mismatch is a compile error.
- Domain events raised on aggregates (`AggregateRoot.Raise`, exposed via `IHasDomainEvents.Events`) are
  auto-published on EF `SaveChanges`; integration events are published explicitly via
  `IMessageContext`/`IMessageBus`. Wolverine persists messages durably in Postgres and wraps handlers in
  EF transactions.
- EF Core: Npgsql, snake_case naming, one `DbContext` + schema + migrations per module, shared connection
  string (`DatabaseConstants.ConnectionStringName`).

### Testing

Full rules and worked examples: `docs/testing-guidelines.md`. The decision rule:

- **1 integration happy path per slice**, through real HTTP and real Postgres. Mandatory.
- **Every other branch is a unit test** — unless the branch is unreachable without infrastructure (a
  query returning `null`, a LINQ filter, a unique index, a transaction/outbox/cascade), which makes it
  integration, one representative case.
- Ceiling **2–4 integration tests per slice**. Integration count must not grow with the number of
  domain branches — the diamond means every slice has an integration test, not every branch does.
- Domain (aggregates, value objects, guards): unit, **all** branches, no mocks, no DB.
- A handler that only does load → delegate → map gets **no test of its own**; unit-testing it would
  need an EF fake, which tests the fake.
- Shared mechanisms (`ErrorType` → HTTP status, validation short-circuit, CSRF, 401) are proven **once
  per application**; mark such a test as not-to-be-repeated per slice.
- A branch unreachable from HTTP is a design signal, not a coverage gap — fix the code instead.
- Never test what the compiler or an architecture test already enforces.

### Build settings

`Directory.Build.props`: nullable, warnings-as-errors, SonarAnalyzer, style enforced at build. Package
versions are centrally managed in `Directory.Packages.props` — never inline `Version=` attributes.

`BannedSymbols.txt` (root, wired to every project via `AdditionalFiles` in `Directory.Build.props`)
turns conventions that docs and PR review would otherwise guard into build errors (RS0030). Prefer it
over a test or a written rule whenever the convention is "never call this API". Docids must name the
**declaring** type, use `` `n `` for type arity and ``` ``n ``` for method arity, and every overload
needs its own line. Keep the include path prefixed with `$(MSBuildThisFileDirectory)` — a relative path
resolves against the importing project, silently points at a missing file and disables the ban.

Transitive package vulnerabilities (`NU1903`, error via warnings-as-errors) go in the dedicated
"Vulnerability overrides" `ItemGroup` at the bottom of `Directory.Packages.props`, each with an explicit
`PackageReference` (no version) in its own bottom `ItemGroup` in every project that actually resolves the
vulnerable transitive package — never in a `src` project with a `FrameworkReference`, that gets pruned
and hits `NU1510` instead (see git history on `Directory.Packages.props`/`Directory.Build.props` for the
full pruning-vs-audit story). `dotnet package update --vulnerable --project <path>` (`.NET 10` SDK,
one project at a time, needs `TreatWarningsAsErrors` off to restore) automates adding both entries — use
it to add new overrides. No automated tool removes stale ones: periodically delete an override entry and
its `PackageReference`, rebuild the affected project(s); if it still builds clean, the transitive minimum
has caught up and the override can stay gone.

## Meta

- This project doubles as a test of the project template — if you find code that should not be here,
  flag it so the template can be fixed too.
- Follow best practices by Milan Jovanovic and Zoran Horvat.
