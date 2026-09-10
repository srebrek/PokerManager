#:project ../src/Contracts/Contracts.csproj
#:property EnableTrimAnalyzer=false
#:property EnableAotAnalyzer=false
#:property EnableSingleFileAnalyzer=false
#:property JsonSerializerIsReflectionEnabledByDefault=true

// Seeds a game with a few players and plays a number of hands to completion over the HTTP API, sending
// each action with a random delay so the game can be watched live (even just as an observer) in the
// browser while it runs. Requires the app to already be running (aspire run).
//
// Usage: dotnet run scripts/seed-game.cs -- [--base-url <url>] [--frontend-url <url>] [--players <n>]
//   [--hands <n>] [--min-delay-ms <n>] [--max-delay-ms <n>] [--stack <n>] [--fold-chance <0..1>]

using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Contracts.Api.Gameplay;
using SeedGame;

Options options = Options.Parse(args);

#pragma warning disable S4830 // Server certificates should be verified — local dev cert only.
using HttpClientHandler handler = new() { ServerCertificateCustomValidationCallback = (_, _, _, _) => true };
#pragma warning restore S4830
using HttpClient http = new(handler) { BaseAddress = new Uri(options.BaseUrl) };
http.DefaultRequestHeaders.Add("X-CSRF", "1");
JsonSerializerOptions json = new(JsonSerializerDefaults.Web);

CreateGameResponse game = await PostAsync<CreateGameRequest, CreateGameResponse>(
    new Uri(GameplayRoutes.Games, UriKind.Relative), new("Host"));
Guid gameId = game.GameId;
Guid hostId = game.ParticipantId;

Dictionary<Guid, string> names = new() { [hostId] = "Host" };
for (int i = 2; i <= options.Players; i++)
{
    string name = $"Player {i}";
    AddParticipantResponse added = await PostAsync<AddParticipantRequest, AddParticipantResponse>(
        GameplayRoutes.GameParticipantsFor(gameId), new(name));
    names[added.ParticipantId] = name;
}

foreach (Guid participantId in names.Keys)
{
    await PostNoContentAsync(GameplayRoutes.RebuyFor(gameId, participantId), new RebuyRequest(hostId, options.Stack));
}

GameStateResponse initialState = await GetAsync<GameStateResponse>(GameplayRoutes.GameFor(gameId));

Console.WriteLine($"Game ready — join code {initialState.JoinCode}, {names.Count} players.");
Console.WriteLine($"Watch live:  {options.FrontendUrl}/games/{gameId}");
Console.WriteLine($"Summary:     {options.FrontendUrl}/games/{gameId}/summary (once finished)");
Console.WriteLine();
#pragma warning disable CA1303 // Plain console output, not a localizable UI string.
Console.WriteLine("Starting in 5s — open the link now to watch as an observer...");
#pragma warning restore CA1303
await Task.Delay(TimeSpan.FromSeconds(5));

for (int handNumber = 1; handNumber <= options.Hands; handNumber++)
{
    await TopUpBustPlayersAsync();

    Console.WriteLine($"--- Hand {handNumber}/{options.Hands} ---");

    StartHandResponse started = await PostAsync<StartHandRequest, StartHandResponse>(
        GameplayRoutes.GameHandsFor(gameId), new(hostId));

    GetHandStateResponse initialHand = await GetAsync<GetHandStateResponse>(GameplayRoutes.HandFor(started.HandId));
    List<SeatSim> seats = [.. initialHand.Seats.Select(s => new SeatSim
    {
        Id = s.ParticipantId,
        StreetContribution = s.StreetContribution,
        RemainingStack = s.RemainingStack,
        State = s.State,
    })];

    await PlayHandAsync(started.HandId, seats);

    GetHandStateResponse finished = await GetAsync<GetHandStateResponse>(GameplayRoutes.HandFor(started.HandId));
    List<PotWinner> winners = [.. finished.Pots.SelectMany(DecideWinners)];

    await PostNoContentAsync(GameplayRoutes.HandWinnersFor(started.HandId), new DeclareWinnersRequest(hostId, winners));
    await PostNoContentAsync(GameplayRoutes.FinishHandFor(started.HandId), new FinishHandRequest(hostId));

    string potSummary = string.Join(
        ", ",
        finished.Pots.Select(pot => $"pot {pot.Amount} → {string.Join('+', WinnerNamesFor(pot.Index, winners))}"));
    Console.WriteLine($"  finished: {potSummary}");

    await DelayAsync();
}

await PostNoContentAsync(GameplayRoutes.FinishGameFor(gameId), new FinishGameRequest(hostId));
Console.WriteLine();
Console.WriteLine($"Game finished. Summary: {options.FrontendUrl}/games/{gameId}/summary");

#pragma warning disable CA5394 // Random is fine for choosing a fake showdown winner in a dev seed script.
List<PotWinner> DecideWinners(HandPotState pot)
{
    IReadOnlyList<Guid> eligible = pot.EligibleParticipantIds;
    bool split = eligible.Count > 1 && Random.Shared.NextDouble() < 0.2;
    IEnumerable<Guid> selected = split
        ? [.. eligible.OrderBy(_ => Random.Shared.Next()).Take(2)]
        : [eligible[Random.Shared.Next(eligible.Count)]];

    return [.. selected.Select(participantId => new PotWinner(pot.Index, participantId))];
}
#pragma warning restore CA5394

IEnumerable<string> WinnerNamesFor(int potIndex, List<PotWinner> winners) =>
    winners.Where(w => w.PotIndex == potIndex).Select(w => names[w.ParticipantId]);

async Task TopUpBustPlayersAsync()
{
    GameStateResponse state = await GetAsync<GameStateResponse>(GameplayRoutes.GameFor(gameId));
    foreach (GameStateParticipant participant in state.Participants)
    {
        if (participant.Chips < state.BigBlind)
        {
            await PostNoContentAsync(
                GameplayRoutes.RebuyFor(gameId, participant.Id), new RebuyRequest(hostId, options.Stack));
            Console.WriteLine($"  rebuy: {participant.Name} topped up to {options.Stack}");
        }
    }
}

async Task PlayHandAsync(Guid handId, List<SeatSim> seats)
{
    int currentBet = seats.Max(s => s.StreetContribution);
    int actingSeatIndex = 2 % seats.Count;
    Street street = Street.PreFlop;

    while (street is not Street.Finished)
    {
        while (seats[actingSeatIndex].State is not SeatState.Active)
        {
            actingSeatIndex = (actingSeatIndex + 1) % seats.Count;
        }

        SeatSim actor = seats[actingSeatIndex];
        int toCall = currentBet - actor.StreetContribution;
        HandActionType type = DecideAction(actor, currentBet, toCall);

        await PostNoContentAsync(GameplayRoutes.HandActionsFor(handId), new RecordActionRequest(actor.Id, type));
        Console.WriteLine($"  {street} — {names[actor.Id]}: {type}");

        ApplyAction(actor, type, toCall);

        bool everyoneElseFolded = seats.Count(s => s.State is not SeatState.Folded) == 1;
        bool roundClosed = seats.All(s =>
            s.State is not SeatState.Active || (s.HasActedThisStreet && s.StreetContribution == currentBet));

        currentBet = seats.Max(s => s.StreetContribution);

        if (!everyoneElseFolded && !roundClosed)
        {
            actingSeatIndex = (actingSeatIndex + 1) % seats.Count;
            await DelayAsync();
            continue;
        }

        street = everyoneElseFolded || street is Street.River || seats.Count(s => s.State is SeatState.Active) <= 1
            ? Street.Finished
            : street + 1;

        currentBet = 0;
        actingSeatIndex = 0;
        foreach (SeatSim seat in seats)
        {
            seat.StreetContribution = 0;
            seat.HasActedThisStreet = false;
        }

        await DelayAsync();
    }
}

static void ApplyAction(SeatSim actor, HandActionType type, int toCall)
{
    if (type is HandActionType.Fold)
    {
        actor.State = SeatState.Folded;
        actor.HasActedThisStreet = true;
        return;
    }

    int pushed = type is HandActionType.AllIn ? actor.RemainingStack : toCall;
    actor.StreetContribution += pushed;
    actor.RemainingStack -= pushed;
    actor.State = type is HandActionType.AllIn ? SeatState.AllIn : SeatState.Active;
    actor.HasActedThisStreet = true;
}

#pragma warning disable CA5394 // Random is fine for deciding a bot's action in a dev seed script.
HandActionType DecideAction(SeatSim actor, int currentBet, int toCall)
{
    if (currentBet == 0)
    {
        return HandActionType.Check;
    }

    if (toCall == 0)
    {
        return HandActionType.Call;
    }

    if (toCall >= actor.RemainingStack)
    {
        return HandActionType.AllIn;
    }

    return Random.Shared.NextDouble() < options.FoldChance ? HandActionType.Fold : HandActionType.Call;
}

Task DelayAsync() => Task.Delay(Random.Shared.Next(options.MinDelayMs, options.MaxDelayMs));
#pragma warning restore CA5394

async Task<TResponse> PostAsync<TRequest, TResponse>(Uri route, TRequest body)
{
    HttpResponseMessage response = await http.PostAsJsonAsync(route, body, json);
    await EnsureSuccessAsync(response, route.ToString());
    return (await response.Content.ReadFromJsonAsync<TResponse>(json))!;
}

async Task PostNoContentAsync<TRequest>(Uri route, TRequest body)
{
    HttpResponseMessage response = await http.PostAsJsonAsync(route, body, json);
    await EnsureSuccessAsync(response, route.ToString());
}

async Task<TResponse> GetAsync<TResponse>(Uri route)
{
    HttpResponseMessage response = await http.GetAsync(route);
    await EnsureSuccessAsync(response, route.ToString());
    return (await response.Content.ReadFromJsonAsync<TResponse>(json))!;
}

static async Task EnsureSuccessAsync(HttpResponseMessage response, string route)
{
    if (response.IsSuccessStatusCode)
    {
        return;
    }

    string body = await response.Content.ReadAsStringAsync();
    throw new InvalidOperationException(
        $"{route} → {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
}

namespace SeedGame
{
    internal sealed class SeatSim
    {
        public required Guid Id { get; init; }
        public int StreetContribution { get; set; }
        public int RemainingStack { get; set; }
        public SeatState State { get; set; }
        public bool HasActedThisStreet { get; set; }
    }

    internal sealed record Options(
        string BaseUrl,
        string FrontendUrl,
        int Players,
        int Hands,
        int MinDelayMs,
        int MaxDelayMs,
        int Stack,
        double FoldChance)
    {
        public static Options Parse(string[] arguments)
        {
#pragma warning disable S1075 // Default dev URLs, overridable via --base-url/--frontend-url.
            string baseUrl = "https://localhost:7200/api/";
            string frontendUrl = "https://localhost:7200";
#pragma warning restore S1075
            int players = 5;
            int hands = 10;
            int minDelayMs = 400;
            int maxDelayMs = 1800;
            int stack = 300;
            double foldChance = 0.15;

            for (int i = 0; i < arguments.Length - 1; i++)
            {
                string value = arguments[i + 1];
                switch (arguments[i])
                {
                    case "--base-url":
                        baseUrl = value;
                        break;
                    case "--frontend-url":
                        frontendUrl = value;
                        break;
                    case "--players":
                        players = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "--hands":
                        hands = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "--min-delay-ms":
                        minDelayMs = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "--max-delay-ms":
                        maxDelayMs = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "--stack":
                        stack = int.Parse(value, CultureInfo.InvariantCulture);
                        break;
                    case "--fold-chance":
                        foldChance = double.Parse(value, CultureInfo.InvariantCulture);
                        break;
                }
            }

            return new Options(
                baseUrl, frontendUrl, Math.Max(players, 3), hands, minDelayMs, maxDelayMs, stack, foldChance);
        }
    }
}
