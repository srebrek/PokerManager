# Progressive MVP plan (the further the step, the less detailed)

Goal: Join -> Seat -> Play Simple Poker Version -> Finish

Delayed Goals (outside MVP but will be in the future, so plan not to block them):
- rebuy (MVP hardcodes the initial participant stack)
- change seat order (in the MVP the joining order is the seating order)
- rotating dealer button (in the MVP the host is always the dealer)
- undo action
- settling up
- side pots (MVP has only one winner)
- chip values (e.g. red is 5)
- showing cards
- editing blinds
- statistics module
- authentication
- SignalR
- last action sent with the refresh-button snapshot
- participant-per-game limit
- rejoining a game
- join refactor (only JoinCode required)
- 2 player mode

The plan does not cover those goals, but it does prepare for them.

## Domain Overview

There are two aggregates in the Gameplay module: Game and Hand

### Game

#### Logic

Game is like a poker room. It holds the participants (child entity) and a reference to the current hand (can be null). Users join via a code (easier to enter than a guid), then they can choose an existing participant or create a new one. They can then choose a seat (a cyclic order where the last participant is next to the first one). A participant can freely change their seat or become unseated, in which case they do not take part in the next hand. The host can edit the game properties, e.g. SB, BB (the BB does not have to be double the SB) and the dealer button placement. The host can start a hand: the seated participants' ids, their chips, the SB and BB values and the dealer placement are sent to the hand. The game then holds a reference to that hand. When the hand finishes, it returns the winners and their awards (a list of all participants and their net award). The game adjusts the participants' stacks, and then the next hand can start, or the game can finish and be settled up. A finished game cannot be undone. Participants cannot leave the game (they may remain unseated). Participants do not have to be logged in to play. Logging in enables statistics. The host can act on behalf of themselves and of the other participants. The host can rebuy for other participants. A user that does not have a participant is an observer.

#### Technical concerns

- game state is stored as a nullable HandId and a bool IsFinished
- seats are stored as a list in the Game
- minimum 2 participants to start a hand

### Hand

#### Logic

Hand is a single poker hand. It holds the participant ids, their order, their stacks, SB and BB. It also holds an action log that will be used later by the statistics module (outside scope). An action is a raise, fold, all-in, call or check, with a participant id and an amount (the amount is attached only to a raise, the rest is calculated by the domain). The hand can then calculate when the streets and the hand finish. Participants cannot act out of order. Players may enter their own cards and the host can enter the community cards before the hand finishes. The hand returns a list of participants and their awards. Hand can be aborted.

#### Technical concerns

- the hand receives an ordered list of the participants' ids and the first one is the dealer
- for the MVP all actions that may lead to side pots are forbidden
- for the MVP a hand where the participants on the SB or the BB do not have sufficient stacks is forbidden
- the action counter is necessary for concurrency and undo reasons (consider a db interceptor)
- each action sends a domain event that triggers snapshot publication
- an action for all the remaining chips has to be transformed into an all-in
- actions are append only (undos are appended too)
- street is derived from the log

### Participant

Participant is a child entity of the Game. It holds a name and Chips.

### SignalR

Game snapshots will be sent via SignalR. In the MVP the same snapshot is sent when the user clicks a special refresh button. The last action will be sent with the snapshot so the FE can display it.

---

## Next slice: Lobby

The game state becomes readable. One new query, no new commands.

Two commits: `refactor(gameplay): make game playable from creation`, then `feat: add lobby slice`.

### Domain

`Game`:

| Change | Why |
|---|---|
| `GameStatus` enum -> `bool IsFinished`; delete `GameStatus.cs` | no third state; "a hand is running" is `CurrentHandId is not null` (added in StartHand) |
| new `_seatingOrder` (`List<ParticipantId>`) + read-only `SeatingOrder` | one ordered value on the root, so every change is a root write and `xmin` covers it |
| `Create(hostName, smallBlind, bigBlind)` appends the host to `_seatingOrder` | host is seated first |
| `Join(name)` drops the `chips` parameter; appends to `_participants` **and** `_seatingOrder` | joining order is the seating order |
| new `const int DefaultStartingStack = 1000` on `Game` | today hardcoded in two handlers |
| delete `Start()` | playable from creation |
| delete `BuildSeatsForNextHand()` | returns as `StartNextHand()` in the StartHand slice |
| `End()` guard: `!IsFinished` | old guard could never fire for a `NotStarted` game |

`Participant`: delete `SittingOut` and `UserId`. `UserId` returns with authentication.

`GameErrors`: delete `GameAlreadyStarted`, `InsufficientParticipantCount`,
`BuildSeatsForNotInProgressGame`, `InsufficientActiveParticipantCount`; `EndNotInProgressGame` ->
`EndFinishedGame`.

`Hand`, `HandStateCalculator`, `HandSeat`, `ChipsStack`: untouched.

### Persistence

One migration, e.g. `PlayableGameShape`. No data migration — Aspire provisions a fresh database per dev
startup.

- `games.status` (text) -> `games.is_finished` (boolean).
- `participants.sitting_out`, `participants.user_id`: dropped.
- `games.seating_order` — backing field mapped as a **primitive collection**. Expected DDL:
  `seating_order uuid[] NOT NULL DEFAULT '{}'`; read the generated migration to confirm. Try element-level
  conversion for `ParticipantId`, fall back to a `List<Guid>` backing field with a `ParticipantId` projection.
  Never `HasConversion` of the whole list to a string: without an order-sensitive `ValueComparer` EF misses the
  mutation and the write is silently lost. The primitive-collection comparer is order-sensitive.

### Contracts

`CreateGame` / `JoinGame` unchanged. New:

```
GameplayRoutes.GetGameState = "gameplay/games/{gameId:guid}"

GameStateResponse(
    Guid GameId,
    string JoinCode,
    bool IsFinished,
    int SmallBlind, int BigBlind,
    Guid HostParticipantId,
    IReadOnlyList<GameStateParticipant> Participants)   // ordered by SeatIndex

GameStateParticipant(Guid Id, string Name, int Chips, int SeatIndex)
```

No per-recipient field — one payload is broadcastable. `CurrentHand` is added by the slice that can fill it.

### Backend — `Features/GetGameState`

- `GetGameStateQuery(Guid GameId)`, `GetGameStateQueryHandler` -> `Result<GameStateResponse>`,
  `GameErrors.GameNotFound` when missing.
- Project, do not load the aggregate: `AsNoTracking`, `Select` into a flat shape, order participants by their
  index in the seating array in C#.
- No validator — `GameId` is typed from the route; `InvokeValidatedAsync` accepts an empty collection.
- `GetGameStateEndpoint`: internal, `MapGet`, `AllowAnonymous`,
  `result.Match(Results.Ok, CustomResults.Problem)`.
- `CreateGameEndpoint`: point its `Created` location at the new route constant.

### Frontend

- `Common/ParticipantSession` — scoped, in memory, holds `GameId` + `ParticipantId`, written by create/join,
  read by the Lobby to mark "you". `localStorage` later goes behind this same class. Not the query string.
- `IGameplayApi.GetGameStateAsync(Guid gameId, CancellationToken)`.
- `Features/Lobby/LobbyPage.razor`, route `/games/{GameId:guid}`:
  - one `ApplyState(snapshot)` method, every path goes through it: `OnInitializedAsync`, refresh button, later
    a SignalR push. A convention, not an interface.
  - large join code, blinds as one line, participants in seat order with chips, "you" / "host" markers,
    mobile-first.
  - "Refresh" button. No polling, no timer.
  - not-found state with a link back to `/games/setup`.
- `SetupGamePage`: replace both `/games/temp` stubs with `/games/{gameId}` and store the session.
- The Lobby is the between-hands view, not a pre-game screen — later slices return here.

### Tests

New project: `dotnet new xunit3 -o tests/Gameplay.UnitTests && dotnet sln add tests/Gameplay.UnitTests`
(model on `tests/Identity.UnitTests`) plus `InternalsVisibleTo("Gameplay.UnitTests")` in `Gameplay.csproj`.

- **Unit, all branches (`Game`)**: `Create` seats the host first, rejects `BB < SB` and a blank name; `Join`
  appends to both collections, uses `DefaultStartingStack`, rejects a finished game; `End` succeeds once,
  then fails.
- **Integration happy path**: create + 2 joins -> `GET` returns 3 participants in joining order,
  `SeatIndex` 0/1/2, correct host id, join code, blinds.
- **Integration not-found**: unknown `GameId` -> 404.
- **Integration mapping test** (silent failure mode): persist a known seating order, read it back from a fresh
  context (or after `ChangeTracker.Clear()`), assert the sequence is identical.
- **bUnit**: participants render with the "you" marker; the refresh button goes through `ApplyState` again.
- **Update existing**: `JoinGameIntegrationTests` uses `game.Start()` (gone) and the old `Join` signature.
- No per-endpoint validation / `ProblemDetails` test — proven once in `CreateGameIntegrationTests`.
- New helper in `Integration.Tests`: build a game with 3 seated participants (`create` + 2 × `join`).

---

## Slices after that (titles only, to be detailed one at a time)

1. `feat: add start hand slice` — `Game.CurrentHandId`, `StartNextHand()` on the root, host-only; guards:
   min. 3 seated, no running hand, every seated stack >= BB
2. `feat: add hand table view slice` — `CurrentHand` fills in inside `GetGameState`
3. `feat: add record hand action slice` — action counter bumped by interceptor; out-of-turn -> 409;
   all-in derived from a zero remaining stack; street derived from the log
4. `feat: add finish hand slice` — awards -> net deltas; hand closed and stacks written in one transaction
5. `feat: add abort hand slice` — touches no chips
6. `feat: add end game slice` — summary screen with final stacks, no settlement
7. `refactor: rework join into observe-or-claim` — see below

### Note for the join rework, so nothing before it blocks it

Joining requires only the join code and yields the snapshot: you are an observer. From there you create a new
participant or pick an existing one — and picking an existing one **is** the rejoin path.

Cheap because identity already lives on the client: picking an existing participant writes to
`ParticipantSession` — zero server state, zero request. Only "create a participant" stays a command.

- Chips are granted only when a participant is created; until rebuy exists there is no other source.
- Duplicate names become a view problem: show chips and seat index in the picker. No guard in `Game`.
- Observers create no row.
- SignalR: the group is the `GameId`; observers get the same payload as players.
- Accepted gap: whoever has the join code can claim somebody else's identity — same gap as the unverified
  `ActingParticipantId`. Authentication closes it once `UserId` returns: claiming is legal only if the
  participant is unbound, or bound to you.
- Needs a lookup by code, e.g. `GET gameplay/games?joinCode=123456`, returning the same `GameStateResponse`.

