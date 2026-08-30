using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;
using Statistics.Domain.ValueObjects;
using Statistics.Infrastructure.Data;
using Wolverine.EntityFrameworkCore;

namespace Statistics.Features.HandStarted;

public sealed class HandStartedIntegrationEventHandler(IDbContextOutbox<StatisticsDbContext> outbox)
{
    public async Task Handle(HandStartedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        StatisticsDbContext context = outbox.DbContext;

        if (await context.Hands.AnyAsync(hand => hand.Id == integrationEvent.HandId, ct))
        {
            return;
        }

        context.Hands.Add(Hand.Start(
            integrationEvent.HandId,
            integrationEvent.GameId,
            integrationEvent.OccurredAt,
            integrationEvent.Seats.Select(seat => new HandSeat(
                seat.ParticipantId,
                seat.Position,
                seat.StartingChips)),
            integrationEvent.Blinds.Select(blind => new HandAction(
                integrationEvent.EventId,
                blind.SequenceNumber,
                blind.ParticipantId,
                blind.Type.ToDomain(),
                blind.AmountTo,
                integrationEvent.OccurredAt))));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
    }
}
