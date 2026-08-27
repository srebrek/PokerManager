using Contracts.Api.Gameplay;
// using Contracts.IntegrationEvents.Gameplay;
using Wolverine.Tracking;
// using EventHandActionType = Contracts.IntegrationEvents.Gameplay.HandActionType;

namespace IntegrationTests.Gameplay;

public sealed class StartHandIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task StartHand_ThreeFundedParticipants_SeatsThePlayersAndPostsTheBlinds()
    {
        // Arrange
        FundedGame game = await CreateFundedGameAsync();

        // Act
        // TODO: _ -> Session
        (ITrackedSession _, HttpResponseMessage Response) =
            await TrackAsync(() => PostStartHandAsync(game.GameId, game.HostParticipantId));

        // Assert
        using HttpResponseMessage response = Response;
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        StartHandResponse? body = await response.Content.ReadFromJsonAsync<StartHandResponse>(CancellationToken);
        body.ShouldNotBeNull();

        GameStateResponse gameState = await GetGameStateAsync(game.GameId);
        gameState.CurrentHandId.ShouldBe(body.HandId);

        GetHandStateResponse handState = await GetHandStateAsync(body.HandId);
        handState.Status.ShouldBe(HandStatus.InProgress);
        handState.Street.ShouldBe(Street.PreFlop);
        handState.LastActionNumber.ShouldBe(1);

        handState.Seats.ShouldBe([
            new HandStateSeat(game.SmallBlindParticipantId, 5, 795, SeatState.Active),
            new HandStateSeat(game.BigBlindParticipantId, 10, 590, SeatState.Active),
            new HandStateSeat(game.HostParticipantId, 0, 1000, SeatState.Active),
        ]);

        HandPotState pot = handState.Pots.ShouldHaveSingleItem();
        pot.Amount.ShouldBe(15);
        pot.EligibleParticipantIds.ShouldBe([
            game.SmallBlindParticipantId, game.BigBlindParticipantId, game.HostParticipantId,
        ]);
        // TODO: uncomment when a consumer appears
        // HandStartedIntegrationEvent published = Session.Sent.SingleMessage<HandStartedIntegrationEvent>();
        // published.HandId.ShouldBe(body.HandId);
        // published.GameId.ShouldBe(game.GameId);

        // published.Seats.ShouldBe([
        //     new HandStartedSeat(game.SmallBlindParticipantId, "TestSmallBlindName", 0, 800),
        //     new HandStartedSeat(game.BigBlindParticipantId, "TestBigBlindName", 1, 600),
        //     new HandStartedSeat(game.HostParticipantId, "TestHostName", 2, 1000),
        // ]);

        // published.Blinds.ShouldBe([
        //     new HandStartedBlind(0, game.SmallBlindParticipantId, EventHandActionType.PostSmallBlind, 5),
        //     new HandStartedBlind(1, game.BigBlindParticipantId, EventHandActionType.PostBigBlind, 10),
        // ]);
    }

    [Fact]
    public async Task StartHand_GameDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        Guid gameId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response = await PostStartHandAsync(gameId, Guid.NewGuid());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task StartHand_ActingParticipantIsNotHost_ReturnsConflict()
    {
        // Arrange
        FundedGame game = await CreateFundedGameAsync();

        // Act
        using HttpResponseMessage response = await PostStartHandAsync(game.GameId, game.SmallBlindParticipantId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        GameStateResponse gameState = await GetGameStateAsync(game.GameId);
        gameState.CurrentHandId.ShouldBeNull();
    }

    [Fact]
    public async Task StartHand_FewerParticipantsThanTheMinimum_ReturnsConflict()
    {
        // Arrange
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse second = await AddParticipantAsync(game.GameId, "TestSecondName");
        await RebuyAsync(game.GameId, game.ParticipantId, game.ParticipantId, 1000);
        await RebuyAsync(game.GameId, game.ParticipantId, second.ParticipantId, 1000);

        // Act
        using HttpResponseMessage response = await PostStartHandAsync(game.GameId, game.ParticipantId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        GameStateResponse gameState = await GetGameStateAsync(game.GameId);
        gameState.CurrentHandId.ShouldBeNull();
    }

    [Fact]
    public async Task StartHand_HandIsAlreadyRunning_ReturnsConflictAndKeepsTheRunningHand()
    {
        // Arrange
        FundedGame game = await CreateFundedGameAsync();
        Guid handId = await StartHandAsync(game.GameId, game.HostParticipantId);

        // Act
        using HttpResponseMessage response = await PostStartHandAsync(game.GameId, game.HostParticipantId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);

        GameStateResponse gameState = await GetGameStateAsync(game.GameId);
        gameState.CurrentHandId.ShouldBe(handId);
    }

    private async Task<FundedGame> CreateFundedGameAsync()
    {
        CreateGameResponse game = await CreateGameAsync("TestHostName");
        AddParticipantResponse smallBlind = await AddParticipantAsync(game.GameId, "TestSmallBlindName");
        AddParticipantResponse bigBlind = await AddParticipantAsync(game.GameId, "TestBigBlindName");

        await RebuyAsync(game.GameId, game.ParticipantId, game.ParticipantId, 1000);
        await RebuyAsync(game.GameId, game.ParticipantId, smallBlind.ParticipantId, 800);
        await RebuyAsync(game.GameId, game.ParticipantId, bigBlind.ParticipantId, 600);

        return new FundedGame(
            game.GameId,
            game.ParticipantId,
            smallBlind.ParticipantId,
            bigBlind.ParticipantId);
    }

    private Task<HttpResponseMessage> PostStartHandAsync(Guid gameId, Guid actingParticipantId)
    {
        StartHandRequest request = new(actingParticipantId);
        return Client.PostAsJsonAsync(GameplayRoutes.GameHandsFor(gameId), request, CancellationToken);
    }

    private sealed record FundedGame(
        Guid GameId,
        Guid HostParticipantId,
        Guid SmallBlindParticipantId,
        Guid BigBlindParticipantId);
}
