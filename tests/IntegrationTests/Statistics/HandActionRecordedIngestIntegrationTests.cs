using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics;
using Statistics.Domain.Entities;
using Statistics.Infrastructure.Data;
using Domain = Statistics.Domain.ValueObjects;

namespace IntegrationTests.Statistics;

public sealed class HandActionRecordedIngestIntegrationTests(ApiFactory factory)
    : StatisticsIntegrationTest(factory)
{
    [Fact]
    public async Task HandActionRecorded_HandWasIngested_StoresTheAction()
    {
        // Arrange
        Table table = Table.New();

        // Act
        await IngestAsync(
            HandStarted(table, At(0), table.Seats()),
            ActionRecorded(table, At(1), 2, table.Host, HandActionType.Raise, 100));

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
        {
            HandAction action = await context.Hands
                .Where(hand => hand.Id == table.HandId)
                .SelectMany(hand => hand.Actions)
                .AsNoTracking()
                .SingleAsync(action => action.SequenceNumber == 2, CancellationToken);

            action.ParticipantId.ShouldBe(table.Host);
            action.Type.ShouldBe(Domain.HandActionType.Raise);
            action.AmountTo.ShouldBe(100);
            action.OccurredAt.ShouldBe(At(1));
            action.UndoneAt.ShouldBeNull();
        });
    }

    [Fact]
    public async Task HandActionRecorded_TheSameEventIsDeliveredTwice_StoresASingleAction()
    {
        // Arrange
        Table table = Table.New();
        HandActionRecordedIntegrationEvent recorded =
            ActionRecorded(table, At(1), 2, table.Host, HandActionType.Fold);

        // Act
        await IngestAsync(HandStarted(table, At(0), table.Seats()), recorded, recorded);

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
            (await context.Hands
                .Where(hand => hand.Id == table.HandId)
                .Select(hand => hand.Actions.Count(action => action.SequenceNumber == 2))
                .SingleAsync(CancellationToken))
                .ShouldBe(1));
    }

    [Fact]
    public async Task HandActionRecorded_HandWasNotIngestedYet_FailsSoTheMessageIsRetried()
    {
        // Arrange
        Table table = Table.New();

        // Act
        Task ingest = IngestAsync(ActionRecorded(table, At(1), 2, table.Host, HandActionType.Fold));

        // Assert
        IngestParentMissingException exception = await Should.ThrowAsync<IngestParentMissingException>(ingest);
        exception.Message.ShouldContain(table.HandId.ToString());
    }
}
