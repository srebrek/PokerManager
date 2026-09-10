using Contracts.Api.Gameplay;
using Contracts.IntegrationEvents.Gameplay;
using Wolverine.Tracking;
using HandActionType = Contracts.Api.Gameplay.HandActionType;

namespace IntegrationTests.Gameplay;

public sealed class RecordActionIntegrationTests(ApiFactory factory) : GameplayIntegrationTest(factory)
{
    [Fact]
    public async Task RecordAction_AllInShortOfTheCurrentBet_FormsASidePot()
    {
        // Arrange
        (_, StartedHand hand) = await TrackAsync(() =>
            StartThreeSeatHandAsync(hostStack: 1000, smallBlindStack: 1000, bigBlindStack: 1000));

        // Act
        (ITrackedSession Session, HttpResponseMessage Response) = await TrackAsync(() =>
            PostActionAsync(hand.HandId, hand.HostParticipantId, HandActionType.Call));

        // Assert
        using HttpResponseMessage response = Response;
        GetHandStateResponse state = await GetHandStateAsync(hand.HandId);
        state.LastActionNumber.ShouldBe(2); // BB, SB, Call

        HandActionRecordedIntegrationEvent published = Session.Sent.SingleMessage<HandActionRecordedIntegrationEvent>();
        published.HandId.ShouldBe(hand.HandId);
        published.ParticipantId.ShouldBe(hand.HostParticipantId);
        published.SequenceNumber.ShouldBe(2);
        published.Type.ShouldBe(Contracts.IntegrationEvents.Gameplay.HandActionType.Call);
        published.AmountTo.ShouldBeNull();
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
