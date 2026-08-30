using Contracts.IntegrationEvents.Gameplay;
using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;
using Statistics.Infrastructure.Data;
using Wolverine.EntityFrameworkCore;

namespace Statistics.Features.RebuyRecorded;

public sealed class RebuyRecordedIntegrationEventHandler(IDbContextOutbox<StatisticsDbContext> outbox)
{
    public async Task Handle(RebuyRecordedIntegrationEvent integrationEvent, CancellationToken ct)
    {
        StatisticsDbContext context = outbox.DbContext;

        if (await context.Rebuys.AnyAsync(rebuy => rebuy.EventId == integrationEvent.EventId, ct))
        {
            return;
        }

        context.Rebuys.Add(new Rebuy(
            integrationEvent.EventId,
            integrationEvent.GameId,
            integrationEvent.ParticipantId,
            integrationEvent.Amount,
            integrationEvent.OccurredAt));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
    }
}
