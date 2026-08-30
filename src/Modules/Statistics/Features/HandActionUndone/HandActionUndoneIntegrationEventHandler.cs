using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;
using Statistics.Infrastructure.Data;
using Wolverine.EntityFrameworkCore;

namespace Statistics.Features.HandActionUndone;

public sealed class HandActionUndoneIntegrationEventHandler(IDbContextOutbox<StatisticsDbContext> outbox)
{
    public async Task Handle(HandActionUndoneIntegrationEvent integrationEvent, CancellationToken ct)
    {
        StatisticsDbContext context = outbox.DbContext;

        Hand hand = await context.Hands.SingleOrDefaultAsync(h => h.Id == integrationEvent.HandId, ct)
            ?? throw IngestParentMissingException.ForHand(integrationEvent.HandId);

        if (!hand.TryUndoAction(integrationEvent.SequenceNumber, integrationEvent.OccurredAt))
        {
            throw IngestParentMissingException.ForAction(
                integrationEvent.HandId,
                integrationEvent.SequenceNumber);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
    }
}
