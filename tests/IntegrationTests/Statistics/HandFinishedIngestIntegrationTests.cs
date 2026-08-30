using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;
using Statistics.Infrastructure.Data;
using Domain = Statistics.Domain.ValueObjects;

namespace IntegrationTests.Statistics;

public sealed class HandFinishedIngestIntegrationTests(ApiFactory factory) : StatisticsIntegrationTest(factory)
{
    [Fact]
    public async Task HandFinished_HandWasIngested_StoresResultsPotsAndWinners()
    {
        // Arrange
        Table table = Table.New();

        // Act
        await IngestAsync(HandStarted(table, At(0), table.Seats()), Finished(table));

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
        {
            Hand hand = await context.Hands
                .AsNoTracking()
                .SingleAsync(hand => hand.Id == table.HandId, CancellationToken);

            hand.FinishedAt.ShouldBe(At(3));

            hand.Results.Count.ShouldBe(3);
            hand.Results.Sum(result => result.Net).ShouldBe(0);

            Domain.HandResult bigBlind = hand.Results.Single(result => result.ParticipantId == table.BigBlind);
            bigBlind.ParticipantName.ShouldBe("TestBigBlindName");
            bigBlind.Net.ShouldBe(5);
            bigBlind.EndingChips.ShouldBe(1005);

            Domain.HandPot pot = hand.Pots.Single();
            pot.PotIndex.ShouldBe(0);
            pot.Amount.ShouldBe(15);
            pot.Winners.Select(winner => winner.ParticipantId).ShouldBe([table.BigBlind]);
        });
    }

    [Fact]
    public async Task HandFinished_TheSameEventIsDeliveredTwice_StoresASingleSetOfResults()
    {
        // Arrange
        Table table = Table.New();
        HandFinishedIntegrationEvent finished = Finished(table);

        // Act
        await IngestAsync(HandStarted(table, At(0), table.Seats()), finished, finished);

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
        {
            var counts = await context.Hands
                .Where(hand => hand.Id == table.HandId)
                .Select(hand => new
                {
                    ResultCount = hand.Results.Count,
                    PotCount = hand.Pots.Count,
                    WinnerCount = hand.Pots.Sum(pot => pot.Winners.Count),
                })
                .SingleAsync(CancellationToken);

            counts.ResultCount.ShouldBe(3);
            counts.PotCount.ShouldBe(1);
            counts.WinnerCount.ShouldBe(1);
        });
    }

    private static HandFinishedIntegrationEvent Finished(Table table) =>
        HandFinished(
            table,
            At(3),
            [new HandFinishedPot(0, 15)],
            [new HandFinishedPotWinner(0, table.BigBlind)],
            [
                new HandFinishedResult(table.Host, "TestHostName", 0, 1000),
                new HandFinishedResult(table.SmallBlind, "TestSmallBlindName", -5, 995),
                new HandFinishedResult(table.BigBlind, "TestBigBlindName", 5, 1005),
            ]);
}
