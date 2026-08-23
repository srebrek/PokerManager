using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class RecordActionIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task RecordAction_AllInShortOfTheCurrentBet_FormsASidePot()
    {
        // Arrange
        StartedHand hand = await StartThreeSeatHandAsync(hostStack: 1000, smallBlindStack: 1000, bigBlindStack: 1000);

        // Act
        using HttpResponseMessage response =
            await PostActionAsync(hand.HandId, hand.HostParticipantId, HandActionType.Call);

        // Assert
        GetHandStateResponse state = await GetHandStateAsync(hand.HandId);
        state.LastActionNumber.ShouldBe(2); // BB, SB, Call
    }

    [Fact]
    public async Task RecordAction_CallForTheEntireRemainingStack_ReturnsConflict()
    {
        // Arrange
        StartedHand hand = await StartThreeSeatHandAsync(hostStack: 10, smallBlindStack: 1000, bigBlindStack: 1000);

        // Act
        using HttpResponseMessage response =
            await PostActionAsync(hand.HandId, hand.HostParticipantId, HandActionType.Call);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task RecordAction_HandDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        Guid handId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response = await PostActionAsync(handId, Guid.NewGuid(), HandActionType.Fold);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> PostActionAsync(
        Guid handId,
        Guid participantId,
        HandActionType type,
        int? amountTo = null) =>
        Client.PostAsJsonAsync(
            GameplayRoutes.HandActionsFor(handId),
            new RecordActionRequest(participantId, type, amountTo),
            CancellationToken);
}
