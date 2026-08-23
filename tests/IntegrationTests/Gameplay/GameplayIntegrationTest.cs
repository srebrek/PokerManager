using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public abstract class GameplayIntegrationTest(ApiFactory factory) : BaseIntegrationTest(factory)
{
    protected async Task<CreateGameResponse> CreateGameAsync(string hostName)
    {
        CreateGameRequest request = new(hostName);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.Games,
            request,
            CancellationToken);
        response.EnsureSuccessStatusCode();

        CreateGameResponse? body = await response.Content.ReadFromJsonAsync<CreateGameResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }

    protected async Task<AddParticipantResponse> AddParticipantAsync(Guid gameId, string participantName)
    {
        AddParticipantRequest request = new(participantName);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.GameParticipantsFor(gameId),
            request,
            CancellationToken);
        response.EnsureSuccessStatusCode();

        AddParticipantResponse? body =
            await response.Content.ReadFromJsonAsync<AddParticipantResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }

    protected async Task<GameStateResponse> GetGameStateAsync(Guid gameId)
    {
        using HttpResponseMessage response = await Client.GetAsync(
            GameplayRoutes.GameFor(gameId),
            CancellationToken);
        response.EnsureSuccessStatusCode();

        GameStateResponse? body = await response.Content.ReadFromJsonAsync<GameStateResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }

    protected async Task RebuyAsync(Guid gameId, Guid hostParticipantId, Guid participantId, int amount)
    {
        RebuyRequest request = new(hostParticipantId, amount);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.RebuyFor(gameId, participantId),
            request,
            CancellationToken);
        await ShouldSucceedAsync(response);
    }

    protected async Task<Guid> StartHandAsync(Guid gameId, Guid hostParticipantId)
    {
        StartHandRequest request = new(hostParticipantId);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.GameHandsFor(gameId),
            request,
            CancellationToken);
        await ShouldSucceedAsync(response);

        StartHandResponse? body = await response.Content.ReadFromJsonAsync<StartHandResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body.HandId;
    }

    protected async Task<StartedHand> StartThreeSeatHandAsync(int hostStack, int smallBlindStack, int bigBlindStack)
    {
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse smallBlind = await AddParticipantAsync(game.GameId, "TestSmallBlindName");
        AddParticipantResponse bigBlind = await AddParticipantAsync(game.GameId, "TestBigBlindName");

        await RebuyAsync(game.GameId, game.ParticipantId, game.ParticipantId, hostStack);
        await RebuyAsync(game.GameId, game.ParticipantId, smallBlind.ParticipantId, smallBlindStack);
        await RebuyAsync(game.GameId, game.ParticipantId, bigBlind.ParticipantId, bigBlindStack);

        Guid handId = await StartHandAsync(game.GameId, game.ParticipantId);

        return new StartedHand(
            game.GameId,
            handId,
            game.ParticipantId,
            smallBlind.ParticipantId,
            bigBlind.ParticipantId);
    }

    protected async Task<StartedHand> FoldToTheBigBlindAsync()
    {
        StartedHand hand = await StartThreeSeatHandAsync(1000, 1000, 1000);
        await ActAsync(hand.HandId, hand.HostParticipantId, HandActionType.Fold);
        await ActAsync(hand.HandId, hand.SmallBlindParticipantId, HandActionType.Fold);

        return hand;
    }

    protected async Task<StartedHand> StartHandWithSidePotAsync()
    {
        StartedHand hand = await StartThreeSeatHandAsync(hostStack: 1000, smallBlindStack: 50, bigBlindStack: 1000);
        await ActAsync(hand.HandId, hand.HostParticipantId, HandActionType.Raise, 100);
        await ActAsync(hand.HandId, hand.SmallBlindParticipantId, HandActionType.AllIn);
        await ActAsync(hand.HandId, hand.BigBlindParticipantId, HandActionType.Call);

        for (int street = 0; street < 3; street++)
        {
            await ActAsync(hand.HandId, hand.BigBlindParticipantId, HandActionType.Check);
            await ActAsync(hand.HandId, hand.HostParticipantId, HandActionType.Check);
        }

        return hand;
    }

    protected async Task<GetHandStateResponse> GetHandStateAsync(Guid handId)
    {
        using HttpResponseMessage response = await Client.GetAsync(GameplayRoutes.HandFor(handId), CancellationToken);
        await ShouldSucceedAsync(response);

        GetHandStateResponse? body = await response.Content.ReadFromJsonAsync<GetHandStateResponse>(CancellationToken);
        body.ShouldNotBeNull();
        return body;
    }

    protected async Task ActAsync(Guid handId, Guid participantId, HandActionType type, int? amountTo = null)
    {
        RecordActionRequest request = new(participantId, type, amountTo);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.HandActionsFor(handId),
            request,
            CancellationToken);
        await ShouldSucceedAsync(response);
    }

    protected async Task DeclareWinnersAsync(
        Guid handId,
        Guid actingParticipantId,
        IReadOnlyList<PotWinner> winners)
    {
        DeclareWinnersRequest request = new(actingParticipantId, winners);
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            GameplayRoutes.HandWinnersFor(handId),
            request,
            CancellationToken);
        await ShouldSucceedAsync(response);
    }

    private async Task ShouldSucceedAsync(HttpResponseMessage response) =>
        response.IsSuccessStatusCode.ShouldBeTrue(
            $"{response.RequestMessage?.RequestUri} returned {(int)response.StatusCode}: "
            + await response.Content.ReadAsStringAsync(CancellationToken));

    protected sealed record StartedHand(
        Guid GameId,
        Guid HandId,
        Guid HostParticipantId,
        Guid SmallBlindParticipantId,
        Guid BigBlindParticipantId);
}
