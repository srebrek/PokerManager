using Contracts.Api.Gameplay;
using Contracts.IntegrationEvents.Gameplay;
using Wolverine.Tracking;
using HandActionType = Contracts.Api.Gameplay.HandActionType;

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
        (ITrackedSession Session, HttpResponseMessage Response) = await TrackAsync(() =>
            PostUndoLastActionAsync(hand.HandId, hand.HostParticipantId));

        // Assert
        using HttpResponseMessage response = Response;
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        GetHandStateResponse state = await GetHandStateAsync(hand.HandId);
        state.ShouldBeEquivalentTo(stateBeforeTheUndoneAction);

        HandActionUndoneIntegrationEvent published = Session.Sent.SingleMessage<HandActionUndoneIntegrationEvent>();
        published.HandId.ShouldBe(hand.HandId);
        published.SequenceNumber.ShouldBe(3);
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
