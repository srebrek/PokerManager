using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;
using Statistics.Infrastructure.Data;
using Domain = Statistics.Domain.ValueObjects;

namespace IntegrationTests.Statistics;

public sealed class HandStartedIngestIntegrationTests(ApiFactory factory) : StatisticsIntegrationTest(factory)
{
    [Fact]
    public async Task HandStarted_ValidEventOccurs_StoresTheHand()
    {
        // Arrange
        Table table = Table.New();

        // Act
        await IngestAsync(HandStarted(table, At(0), table.Seats(1000, 800, 600)));

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
        {
            Hand hand = await context.Hands
                .AsNoTracking()
                .SingleAsync(hand => hand.Id == table.HandId, CancellationToken);

            hand.GameId.ShouldBe(table.GameId);
            hand.StartedAt.ShouldBe(At(0));
            hand.FinishedAt.ShouldBeNull();
            hand.AbortedAt.ShouldBeNull();
            hand.Seats
                .OrderBy(seat => seat.Position)
                .Select(seat => (seat.Position, seat.ParticipantId, seat.StartingChips))
                .ShouldBe([(0, table.SmallBlind, 800), (1, table.BigBlind, 600), (2, table.Host, 1000)]);

            List<HandAction> blinds = [.. hand.Actions.OrderBy(action => action.SequenceNumber)];
            blinds.Count.ShouldBe(2);
            blinds[0].Type.ShouldBe(Domain.HandActionType.PostSmallBlind);
            blinds[0].ParticipantId.ShouldBe(table.SmallBlind);
            blinds[0].AmountTo.ShouldBe(SmallBlindAmount);
            blinds[1].Type.ShouldBe(Domain.HandActionType.PostBigBlind);
            blinds[1].ParticipantId.ShouldBe(table.BigBlind);
            blinds[1].AmountTo.ShouldBe(BigBlindAmount);
            blinds.ShouldAllBe(blind => blind.OccurredAt == At(0) && blind.UndoneAt == null);
        });
    }

    [Fact]
    public async Task HandStarted_TheSameEventIsDeliveredTwice_KeepsASingleHand()
    {
        // Arrange
        Table table = Table.New();
        HandStartedIntegrationEvent handStarted = HandStarted(table, At(0), table.Seats());

        // Act
        await IngestAsync(handStarted, handStarted);

        // Assert
        await ExecuteWithContextAsync<StatisticsDbContext>(async context =>
        {
            var counts = await context.Hands
                .Where(hand => hand.Id == table.HandId)
                .Select(hand => new { SeatCount = hand.Seats.Count, ActionCount = hand.Actions.Count })
                .ToListAsync(CancellationToken);

            counts.Count.ShouldBe(1);
            counts[0].SeatCount.ShouldBe(3);
            counts[0].ActionCount.ShouldBe(2);
        });
    }
}
