# Wolverine: transactions, events and external API calls

Reference for anyone (human or agent) writing an endpoint, command handler or event handler in this
solution.

Verified empirically against **`WolverineFx* 6.24.4`** + EF Core 10 + PostgreSQL 17 in
`~/repos/wolverine-lab`, a rig that SIGKILLs itself at four points in the write path and restarts so the
durability agent can recover. Claims marked **[measured]** come from that rig
(`MATRIX-6.24.4.md`, 55 scenarios); **[source]** from reading the Wolverine source at `V6.24.4` and on
`main`. Re-verify after every Wolverine upgrade — all of this depends on generated-code ordering.

**The one-line version:** Wolverine is our **bus and durability layer**, not our mediator.
`IDbContextOutbox<T>` is the only thing that writes. Event handlers are idempotent.

---

## 1. The single mental model

Wolverine has **no concept of "domain event" versus "integration event"**. Both become the same
`Envelope`. The only thing that decides whether a published message is atomic with the business write is
**whether the envelope is persisted by the same `SaveChangesAsync` as the business row.**

`AddDbContextWithWolverineIntegration` maps the envelope tables into the EF model, so
`EfCoreEnvelopeTransaction.PersistOutgoingAsync` / `PersistIncomingAsync` do nothing but
`DbContext.Add(new OutgoingMessage/IncomingMessage(envelope))` [source]. They need a *later* `SaveChanges`.

| Mechanism | Envelope written by | Atomic |
|---|---|---|
| `outbox.PublishAsync(...)` then `outbox.SaveChangesAndFlushMessagesAsync(ct)` | that `SaveChanges` | **yes** [measured] |
| `AggregateRoot.Raise(...)`, scraped by `SaveChangesAndFlushMessagesAsync` **before** the save | that `SaveChanges` | **yes** [measured] |
| `AggregateRoot.Raise(...)` under the Eager `[Transactional]` middleware, scraped **after** the save | nobody | **no** — delivered in-process, never stored, lost forever on a crash [measured] |

`SaveChangesAndFlushMessagesAsync` is three steps, in this order [source]:

```csharp
foreach (var scraper in _scrapers) await scraper.ScrapeEvents(DbContext, this);  // domain events FIRST
await DbContext.SaveChangesAsync(token);                                          // one write
if (DbContext.Database.CurrentTransaction != null) await …CommitTransactionAsync(token);
await FlushOutgoingMessagesAsync();
```

Measured happy path, identical whether the handler is reached from HTTP or from a durable queue:

```
enter               tx=False
external call       pg=[idle:3(in_tx=0)]                              <- no transaction held
after publish       tx=False                                          <- publishing opens nothing (GH-3121)
ef:SavingChanges    tx=False tracked=[IncomingMessagex2,UserProfilex1]
TransactionStarted / TransactionCommitted / rows=3                    <- ONE implicit transaction
```

---

## 2. Configuration

```csharp
// src/Api.Bootstrapper/Program.cs
builder.Host.UseWolverine(opts =>
{
    if (builder.Environment.IsProduction())
    {
        opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
    }

    opts.PersistMessagesWithPostgresql(databaseConnectionString);
    opts.Durability.MessageStorageSchemaName = "wolverine";
    opts.Policies.UseDurableLocalQueues();

    opts.UseEntityFrameworkCoreTransactions();      // registers IDbContextOutbox<>
    // opts.Policies.AutoApplyTransactions();       // deliberately NOT enabled - see 2.1
    opts.PublishDomainEventsFromEntityFrameworkCore<IHasDomainEvents, IDomainEvent>(x => x.Events);

    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
    // ...discovery, AlwaysUseServiceLocationFor as today
});
```

Modules keep `services.AddDbContextWithWolverineIntegration<XDbContext>(...)`. Under this shape the
mapping is an advantage, not the liability it is under the Eager middleware: envelopes become tracked
entities flushed by the same `SaveChanges` as the business row — one round trip, one implicit
transaction — and on 6.x a publish no longer begins a transaction at all (GH-3121) [measured], so an
external call may sit anywhere before the save.

`WolverineFx.RuntimeCompilation` must be referenced. Wolverine 6 removed the Roslyn compiler from the
core package (GH-2876) and `TypeLoadMode.Dynamic` throws at startup without it:
`no IAssemblyGenerator (Roslyn) is registered`. The eventual production setup is `codegen write` in
CI + `TypeLoadMode.Static` + the package excluded from Release; until that lands the package ships.

There is no `DomainEventsClearingInterceptor` any more, and nothing needs one: the scrape runs before the
save, exactly once per `SaveChangesAndFlushMessagesAsync`. Under the Eager middleware such an interceptor
publishes **zero** domain events, because there the scrape runs after `SavedChanges` has already cleared
the collections.

### 2.1 Why `AutoApplyTransactions()` is off

It applies the Eager middleware to any chain with a `DbContext` dependency — and it sees the `DbContext`
**through** `IDbContextOutbox<T>`. Measured on 6.24.4 with the policy on and no `[NonTransactional]`:

```
business: "Writes"=3
incoming: Handled x6
deadletters: <trigger> x1  "Connection is not open"
```

Three full applications of the write, six delivered events, then a dead letter.
`SaveChangesAndFlushMessagesAsync` commits and releases the connection; the middleware's
`EfCoreEnvelopeTransaction.CommitAsync` then runs against it, throws, and Wolverine retries the whole
handler. No handler in this solution carries `[Transactional]` or `[NonTransactional]`, and neither has a
legitimate use here.

### 2.2 Why there is no inline retry continuation for commands

`Executor.InvokeInlineAsync` loops attempts on the **same** pipeline `MessageContext` and flushes only
after the loop; neither the generated `catch` nor `RollbackAsync` clears `_outstanding` [source]. A
rolled-back attempt's messages are then flushed by the successful one — a duplicate integration event
[measured]. Commands no longer go through the bus (§3.1), so this cannot reach them; the
`OnException<DbUpdateConcurrencyException>` policy still covers event handlers, whose durable-listener
retries never reuse the outstanding list [measured]. Practical consequence: a concurrency conflict on a
command now surfaces as 409 immediately instead of being retried three times.

---

## 3. Rules per artifact

### 3.1 Endpoint — calls the handler directly

Wolverine contributes nothing to a command chain under this shape: no middleware is applied, and
`IDbContextOutbox<T>` is an ordinary scoped DI service (it derives from `MessageContext` and pre-sets its
`Transaction` to an `EfCoreEnvelopeTransaction` over the same `DbContext`). It needs the Wolverine
*runtime*, not the Wolverine *pipeline*.

```csharp
internal sealed class CreateGameEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost(
            GameplayRoutes.CreateGame,
            async (
                CreateGameRequest request,
                CreateGameCommandHandler handler,
                IEnumerable<IValidator<CreateGameCommand>> validators,
                CancellationToken ct) =>
            {
                CreateGameCommand command = new(request.HostName);
                Result<CreateGameResponse> result =
                    await validators.HandleValidatedAsync(command, handler.Handle, ct);
                return result.Match(Results.Created(...), CustomResults.Problem);
            })
            .WithTags("Gameplay")
            .AllowAnonymous();
    }
}
```

- `internal sealed class <Feature>Endpoint : IEndpoint`, colocated with the command.
- Takes the handler, its validators and `CancellationToken` as delegate parameters.
- `Shared.Application.Validation.ValidationExtensions.HandleValidatedAsync` is the only way in. The
  handler is passed as a **method group**, so there is no way to run it without having validated first.
  The validator collection stays an explicit `IEnumerable<IValidator<TCommand>>` parameter rather than
  something resolved inside: the generic parameter ties it to the command type, so a copy-pasted mismatch
  is a compile error.
- Handlers never validate. Validation runs once, at the HTTP boundary.
- **No I/O of its own**: no `DbContext`, no HTTP calls, no `SaveChanges`.
- No `try`/`catch`. Infrastructure exceptions are mapped once in `GlobalExceptionHandler`.

### 3.2 Command handler

```csharp
internal sealed class CreateGameCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result<CreateGameResponse>> Handle(CreateGameCommand command, CancellationToken ct)
    {
        Result<Game> create = Game.Create(...);
        if (create.IsFailure)
        {
            return Result.Failure<CreateGameResponse>(create.Error);
        }

        Game game = create.Value;
        outbox.DbContext.Games.Add(game);

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return new CreateGameResponse(game.Id.Value, game.HostParticipantId.Value);
    }
}
```

Hard rules:

1. Dependency is `IDbContextOutbox<XDbContext>`, never the bare `XDbContext`. With a bare `DbContext` and
   no middleware nothing enlists the outbox: envelopes never reach the change tracker, the scrapers never
   run and no `SaveChanges` happens at all [measured].
2. `await outbox.SaveChangesAndFlushMessagesAsync(ct)` is the **last statement**. Anything after it is
   already committed and flushed.
3. External calls and reads go before it. No transaction is open until it runs [measured].
4. Never call `outbox.DbContext.SaveChangesAsync(...)`. A save before the outbox's own commits the
   business row on its own, outside the transaction the outbox will use: the happy path is
   indistinguishable from correct, and a crash leaves the row with no envelope [measured].
5. Do not inject `IMessageBus`/`IMessageContext` alongside `IDbContextOutbox<T>` — two different
   `MessageContext` instances; publishing through the wrong one bypasses your transaction.
6. `ExecuteUpdateAsync`/`ExecuteDeleteAsync` are not atomic with the save unless you opened a transaction
   yourself (`outbox.DbContext.Database.BeginTransactionAsync(ct)`; `SaveChangesAndFlushMessagesAsync`
   commits an existing transaction if there is one [source]).
7. Return `Result.Failure(...)` **before** the save — nothing is written and nothing published. Returning
   failure after the save is a bug: it is already committed.
8. Read → external call → conditional write needs an optimistic concurrency token
   (`UseXminAsConcurrencyToken()`), otherwise the write silently overwrites whatever changed while the
   call was in flight. With the token: `DbUpdateConcurrencyException` and **nothing** written — not the
   row, not the envelopes [measured].
9. `internal`, and take `CancellationToken ct`.

Crash windows [measured]:

| Kill point | Result |
|---|---|
| before the save | nothing written, **nothing recovered** — the caller never got a response, so retrying is the client's job |
| after `COMMIT`, before the outbox flush | business row + **all** envelopes committed; the restarted process delivers each event **exactly once** and duplicates nothing |

### 3.3 Event handler

Same body; different entry path, return type and visibility.

```csharp
public sealed class PlayerJoinedEventHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task Handle(PlayerJoinedIntegrationEvent message, CancellationToken ct)
    {
        GameplayDbContext context = outbox.DbContext;

        if (await context.PlayerProfiles.AnyAsync(p => p.PlayerId == message.PlayerId, ct))
        {
            return;                                     // idempotent: already applied
        }

        context.PlayerProfiles.Add(PlayerProfile.Create(message.PlayerId));
        await outbox.PublishAsync(new PlayerProfileCreatedIntegrationEvent(message.PlayerId));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
    }
}
```

On top of §3.2:

1. **Returns `Task`, never `Result`.** Codegen emits an unconditional `EnqueueCascadingAsync` for any
   handler that returns a value; for a published message that logs
   `No routes can be determined for Envelope #… (Result)` at **Information** — invisible at a production
   `Warning` threshold — and the failure is silently swallowed: the message is marked `Handled` and
   nothing retries. Signal failure by **throwing**: full rollback, retries, then a
   `wolverine_dead_letters` row carrying the exception message [measured].
2. **Public**, and so is every type in its signature. See §4.
3. **Idempotent.** Not advisory — see §5.
4. An external call in the same handler must be a safe repeat: a retry re-runs the whole handler
   including the call [measured `call#1`, `call#2`]. For a side-effecting call, derive an idempotency key
   from the message or aggregate id, or split into call-only handler → command → writer handler.

---

## 4. Visibility policy

Wolverine's discovery excludes non-public types
(`HandlerQuery.Excludes.WithCondition("Is not a public type", …)`, `_messageQuery.Excludes.IsNotPublic()`)
[source], and under `TypeLoadMode.Dynamic` the generated chains live in a separate assembly, so anything
they name must be public too.

Must be **public** — and only these:

- integration and domain event types;
- event handler types and their `Handle` methods;
- every type in an event handler's signature: the `DbContext` (hence `IDbContextOutbox<XDbContext>`'s type
  argument) and any injected service interface;
- service implementations behind `Abstractions` interfaces injected into an event handler.

Everything else is **internal**: commands, queries, command/query handlers, endpoints, validators, the
whole `Domain` model, EF configurations, design-time factories, the rest of `Infrastructure`.
`DbSet` properties stay internal.

Command and query handlers are registered in DI by `Shared.Application.ServiceCollectionExtensions`,
which scans the module assembly for `*CommandHandler`/`*QueryHandler`. Scanned rather than listed per
slice: a forgotten registration would only surface as a runtime resolution failure. Event handlers are
deliberately **not** registered there — the Wolverine pipeline instantiates them from generated code.

---

## 5. Idempotency is mandatory

`SaveChangesAndFlushMessagesAsync` never marks the incoming envelope handled — only
`EfCoreEnvelopeTransaction.CommitAsync` does, and this shape does not call it [source]. The ack is done by
`DurableReceiver.CompleteAsync` → `MarkIncomingEnvelopeAsHandledAsync` after the handler returns, on a
separate connection.

Measured, SIGKILL after `COMMIT` in an event handler: business row and both envelopes committed, trigger
envelope still `Incoming`, recovered on restart, handler runs a second time —
**`Writes=2`, every event delivered twice.**

Ownership does not save you. `Envelope.MarkReceived` stamps `OwnerId = settings.AssignedNodeNumber` on the
inbox row and recovery only picks up `owner_id = 0` (`ReleaseOrphanedMessagesOperation` resets it when a
node leaves the `nodes` table) [source], so two nodes never process the same envelope concurrently. But a
redelivered envelope is the *same row*, so envelope-id dedup cannot help either. **Ownership prevents
concurrent processing; only idempotency prevents repeated processing.**

Pick one, and put it **in the same transaction as the write**:

- a state check on the aggregate (`if (game.Status is GameStatus.Started) return;`);
- an upsert on a natural key — unique index plus `ON CONFLICT`;
- an optimistic concurrency token (`UseXminAsConcurrencyToken()`).

Never a separate "already processed" table on another connection: a crash between the guard and the
`COMMIT` makes the retry skip as a duplicate and the write is lost forever [measured].

Wolverine's `[Idempotent]` / `AutoApplyIdempotencyOnNonTransactionalHandlers()` does **not** help:
`TryMakeEagerIdempotencyCheckAsync` starts with `if (envelope.WasPersistedInInbox) return true;`, and
behind a durable listener that is always true [source]. Those APIs only cover Inline/Buffered endpoints.

---

## 6. Definitively does not work

### 6.1 The Eager `[Transactional]` middleware with a mapped `DbContext`

Generated code is `SaveChangesAsync()` then `efCoreEnvelopeTransaction.CommitAsync()`. `CommitAsync`
scrapes domain events **first thing**, and on a mapped `DbContext` the scrape only `DbContext.Add(...)`s
the envelopes — then it commits, with no second save [source, and visible in the generated chain].

Measured on 6.24.4, kill after `COMMIT`: business row committed, trigger `Handled`, the explicitly
published integration event recovered and delivered — and the aggregate-raised domain event **never
delivered, ever**: not in `wolverine_outgoing_envelopes`, not in `wolverine_incoming_envelopes`, not
redelivered.

Upstream [#3744](https://github.com/JasperFx/wolverine/issues/3744); PR #3754 (merged 2026-08-01) added the
missing `SaveChangesAsync` after the scrape but only in `CommitTenantedDbContextTransaction`, the managed
multi-tenancy frame. The single-`DbContext` path never got it. Still broken on `main`.

Separately, `EnrollDbContextInTransaction` is inserted with `chain.Middleware.Insert(0, …)` [source], so
the transaction is open before the handler body and before every `Load`/`Before` method — any external
call in a `[Transactional]` handler runs as `idle in transaction` [measured].

The whole 2×2, measured: the domain event is lost **iff** (Eager middleware) **and** (mapped `DbContext`).
Every other combination persists it.

### 6.2 `TransactionMiddlewareMode.Lightweight` on a non-HTTP chain

No `EfCoreEnvelopeTransaction` is ever created, so there is no EF outbox. (On 6.x a Wolverine.Http chain
does get one via the `EnlistDbContextInOutbox` frame, GH-3291 — this solution uses Minimal API, so that
never applies.)

### 6.3 A handler named `…Handlers`, or an event handler left `internal`

Not discovered. `PublishAsync` succeeds, no envelope is written, nothing is logged above Information, and
`codegen test` stays green because a missing chain is not a compile error. Discovery got **stricter** in
6.x: a plural class name that was discovered on 5.39.1 is silently dropped on 6.24.4 [measured].

Nothing in this solution guards against it automatically. The check that would —
`((WolverineRuntime)sp.GetRequiredService<IWolverineRuntime>()).Handlers.CanHandle(type)` over every
event type, run from an `IHostedService` registered after `UseWolverine` — was written and deliberately
dropped as not worth the moving part while there is one event handler. Reach for it when there are more.
Note it must run after Wolverine's own hosted service: the handler graph does not exist earlier, so
asserting straight after `builder.Build()` reports every event type as an orphan.

### 6.4 A background job that does the work itself

A job is not a message: no inbox, nothing to redeliver. A kill mid-transaction loses the work completely
and silently [measured]. The job body is one line — `await bus.PublishAsync(new ReconcileCommand(id))` —
and the work lives in a §3.3 handler.

### 6.5 `Result.Failure` returned from an event handler

Silently swallowed: marked `Handled`, nothing retries, nothing recorded. Throw instead.

---

## 7. Enforcement

Four conventions carry the shape and **none of them is currently enforced by anything but review**:

- no handler takes a bare `DbContext`;
- no handler takes both `IDbContextOutbox<>` and a message bus;
- no `[Transactional]` / `[NonTransactional]` anywhere;
- every `*EventHandler.Handle` returns `Task`.

Each is a one-`Fact` reflection check over `ModuleAssemblies` if that ever stops being enough.

`ModifierTests` asserts commands/queries and their handlers **internal** (they were asserted public when
Wolverine was the mediator). That modifier is not cosmetic: `HandlerQuery.Includes.WithNameSuffix("Handler")`
does match `CreateGameCommandHandler` by name, and the only reason Wolverine skips it is
`HandlerQuery.Excludes.WithCondition("Is not a public type", …)`. Verified by generating the chains —
`GeneratedHandlerRegistry.HandlerTypes()` returns exactly one type, the integration-event handler.
Make a command handler public and Wolverine starts generating a chain for it.

**`codegen test`** — load-bearing now that a mis-named or non-public handler is skipped silently:

```bash
ASPNETCORE_ENVIRONMENT=Production ConnectionStrings__PokerManager-db='Host=localhost;Port=1;Database=x;Username=x;Password=x' \
  dotnet run --project src/Api.Bootstrapper --no-launch-profile -- codegen test
```

Currently green; not yet wired into CI (`docs/architecture-review.md` #18).

---

## 8. Known trade-offs and gaps

1. **The inbox ack is not atomic with the business write.** Deliberate: it is what buys one handler shape
   instead of two, and it is paid for by §5. A crash between `COMMIT` and the ack redelivers the message
   and re-runs the handler [measured].
2. **Wolverine is not our mediator.** Command handlers get no retry policies, no Wolverine telemetry and
   no middleware. In exchange they are `internal`, compiler-checked, and immune to §2.2.
3. **`Register` is reasoned, not measured.** ASP.NET Core Identity owns its own persistence:
   `UserManager.CreateAsync` calls `SaveChangesAsync` on the same scoped `IdentityDbContext`.
   `RegisterCommandHandler` therefore opens the transaction *before* calling it, so that save joins ours
   instead of committing on its own, and the single commit happens in
   `SaveChangesAndFlushMessagesAsync` together with the `UserRegisteredIntegrationEvent` envelope. This
   follows from EF Core semantics (`SaveChangesAsync` uses `Database.CurrentTransaction` when one exists
   and does not commit it) but was **not crash-tested** the way the Gameplay shape was. Re-verify when
   Identity is refactored to decouple its domain from ASP.NET Core Identity.
4. **Three application-level tests are still missing.** Write each one as soon as its subject exists —
   mark them "once per application", not per slice:
   * a domain event raised on an aggregate reaches its handler. **The subject now exists** (2026-08-18):
     six `Raise(...)` sites across `Game` and `Hand`, all consumed by
     `Gameplay.Features.SubscribeGameStateChanges.GameUpdatePushEventHandler`, which pushes them over
     SignalR. So §6.1 now guards live code and nothing but review enforces it — this is the one test in
     this list whose subject is already here.
   * a handler makes its external call with no transaction open (assert on
     `DbContext.Database.CurrentTransaction`). **No handler makes an external call yet.**
   * a unique-violation (`23505`) maps to 409. Identity's duplicate-email path returns 409 through
     `IdentityErrors`, not through `DbUpdateException`, so the central mapping is still untested.
5. **Production codegen.** `TypeLoadMode.Static` is set for Production but nothing pre-generates the
   chains, so `WolverineFx.RuntimeCompilation` ships. Wire `codegen write` into CI, commit
   `Internal/Generated`, then drop the package from Release builds.

---

## 9. Migration summary (what changed, 2026-08-03)

| Before | After |
|---|---|
| `WolverineFx* 5.39.1` | `6.24.4` + `WolverineFx.RuntimeCompilation` |
| `opts.Policies.AutoApplyTransactions()` | removed (§2.1) |
| `AddDomainEventsClearing()` on both `DbContext`s | removed, interceptor deleted |
| endpoint → `bus.InvokeValidatedAsync(...)` | endpoint → `validators.HandleValidatedAsync(command, handler.Handle, ct)` |
| `Shared.Infrastructure.Messaging.*` | `Shared.Application.Validation.ValidationExtensions` |
| handler takes `GameplayDbContext`, no save | handler takes `IDbContextOutbox<GameplayDbContext>`, saves last |
| `RegisterCommandHandler` returns `(Result, event?)` cascade | publishes explicitly through the outbox, returns `Result` |
| commands + handlers `public` | `internal` — and that is what keeps them out of Wolverine's discovery |
| `BannedSymbols.txt` + `Microsoft.CodeAnalysis.BannedApiAnalyzers` | removed; the conventions live in this document and in review |
| `RegisterCommandHandlerTests` (unit) | `RegisterIntegrationTests` — both branches need Identity's own persistence |

`dotnet test --filter "FullyQualifiedName!~E2E"`: 68 passed, 0 failed.
`dotnet format --verify-no-changes`: clean. `codegen test`: `Success!`.
