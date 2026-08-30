using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics;
using Statistics.Domain.Entities;
using Statistics.Infrastructure.Data;
using Domain = Statistics.Domain.ValueObjects;

namespace IntegrationTests.Statistics;

public sealed class HandActionUndoneIngestIntegrationTests(ApiFactory factory) : StatisticsIntegrationTest(factory)
{
    [Fact]
    public async Task HandActionUndone_ActionIsLive_MarksItUndoneAndKeepsTheRow()
    {
        // Arrange
        Table table = Table.New();

        // Act
        await IngestAsync(
            HandStarted(table, At(0), table.Seats()),
            ActionRecorded(table, At(1), 2, table.Host, HandActionType.Raise, 100),
            ActionUndone(table, At(2), 2));

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
        {
            HandAction action = await context.Hands
                .Where(hand => hand.Id == table.HandId)
                .SelectMany(hand => hand.Actions)
                .AsNoTracking()
                .SingleAsync(action => action.SequenceNumber == 2, CancellationToken);

            action.Type.ShouldBe(Domain.HandActionType.Raise);
            action.UndoneAt.ShouldBe(At(2));
        });
    }

    [Fact]
    public async Task HandActionUndone_IsRedeliveredAfterTheReplacingActionWasIngested_LeavesTheReplacementLive()
    {
        // Arrange
        Table table = Table.New();
        HandActionUndoneIntegrationEvent undone = ActionUndone(table, At(2), 2);

        // Act
        await IngestAsync(
            HandStarted(table, At(0), table.Seats()),
            ActionRecorded(table, At(1), 2, table.Host, HandActionType.Raise, 100),
            undone,
            ActionRecorded(table, At(3), 2, table.Host, HandActionType.Fold),
            undone);

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
        {
            List<HandAction> sequenceTwo = await context.Hands
                .Where(hand => hand.Id == table.HandId)
                .SelectMany(hand => hand.Actions)
                .Where(action => action.SequenceNumber == 2)
                .AsNoTracking()
                .ToListAsync(CancellationToken);

            sequenceTwo.Count.ShouldBe(2);
            sequenceTwo.Single(action => action.Type is Domain.HandActionType.Raise).UndoneAt.ShouldBe(At(2));
            sequenceTwo.Single(action => action.Type is Domain.HandActionType.Fold).UndoneAt.ShouldBeNull();
        });
    }

    [Fact]
    public async Task HandActionUndone_HandWasNotIngestedYet_FailsSoTheMessageIsRetried()
    {
        // Arrange
        Table table = Table.New();

        // Act
        Task ingest = IngestAsync(ActionUndone(table, At(2), 2));

        // Assert
        IngestParentMissingException exception = await Should.ThrowAsync<IngestParentMissingException>(ingest);
        exception.Message.ShouldContain(table.HandId.ToString());
    }

    [Fact]
    public async Task HandActionUndone_ActionWasNotIngestedYet_FailsSoTheMessageIsRetried()
    {
        // Arrange
        Table table = Table.New();

        // Act
        Task ingest = IngestAsync(
            HandStarted(table, At(0), table.Seats()),
            ActionUndone(table, At(2), 7));

        // Assert
        IngestParentMissingException exception = await Should.ThrowAsync<IngestParentMissingException>(ingest);
        exception.Message.ShouldContain("7");
    }
}
