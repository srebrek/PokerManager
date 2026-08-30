using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;
using Statistics.Domain.ValueObjects;
using Statistics.Infrastructure.Data;
using Wolverine.EntityFrameworkCore;

namespace Statistics.Features.HandFinished;

public sealed class HandFinishedIntegrationEventHandler(IDbContextOutbox<StatisticsDbContext> outbox)
{
    public async Task Handle(HandFinishedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        StatisticsDbContext context = outbox.DbContext;

        Hand hand = await context.Hands.SingleOrDefaultAsync(h => h.Id == integrationEvent.HandId, ct)
            ?? throw IngestParentMissingException.ForHand(integrationEvent.HandId);

        hand.Finish(
            integrationEvent.OccurredAt,
            integrationEvent.Results.Select(result => new HandResult(
                result.ParticipantId,
                result.Name,
                result.Net,
                result.EndingChips)),
            integrationEvent.Pots.Select(pot => HandPot.Create(
                pot.Index,
                pot.Amount,
                integrationEvent.PotWinners
                    .Where(winner => winner.PotIndex == pot.Index)
                    .Select(winner => winner.ParticipantId))));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
    }
}
