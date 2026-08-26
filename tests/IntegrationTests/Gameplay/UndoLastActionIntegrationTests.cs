using Contracts.Api.Gameplay;

namespace IntegrationTests.Gameplay;

public sealed class UndoLastActionIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task UndoLastAction_ActionWasRecorded_RestoresTheStateFromBeforeIt()
    {
        // Arrange
        StartedHand hand = await StartThreeSeatHandAsync(1000, 1000, 1000);
        await ActAsync(hand.HandId, hand.HostParticipantId, HandActionType.Raise, 100);
        GetHandStateResponse stateBeforeTheUndoneAction = await GetHandStateAsync(hand.HandId);
        await ActAsync(hand.HandId, hand.SmallBlindParticipantId, HandActionType.Call);

        // Act
        using HttpResponseMessage response = await PostUndoLastActionAsync(hand.HandId, hand.HostParticipantId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GetHandStateResponse state = await GetHandStateAsync(hand.HandId);
        state.ShouldBeEquivalentTo(stateBeforeTheUndoneAction);
    }

    [Fact]
    public async Task UndoLastAction_OnlyTheBlindsAreLeft_ReturnsConflict()
    {
        // Arrange
        StartedHand hand = await StartThreeSeatHandAsync(1000, 1000, 1000);
        await ActAsync(hand.HandId, hand.HostParticipantId, HandActionType.Fold);
        using HttpResponseMessage undoneAction = await PostUndoLastActionAsync(
            hand.HandId, hand.HostParticipantId);

        // Act
        using HttpResponseMessage response = await PostUndoLastActionAsync(hand.HandId, hand.HostParticipantId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UndoLastAction_ActingParticipantIsNotTheHost_ReturnsConflict()
    {
        // Arrange
        StartedHand hand = await FoldToTheBigBlindAsync();

        // Act
        using HttpResponseMessage response = await PostUndoLastActionAsync(hand.HandId, hand.BigBlindParticipantId);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UndoLastAction_HandDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        Guid handId = Guid.NewGuid();

        // Act
        using HttpResponseMessage response = await PostUndoLastActionAsync(handId, Guid.NewGuid());

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private Task<HttpResponseMessage> PostUndoLastActionAsync(Guid handId, Guid actingParticipantId) =>
        Client.PostAsJsonAsync(
            GameplayRoutes.UndoLastActionFor(handId),
            new UndoLastActionRequest(actingParticipantId),
            CancellationToken);
}
