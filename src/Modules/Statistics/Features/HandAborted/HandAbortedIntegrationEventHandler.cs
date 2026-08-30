using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;
using Statistics.Infrastructure.Data;
using Wolverine.EntityFrameworkCore;

namespace Statistics.Features.HandAborted;

public sealed class HandAbortedIntegrationEventHandler(IDbContextOutbox<StatisticsDbContext> outbox)
{
    public async Task Handle(HandAbortedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        StatisticsDbContext context = outbox.DbContext;

        Hand hand = await context.Hands.SingleOrDefaultAsync(h => h.Id == integrationEvent.HandId, ct)
            ?? throw IngestParentMissingException.ForHand(integrationEvent.HandId);

        hand.Abort(integrationEvent.OccurredAt);

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
    }
}
