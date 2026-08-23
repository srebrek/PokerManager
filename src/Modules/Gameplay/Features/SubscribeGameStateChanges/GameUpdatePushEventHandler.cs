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

    public Task Handle(RebuyRecordedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(SeatingOrderChangedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(ParticipantSittingOutChangedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(DealerButtonMovedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(GameRulesChangedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    public Task Handle(GameFinishedDomainEvent e, CancellationToken ct) => PushAsync(e, ct);

    private Task PushAsync(IGameActivityDomainEvent domainEvent, CancellationToken ct) =>
        hubContext.Clients
            .Group(GameHub.GroupName(domainEvent.GameId))
            .SendAsync(GameplayRoutes.HubGameUpdatedMethod, domainEvent.GameId, ct);
}

// TODO: move to the separate files
public sealed class HandUpdatePushEventHandler(IHubContext<GameHub> hubContext)
{
    public Task Handle(HandWinnersDeclaredDomainEvent e, CancellationToken ct) =>
        hubContext.Clients
            .Group(GameHub.HandGroupName(e.HandId))
            .SendAsync(GameplayRoutes.HubHandUpdatedMethod, e.HandId, ct);
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
                    e.Pots,
                    e.NewStreet,
                    new(e.Seat.ParticipantId, e.Seat.ChipsDelta, e.Seat.NewState)),
                ct);
}
