using Contracts.Api.Gameplay;
// using Contracts.IntegrationEvents.Gameplay;
using Wolverine.Tracking;

namespace IntegrationTests.Gameplay;

public sealed class FinishHandIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task FinishHand_SidePotHasItsOwnWinner_SettlesEveryStackAndClosesTheHand()
    {
        // Arrange
        StartedHand hand = await StartHandWithSidePotAsync();
        await DeclareWinnersAsync(
            hand.HandId,
            hand.HostParticipantId,
            [new PotWinner(0, hand.SmallBlindParticipantId), new PotWinner(1, hand.BigBlindParticipantId)]);

        // Act
        // TODO: _ -> Session
        (ITrackedSession _, HttpResponseMessage Response) =
            await TrackAsync(() => PostFinishHandAsync(hand.HandId, hand.HostParticipantId));

        // Assert
        using HttpResponseMessage response = Response;
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GameStateResponse game = await GetGameStateAsync(hand.GameId);
        game.CurrentHandId.ShouldBeNull();
        game.Participants.Single(p => p.Id == hand.SmallBlindParticipantId).Chips.ShouldBe(150);
        game.Participants.Single(p => p.Id == hand.BigBlindParticipantId).Chips.ShouldBe(1000);
        game.Participants.Single(p => p.Id == hand.HostParticipantId).Chips.ShouldBe(900);

        // TODO: uncomment when a consumer appears
        // HandFinishedIntegrationEvent published = Session.Sent.SingleMessage<HandFinishedIntegrationEvent>();
        // published.HandId.ShouldBe(hand.HandId);
        // published.Pots.ShouldBe([new HandFinishedPot(0, 150), new HandFinishedPot(1, 100)]);
        // published.PotWinners.ShouldBe([
        //     new HandFinishedPotWinner(0, hand.SmallBlindParticipantId),
        //     new HandFinishedPotWinner(1, hand.BigBlindParticipantId),
        // ]);
        // published.Results.Count.ShouldBe(3);
        // published.Results.Sum(result => result.Net).ShouldBe(0);
        // published.Results.Single(result => result.ParticipantId == hand.SmallBlindParticipantId).Net.ShouldBe(100);
    }

    [Fact]
    public async Task FinishHand_WinnersWereNotDeclared_ReturnsConflict()
    {
        // Arrange
        StartedHand hand = await FoldToTheBigBlindAsync();

        // Act
        using HttpResponseMessage response = await PostFinishHandAsync(hand.HandId, hand.HostParticipantId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FinishHand_ActingParticipantIsNotTheHost_ReturnsConflict()
    {
        // Arrange
        StartedHand hand = await FoldToTheBigBlindAsync();
        await DeclareWinnersAsync(
            hand.HandId,
            hand.HostParticipantId,
            [new PotWinner(0, hand.BigBlindParticipantId)]);

        // Act
        using HttpResponseMessage response = await PostFinishHandAsync(hand.HandId, hand.BigBlindParticipantId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task FinishHand_HandDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        Guid handId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response = await PostFinishHandAsync(handId, Guid.NewGuid());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> PostFinishHandAsync(Guid handId, Guid actingParticipantId) =>
        Client.PostAsJsonAsync(
            GameplayRoutes.FinishHandFor(handId),
            new FinishHandRequest(actingParticipantId),
            CancellationToken);
}
