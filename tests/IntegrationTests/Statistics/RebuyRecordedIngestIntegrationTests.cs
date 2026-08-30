using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Infrastructure.Data;

namespace IntegrationTests.Statistics;

public sealed class RebuyRecordedIngestIntegrationTests(ApiFactory factory) : StatisticsIntegrationTest(factory)
{
    [Fact]
    public async Task RebuyRecorded_ValidEvent_StoresTheRebuy()
    {
        // Arrange
        Table table = Table.New();

        // Act
        await IngestAsync(RebuyRecorded(table, At(0), table.Host, 500));

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
        {
            var rebuy = await context.Rebuys
                .Where(rebuy => rebuy.GameId == table.GameId)
                .Select(rebuy => new { rebuy.ParticipantId, rebuy.Amount, rebuy.OccurredAt })
                .SingleAsync(CancellationToken);

            rebuy.ParticipantId.ShouldBe(table.Host);
            rebuy.Amount.ShouldBe(500);
            rebuy.OccurredAt.ShouldBe(At(0));
        });
    }

    [Fact]
    public async Task RebuyRecorded_TheSameEventIsDeliveredTwice_StoresASingleRebuy()
    {
        // Arrange
        Table table = Table.New();
        RebuyRecordedIntegrationEvent rebuy = RebuyRecorded(table, At(0), table.Host, 500);

        // Act
        await IngestAsync(rebuy, rebuy);

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
            (await context.Rebuys.CountAsync(rebuy => rebuy.GameId == table.GameId, CancellationToken)).ShouldBe(1));
    }

    [Fact]
    public async Task RebuyRecorded_ParticipantRebuysTwice_StoresBothRebuys()
    {
        // Arrange
        Table table = Table.New();

        // Act
        await IngestAsync(
            RebuyRecorded(table, At(0), table.Host, 500),
            RebuyRecorded(table, At(5), table.Host, 300));

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
            (await context.Rebuys
                .Where(rebuy => rebuy.ParticipantId == table.Host)
                .SumAsync(rebuy => rebuy.Amount, CancellationToken))
                .ShouldBe(800));
    }
}
