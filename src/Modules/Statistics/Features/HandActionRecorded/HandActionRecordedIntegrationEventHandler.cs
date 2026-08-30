using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;
using Statistics.Domain.ValueObjects;
using Statistics.Infrastructure.Data;
using Wolverine.EntityFrameworkCore;

namespace Statistics.Features.HandActionRecorded;

public sealed class HandActionRecordedIntegrationEventHandler(IDbContextOutbox<StatisticsDbContext> outbox)
{
    public async Task Handle(HandActionRecordedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        StatisticsDbContext context = outbox.DbContext;

        Hand hand = await context.Hands.SingleOrDefaultAsync(h => h.Id == integrationEvent.HandId, ct)
            ?? throw IngestParentMissingException.ForHand(integrationEvent.HandId);

        hand.RecordAction(new HandAction(
            integrationEvent.EventId,
            integrationEvent.SequenceNumber,
            integrationEvent.ParticipantId,
            integrationEvent.Type.ToDomain(),
            integrationEvent.AmountTo,
            integrationEvent.OccurredAt));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
    }
}
