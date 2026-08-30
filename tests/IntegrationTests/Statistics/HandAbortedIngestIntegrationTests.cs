using Microsoft.EntityFrameworkCore;
using Statistics;
using Statistics.Infrastructure.Data;

namespace IntegrationTests.Statistics;

public sealed class HandAbortedIngestIntegrationTests(ApiFactory factory) : StatisticsIntegrationTest(factory)
{
    [Fact]
    public async Task HandAborted_HandWasIngested_StampsTheAbortTime()
    {
        // Arrange
        Table table = Table.New();

        // Act
        await IngestAsync(HandStarted(table, At(0), table.Seats()), HandAborted(table, At(1)));

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
            (await context.Hands
                .Where(hand => hand.Id == table.HandId)
                .Select(hand => hand.AbortedAt)
                .SingleAsync(CancellationToken))
                .ShouldBe(At(1)));
    }

    [Fact]
    public async Task HandAborted_TheSameEventIsDeliveredTwice_KeepsTheOriginalAbortTime()
    {
        // Arrange
        Table table = Table.New();

        // Act
        await IngestAsync(
            HandStarted(table, At(0), table.Seats()),
            HandAborted(table, At(1)),
            HandAborted(table, At(2)));

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
            (await context.Hands
                .Where(hand => hand.Id == table.HandId)
                .Select(hand => hand.AbortedAt)
                .SingleAsync(CancellationToken))
                .ShouldBe(At(1)));
    }

    [Fact]
    public async Task HandAborted_ArrivedBeforeTheHandWasIngested_FailsSoTheMessageIsRetried()
    {
        // Arrange
        Table table = Table.New();

        // Act
        Task ingest = IngestAsync(HandAborted(table, At(1)));

        // Assert
        IngestParentMissingException exception = await Should.ThrowAsync<IngestParentMissingException>(ingest);
        exception.Message.ShouldContain(table.HandId.ToString());
    }
}
