using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;
using Wolverine.EntityFrameworkCore;

namespace Gameplay.Features.StartHand;

internal sealed class StartHandCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result<StartHandResponse>> Handle(StartHandCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games
            .Include(g => g.Participants)
            .SingleOrDefaultAsync(g => g.Id == GameId.From(command.GameId), ct);

        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        if (!game.PrepareNextHand(ParticipantId.From(command.ActingParticipantId))
                .TryGetValue(out IReadOnlyList<HandSeat>? seats, out Error? error))
        {
            return error;
        }

        if (!Hand.Start(GameId.From(command.GameId), seats, game.SmallBlind, game.BigBlind)
                .TryGetValue(out Hand? hand, out error))
        {
            return error;
        }

        outbox.DbContext.Hands.Add(hand);

        if (game.AttachHand(hand.Id).TryGetError(out error))
        {
            return error;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return new StartHandResponse(hand.Id.Value);
    }
}
