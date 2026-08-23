using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class DeclareWinnersIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task DeclareWinners_EveryPotHasItsOwnWinner_ShowsThemInTheHandState()
    {
        // Arrange
        StartedHand hand = await StartHandWithSidePotAsync();

        // Act
        using HttpResponseMessage response = await PostDeclareWinnersAsync(
            hand.HandId,
            hand.HostParticipantId,
            [
                new PotWinner(0, hand.SmallBlindParticipantId),
                new PotWinner(1, hand.BigBlindParticipantId),
            ]);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GetHandStateResponse state = await GetHandStateAsync(hand.HandId);
        state.Pots.Single(pot => pot.Index == 0).WinnerParticipantIds
            .ShouldBe([hand.SmallBlindParticipantId]);
        state.Pots.Single(pot => pot.Index == 1).WinnerParticipantIds
            .ShouldBe([hand.BigBlindParticipantId]);
    }

    [Fact]
    public async Task DeclareWinners_ActingParticipantIsNotTheHost_ReturnsConflict()
    {
        // Arrange
        StartedHand hand = await FoldToTheBigBlindAsync();

        // Act
        using HttpResponseMessage response = await PostDeclareWinnersAsync(
            hand.HandId,
            hand.BigBlindParticipantId,
            [new PotWinner(0, hand.BigBlindParticipantId)]);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeclareWinners_WinnerIsNotEligibleForThePot_ReturnsConflict()
    {
        // Arrange
        StartedHand hand = await FoldToTheBigBlindAsync();

        // Act
        using HttpResponseMessage response = await PostDeclareWinnersAsync(
            hand.HandId,
            hand.HostParticipantId,
            [new PotWinner(0, hand.SmallBlindParticipantId)]);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeclareWinners_HandDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        Guid handId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response = await PostDeclareWinnersAsync(
            handId,
            Guid.NewGuid(),
            [new PotWinner(0, Guid.NewGuid())]);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> PostDeclareWinnersAsync(
        Guid handId,
        Guid actingParticipantId,
        IReadOnlyList<PotWinner> winners) =>
        Client.PostAsJsonAsync(
            GameplayRoutes.HandWinnersFor(handId),
            new DeclareWinnersRequest(actingParticipantId, winners),
            CancellationToken);
}
