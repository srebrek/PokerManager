using Contracts.Api.Gameplay;
using Gameplay.Domain.Events;
using Microsoft.AspNetCore.SignalR;

namespace Gameplay.Features.SubscribeGameStateChanges;

public sealed class GameUpdatePushEventHandler(IHubContext<GameHub> hubContext)
{
    public Task Handle(ParticipantAddedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(HandAttachedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(HandDetachedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(HandAwardsAppliedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(GameFinishedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(HandStartedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(HandFinishedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(HandAbortedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    private Task PushAsync(IGameActivityDomainEvent domainEvent, CancellationToken ct) =>
        hubContext.Clients
            .Group(GameHub.GroupName(domainEvent.GameId))
            .SendAsync(GameplayRoutes.HubGameUpdatedMethod, domainEvent.GameId, ct);
}

public sealed class RecordActionEffectPushEventHandler(IHubContext<GameHub> hubContext)
{
    public Task Handle(HandActionRecordedDomainEvent e, CancellationToken ct) =>
        hubContext.Clients
            .Group(GameHub.HandGroupName(e.HandId))
            .SendAsync(
                GameplayRoutes.HubApplyHandActionEffectMethod,
                new HandActionEffect(
                    e.ActionNumber,
                    e.PotDelta,
                    e.NewStreet,
                    new(e.Seat.ParticipantId, e.Seat.ChipsDelta, e.Seat.NewState)),
                ct);
}
