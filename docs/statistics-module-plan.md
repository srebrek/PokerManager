# Statistics module — plan

Design decisions for the `Statistics` module, agreed 2026-08-26 and implemented the same day. Written in
English to match the other docs in this folder. Sections amended during implementation say so inline —
§3.1, the `hand_action` table, §4.1 and §6 are the ones that changed.

Read `docs/wolverine-transactions-and-events.md` first — every rule in §"Ingest handlers" comes from it.

## 1. Goal

MVP: after **Finish hand** the host lands on a hand summary; after **Finish game** everyone lands on a
game summary. Everyone sees game statistics and their own.

MVP statistics:

- **chips timeline** — one line per participant across hands (the chart from `docs/requirements.md`);
- **showdown rate** — share of hands that reached a showdown;
- per-hand: pots, winners, net per participant, duration, action count, showdown or not.

Deliberately out of MVP but collected from day one: action timestamps, pots and declared winners, rebuy
amounts. Cards come later.

## 2. Scope split — the load-bearing decision

**Gameplay stores nothing new.** No timestamp columns, no rebuy log, no `UserId`. Every fact Statistics
needs is a fact Gameplay already owns at the moment it happens, so it travels in the **event payload**
and is stored by Statistics.

The distinction: *publishing a fact you own is the module's public language; storing data you never read
is coupling.* An earlier draft of this plan put timestamp columns and a rebuy table in Gameplay — that
was wrong and is rejected here.

Consequence to accept: Gameplay cannot backfill. A fact that never reaches Statistics is gone. The
outbox guarantees delivery once published (envelope committed atomically with the business row), so the
real exposure is only games played before this module ships. Publishing the events earlier would not
help — an event with no handler is never persisted at all (`No routes can be determined`, logged at
Information).

## 3. Integration events

New, in `src/Contracts/IntegrationEvents/Gameplay/`. Six events, all about facts Gameplay already holds.

| event | payload | published from |
|---|---|---|
| `HandStartedIntegrationEvent` | `handId`, `gameId`, seats `(participantId, name, position, startingChips)`, the two blind actions in the same shape as regular actions (sequences 0 and 1) | `StartHandCommandHandler` |
| `HandActionRecordedIntegrationEvent` | `handId`, `sequenceNumber`, `participantId`, `type`, `amountTo` | `RecordActionCommandHandler` |
| `HandActionUndoneIntegrationEvent` | `handId`, `sequenceNumber` of the removed action | `UndoLastActionCommandHandler` |
| `HandFinishedIntegrationEvent` | `handId`, pots `(index, amount)`, pot winners `(potIndex, participantId)`, results for **every** game participant `(participantId, name, net, endingChips)` | `FinishHandCommandHandler` |
| `HandAbortedIntegrationEvent` | `handId` | `AbortHandCommandHandler` |
| `RebuyRecordedIntegrationEvent` | `eventId`, `gameId`, `participantId`, `amount` | `RebuyCommandHandler` |

Published with `outbox.PublishAsync(...)` from the command handler that already has the aggregate loaded,
before `SaveChangesAndFlushMessagesAsync` — atomic with the business write. Not from a domain-event
handler: that would have to reload the aggregate.

`HandStarted` carries the blind actions because `Hand.Start` puts `PostSmallBlind`/`PostBigBlind` straight
into `_actions` without raising a per-action domain event. This keeps the action log uniform and means
blinds are never a column anywhere.

`HandFinished` carries results for every participant, not only the seated ones (sitting-out players get
`net = 0`). That makes the chips timeline a direct read instead of a reconstruction from
`startingChips + rebuys + nets`.

### 3.1 Event metadata — amended during implementation

`IIntegrationEvent` was a bare marker. It now carries **both** `DateTimeOffset OccurredAt` and
**`Guid EventId`**, minted with `Guid.NewGuid()` in the publishing command handler and stamped
from the already-registered `TimeProvider` singleton.

The original draft kept `EventId` off the interface, reasoning that only `RebuyRecordedIntegrationEvent`
lacked a natural key, and left the door open: *"Promote it to the interface when a second event needs
one."* That door opened immediately, because **`(handId, sequenceNumber)` is not a natural key at all.**
`Hand.UndoLastAction` does `_actions.RemoveAt(...)`, so the sequence number returns to the pool and the
*next* action reuses it. The undone `Raise` and the `Call` that replaces it are two distinct facts, each
delivered exactly once, sharing that key — and the §5 rule ("check the PK, `return` if the row is
already there") would have silently dropped the real action and left the undone one looking live.

Note what this is *not*. Wolverine's inbox is a durability mechanism, not a dedup mechanism (§5): it
guarantees delivery, and redelivery re-runs the handler. But redelivery was never the problem here —
two different facts colliding on a non-unique key is a modelling error, and no inbox can split them.

Identity therefore has to arrive in the message, and Gameplay cannot supply one from its own state
(§2 forbids storing timestamps or ids). Two candidates:

- **`Envelope.Id`** — public on `Wolverine.Envelope`, stable across redelivery of the same envelope, and
  free. Rejected: it is infrastructure identity, and §3.2 already rejected exactly that dependency for
  timestamps. *"Arrival stamps are rewritten by any re-projection or re-ingest; payload stamps survive"*
  applies verbatim to envelope ids.
- **`Guid EventId` in the payload** — payload-owned, survives re-ingest, committed atomically with the
  envelope so redelivery carries the same value. Chosen.

A hand-rolled alternative — widening the primary key with `occurred_at` instead — was also rejected: it
is an event id made out of a timestamp, wider than a real one and conflating *when* with *which*.

### 3.2 Why the timestamp is in the payload, not stamped on arrival

Stamping at ingest time was considered and rejected. Ordering is not the reason — `sequenceNumber` gives
logical order independently of any clock. The reasons are:

1. **Recovery and retry bias.** A recovered envelope is handled minutes later; a whole hand's actions get
   near-identical stamps that look exactly like a legitimately fast hand. Systematic, undetectable, and
   not the random noise a statistical filter can remove.
2. **Replay.** The point of the raw layer is adding new statistics over historical data later.
   Arrival stamps are rewritten by any re-projection or re-ingest; payload stamps survive.

Optional later: Statistics may also store its own `received_at` per row. Difference from `occurred_at`
is ingest lag and reveals the recovery bursts. Not MVP.

## 4. Storage

Schema `statistics`, own `StatisticsDbContext`, own migrations, `UseSnakeCaseNamingConvention`, own
`MigrationsHistoryTable`. Identifiers are plain `Guid` — they belong to Gameplay; Statistics has no
identity of its own to wrap in a strongly-typed id.

Enums are stored as `text`, not `int`: an analytics store outlives a reordering of a C# enum, and every
ad-hoc SQL query against it stays readable.

Foreign keys **are** used within the schema. Seven tables.

### `hand`

| column | type | source |
|---|---|---|
| `id` | uuid **PK** | Gameplay `HandId` |
| `game_id` | uuid | `HandStarted` |
| `started_at` | timestamptz | `HandStarted.OccurredAt` |
| `finished_at` | timestamptz null | `HandFinished.OccurredAt` |
| `aborted_at` | timestamptz null | `HandAborted.OccurredAt` |

No `status` column — derivable from the two nullable timestamps, which also record *when*.
Index: `(game_id, started_at)`; every game-level query orders by it.

### `hand_seat` — who played the hand

| column | type | notes |
|---|---|---|
| `hand_id` | uuid **PK** | FK → `hand.id` |
| `participant_id` | uuid **PK** | |
| `position` | int | `0` = small blind, per `Game.BuildSeats` |
| `starting_chips` | int | stack when cards were dealt |

Basis for positional statistics. No name here — the name lives on `hand_result`, which is a superset.

### `hand_action` — append-only action log

Amended during implementation: the primary key is the event id, not `(hand_id, sequence_number)` — see
§3.1 for why that key does not hold.

| column | type | notes |
|---|---|---|
| `event_id` | uuid **PK** | from the payload; the idempotency key |
| `sequence_number` | int **PK** | second key column only because `HandStarted` carries two blinds under one `event_id` |
| `hand_id` | uuid | FK → `hand.id`, indexed with `sequence_number`; **not** unique |
| `participant_id` | uuid | |
| `type` | text | `PostSmallBlind`, `PostBigBlind`, `Fold`, `Check`, `Call`, `Bet`, `Raise`, `AllIn` |
| `amount_to` | int null | only `Bet`/`Raise`/blinds |
| `occurred_at` | timestamptz | |
| `undone_at` | timestamptz null | undo sets this and **never deletes the row** |

Sequences 0 and 1 arrive with `HandStarted`, the rest with `HandActionRecorded`. Blind sizes, showdown
rate, aggression and decision times are all derived from this table, filtered to `undone_at IS NULL`.
Keeping undone rows preserves faithful history — including a re-recorded action sitting next to the one it
replaced, under the same sequence number — and, as a bonus, measures how often the host misclicks.

### `hand_result` — state of **every** participant after the hand

| column | type | notes |
|---|---|---|
| `hand_id` | uuid **PK** | FK → `hand.id` |
| `participant_id` | uuid **PK** | |
| `participant_name` | text | snapshot |
| `net` | int | `HandAward.Net`; sums to 0 across seated players |
| `ending_chips` | int | stack after `ApplyHandAwards` |

One row per game participant, including those sitting out. The only table covering everyone, hence the
name lives here.

### `hand_pot`

| column | type | notes |
|---|---|---|
| `hand_id` | uuid **PK** | FK → `hand.id` |
| `pot_index` | int **PK** | |
| `amount` | int | |

### `hand_pot_winner`

| column | type | notes |
|---|---|---|
| `hand_id` | uuid **PK** | FK → `hand_pot` |
| `pot_index` | int **PK** | FK → `hand_pot` |
| `participant_id` | uuid **PK** | |

These two are not needed by MVP. They are in because they are **unrecoverable**: the winner is declared by
the host, and pot amounts would mean reimplementing `PotCalculator` in a second module. Both are raw facts,
and the principle is to capture raw facts now and project later.

### `rebuy`

| column | type | notes |
|---|---|---|
| `event_id` | uuid **PK** | from the event; the only fact with no natural key |
| `game_id` | uuid | indexed |
| `participant_id` | uuid | |
| `amount` | int | may be negative — `Participant.Rebuy` allows cash-out |
| `occurred_at` | timestamptz | |

Gives the break-even line on the chips chart.

### Later: cards

`hand_board(hand_id, street, cards)` and `hand_hole_cards(hand_id, participant_id, cards)`. Written by
Statistics' own commands, not from events — the board and hole cards influence no gameplay rule, so they
never belong in Gameplay's domain.

### No `game` table, no `participant` table

Both were dropped: everything is derived from the rows carrying a `game_id`. The `game` row would have held
only `finished_at`, which is not even the best measure of game length (`last hand finished_at − first hand
started_at` excludes the idle time before the host clicks). Participants come from `hand_result`.

Consequences, accepted:

- **`GameFinishedIntegrationEvent` does not exist.** It would have no handler, and an unrouted event is
  never persisted.
- Statistics does not know whether a game is over. It does not need to — `GameStateResponse.IsFinished`
  already tells the frontend. A summary requested mid-game returns current standings, which is not wrong.
- A participant who only ever sat out, rebought and never played a hand has no name anywhere. They should
  not be on the chart either.

### 4.1 Aggregate: `Hand` is the root — amended after implementation

The first cut had seven flat row types, each with its own `DbSet`. `Hand` is now the aggregate root:
`hand_seat`, `hand_action`, `hand_result` and `hand_pot` are `OwnsMany` collections on it, and
`hand_pot_winner` is nested under `hand_pot` (which is what preserves its foreign key to `hand_pot`
rather than to `hand`). Only `Hand` and `Rebuy` keep a `DbSet`. `Rebuy` stays outside the aggregate: it
is game-scoped, has no `hand_id`, and is its own root.

The reason is visible in the handlers that were replaced. Every one of them opened with a
`Hands.AnyAsync` probe and then ran a per-table dedup query — a load of the root done piecemeal, with the
hand's invariants spread across six handlers. §5's rule ("check the primary key, `return` if the row is
already there") now lives inside `Hand.RecordAction`, `Hand.TryUndoAction`, `Hand.Finish` and
`Hand.Abort`; a handler loads the root and calls one method. `HandActionUndone` no longer needs its
second "already applied" query at all — the actions are in memory.

Not `AggregateRoot<TId>`: `Shared.Domain.Entity<TId>` constrains `TId : IStronglyTypedId<TId>` and §4
keeps identifiers as plain `Guid`. Statistics also raises no domain events, so the base type would carry
nothing. The pattern is copied from Gameplay's `Hand` — private constructor, private lists behind
`IReadOnlyList` navigation properties, a static factory — without the base class.

`HandAction` is the only owned child with its own identity (`event_id`) *and* mutable state
(`undone_at`), so it stays a class under `Domain/Entities` with a `MarkUndone` method. The immutable
children are records under `Domain/ValueObjects`.

Three consequences, all measured:

- **Owned collections are always eager-loaded.** On EF Core 10.0.7 / Npgsql 10.0.1 the navigations report
  `IsEagerLoaded = true`, the SQL for a bare `db.Hands` carries all four `LEFT JOIN`s with no `Include`,
  and `IgnoreAutoIncludes()` produces byte-identical SQL. So any query that *materialises* a `Hand` pulls
  the whole hand graph. The read side therefore projects with `Select` instead of materialising — a
  projection fetches only the columns and child rows it names, which is what §6 wanted anyway.
- **Every non-FK key part of an owned collection needs `ValueGeneratedNever()`.** Without it EF gives
  `hand_pot.pot_index` a Postgres identity default, and treats a freshly added `hand_result` — whose key
  part `participant_id` is a `Guid` that already carries a value — as an existing row, emitting
  `UPDATE hand_result SET …` that affects 0 rows and throws `DbUpdateConcurrencyException`.
- **The schema is unchanged bar one constraint rename**, `fk_hand_action_hands_hand_id` →
  `fk_hand_action_hand_hand_id` (migration `RenameHandActionForeignKey`). The plural name was already the
  odd one out: the other five foreign keys in `InitialStatistics` use the singular table name.

There is deliberately no concurrency token on `hand`. A rowversion would not fire for an ingest that only
inserts child rows — EF emits no `UPDATE` of the root, so no token is checked. Concurrent delivery of two
events for the same hand is guarded by the primary key plus Wolverine's retry, which is enough: two
different actions carry different `event_id`s, and a redelivered one collides on the key and retries into
the dedup check.

### Which event writes what

| event | tables |
|---|---|
| `HandStarted` | `hand`, `hand_seat`, `hand_action` (blinds) |
| `HandActionRecorded` | `hand_action` |
| `HandActionUndone` | `hand_action.undone_at` |
| `HandFinished` | `hand.finished_at`, `hand_result`, `hand_pot`, `hand_pot_winner` |
| `HandAborted` | `hand.aborted_at` |
| `RebuyRecorded` | `rebuy` |

## 5. Ingest handlers

All rules from `docs/wolverine-transactions-and-events.md` §3.3, no exceptions:

- one handler per event, `public sealed`, own file, own feature folder — do not repeat Gameplay's
  multi-`Handle` class that carries `// TODO: move to the separate files`;
- `Handle` returns **`Task`**, never `Result` — a `Result` from an event handler is silently swallowed,
  marked `Handled`, never retried (§6.5);
- dependency is `IDbContextOutbox<StatisticsDbContext>`; `await outbox.SaveChangesAndFlushMessagesAsync(ct)`
  is the last statement; never `outbox.DbContext.SaveChangesAsync`;
- **idempotent** — a redelivered envelope re-runs the handler (§5). Every table's primary key is the
  idempotency key: check, and `return` before writing if the row is already there.

`HandActionUndone` is the one handler where that check is not a plain primary-key lookup, because the undo
event has no row of its own. It marks

```sql
UPDATE hand_action SET undone_at = :occurred_at
WHERE hand_id = :hand_id AND sequence_number = :sequence_number
  AND undone_at IS NULL AND occurred_at < :occurred_at
```

The `occurred_at <` predicate is what makes it idempotent: a redelivered undo arriving after the sequence
number was reused finds the replacement action stamped *later* than itself and leaves it alone. If nothing
matches and no row already carries `undone_at = :occurred_at`, the action has not been ingested yet and the
handler throws (§5.1).

### 5.1 Missing parent: throw, do not upsert

If the `hand` row is missing, the handler throws a dedicated exception type and Wolverine retries.

Rejected alternative: upsert-on-first-sight (every handler creates a shell `hand` row). It would need
`gameId` on every hand-scoped event, adds a branch to every handler, and — decisively — leaves **silently
incomplete data**: a `hand` row with no `started_at` and no seats if `HandStarted` never arrives. A dead
letter is visible; a half-filled row is not.

Out-of-order delivery is rare by construction: `HandStarted` and `HandActionRecorded` are published from
**different HTTP requests**, seconds apart at human pace, so the parent's envelope is committed and
flushed long before the child's request arrives. There is no batch of events inside one transaction.
Reordering therefore requires a post-restart backlog and nothing else.

A dedicated exception type rather than relying on the FK violation: a `23503` arrives as
`DbUpdateException`, and a retry policy on that type would also catch unique violations — which mean
"duplicate, already handled" and should succeed, not retry.

Accepted knock-on effect: if `HandStarted` dead-letters permanently, every action of that hand dead-letters
too. Correct — a hand with no seats is useless — and visible.

### 5.2 Retry policy

Start with Wolverine's default. Revisit only if dead letters actually appear.

If it needs tuning, note the cost asymmetry: a lost retry means permanently lost statistics, while waiting
costs nothing because this is background ingest that nobody waits on. Something like
100 ms / 500 ms / 2 s / 5 s on the dedicated exception type, as a separate policy from the existing
`OnException<DbUpdateConcurrencyException>` one. Seconds-long cooldowns are not needed: after a restart the
parent's envelope is *already in the queue*, so the wait is for one message, not for the backlog to clear.

### 5.3 Observability

`GlobalExceptionHandler` is an `IExceptionHandler` on the HTTP pipeline — an event handler never reaches it.
Three paths already exist with no new code:

- **logs** — Wolverine logs a failed envelope through `ILogger`, with the exception;
- **traces** — `TelemetryExtensions` already has `AddSource("Wolverine")`, so the per-message span reaches
  the Aspire dashboard. Note `InfrastructureSqlFilterProcessor` drops activities whose display name contains
  `wolverine_dead_letters`: that filters the SQL writing the dead letter, not the handler span;
- **metrics** — `AddMeter("Wolverine*")` is wired, so failure and dead-letter counters are already there.

`wolverine_dead_letters` is the authoritative record of what was lost, carrying the exception message.

What is missing is only someone looking. The dashboard metric is enough for now; add a health check counting
dead letters when this runs unattended.

One nuance for the missing-parent exception: a retry there is *expected behaviour*, not a failure. Logging
every attempt at Error trains you to ignore the channel. If that becomes noisy, the lever is Wolverine's
message-failure log level — check the exact API against 6.24.4 before using it.

## 6. Read side

**Compute on read. No materialized statistics tables.**

Query handlers aggregate the raw tables on every GET. At tens of hands per game the aggregation is trivial,
and this buys: no second write path, no staleness, no readiness state, and a new statistic works on the
whole history immediately with no backfill job. Materialize only when a specific query actually gets slow.

The counting itself lives in `Domain/Services/` as pure calculators over rows — unit-testable, mirroring
Gameplay's `HandStateCalculator` and `PotCalculator`. Queries never go through the aggregate: they project
straight off `db.Hands` with `Select`, both because the summaries read across many hands and because
materialising a `Hand` drags its whole graph (§4.1).

Amended: the original wording here said Statistics entities are flat records with no invariants and that
`AggregateRoot` should not be forced onto them. The first half was wrong — idempotent ingest *is* an
invariant of the hand, which is why §4.1 makes `Hand` a root. The second half stands for a different
reason: the base class needs a strongly-typed id and there are no domain events to raise. `Result` is
still absent by design — an event handler that returns one is silently marked handled and never retried
(§5).

### 6.1 Endpoints

Two, with route constants in `Contracts/Api/Statistics/` mirroring `GameplayRoutes`, and response DTOs
alongside:

- hand summary — 404 while `hand.finished_at` is null;
- game summary — returns what it has, plus the number of hands it actually sees.

### 6.2 No SignalR, no polling, no readiness machinery

The ingest race is real but tiny, and a page refresh is a sufficient remedy. `Game.Finish()` requires
`CurrentHandId is null`, so the last hand's transaction is already committed before **Finish game** is even
allowed, and the host then has to click — hundreds of milliseconds of human time against a queue that
drains in milliseconds. Hitting the window requires a stalled queue.

Therefore, all rejected:

- `finishedHandCount` in an event and an `expected_hand_count` column, to prove completeness;
- a `StatisticsHub` — Statistics cannot use Gameplay's `GameHub`, so this meant a second SignalR
  connection from the frontend;
- polling.

The frontend retries once after a short delay if the hand summary 404s — that covers the one tight case,
where the host clicks **Finish hand** and immediately wants the summary. Reporting the hand count on the
game summary gives the user something to notice a gap by.

## 7. Module wiring checklist

1. `src/Modules/Statistics`, referencing `Contracts` and `Shared` only — never another module.
2. `StatisticsModule.AddStatisticsModule()` / `ApplyStatisticsMigrationsAsync()`, modelled on
   `GameplayModule`: `AddDbContextWithWolverineIntegration<StatisticsDbContext>`, schema `statistics`,
   `UseSnakeCaseNamingConvention()`, own `MigrationsHistoryTable`.
3. In `Program.cs`: `AddStatisticsModule(...)` **and**
   `opts.Discovery.IncludeAssembly(typeof(StatisticsModule).Assembly)`. Without the second line handlers are
   not discovered, `PublishAsync` still succeeds, no envelope is written, and nothing is logged above
   Information (§6.3).
4. Register the assembly in `BaseArchitectureTest.ModuleAssemblies` — otherwise the module is invisible to
   every architecture rule.
5. Visibility: public = event types, event handler types and their `Handle` methods, `StatisticsDbContext`
   (with `internal DbSet`s). Everything else internal — with one forced exception:
   `IngestParentMissingException` is public because `CA1064` and Sonar `S3871` reject a non-public
   exception type. It lives in the module root (namespace `Statistics`), where no architecture rule
   demands `internal`, and it is the type §5.2's retry policy would name from `Api.Bootstrapper`.
6. Migration: `dotnet ef migrations add InitialStatistics --project src/Modules/Statistics --output-dir Infrastructure/Data/Migrations`.
7. After adding handlers, run `codegen test` in the Production configuration. **It does not catch a
   misnamed or non-public handler** — §6.3 of the Wolverine doc is right that a missing chain is not a
   compile error, and this was confirmed here. Use `codegen preview` and grep for each handler type name;
   that is what actually proves discovery found all six.

## 8. Order of work

1. `OccurredAt` **and `EventId`** on `IIntegrationEvent` (§3.1). — done
2. The six events in `Contracts`, published from the Gameplay command handlers. — done
3. `Statistics` module skeleton + schema + ingest handlers. — done
4. Hand summary endpoint and page. — endpoint done, page outstanding
5. Game summary endpoint, chips timeline chart, showdown rate. — endpoint done, chart outstanding
6. Cards: `hand_board`, `hand_hole_cards` and the commands that fill them. — outstanding

The backend of 1-5 landed together. `GameSummaryResponse` carries `ChipsTimeline` as `IReadOnlyList<int?>`
per participant, aligned by index to `Hands`: a `null` is a hand that participant has no result for, so a
latecomer's line simply starts later. `HandSummaryResponse.ActionCount` counts live actions **excluding**
the two blinds — the blinds are always present, so counting them would add a constant to every hand.

Table names follow the plan's singular form (`hand`, `hand_seat`, …), which differs from Gameplay's plural
(`hands`, `hand_seats`, …). Deliberate: this is a separate schema meant to be queried by hand.

## 9. Explicitly deferred

- **`Participant.UserId`.** Not needed now: there is no authentication, and the summary is shown at the end
  of the game where every participant is known. When it lands it will be a Gameplay change for Gameplay's
  own reasons — today all endpoints are `AllowAnonymous()` and `ActingParticipantId` comes from the request
  body, so host authorization (`GameErrors.NotHost`) trusts a value the client picks freely.
- Cross-game and per-user statistics — blocked on the above.
- Materialized statistics tables (§6).
- `received_at` alongside `occurred_at` (§3.2).
- A health check over `wolverine_dead_letters` (§5.3).
