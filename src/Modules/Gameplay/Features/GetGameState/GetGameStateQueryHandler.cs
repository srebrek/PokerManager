using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;

namespace Gameplay.Features.GetGameState;

internal sealed class GetGameStateQueryHandler(GameplayDbContext db)
{
    public async Task<Result<GameStateResponse>> Handle(GetGameStateQuery query, CancellationToken ct)
    {
        var game = await db.Games
            .Where(g => g.Id == GameId.From(query.GameId))
            .Select(g => new
            {
                g.JoinCode,
                g.IsFinished,
                g.SmallBlind,
                g.BigBlind,
                g.HostParticipantId,
                g.DealerButtonParticipantId,
                g.Participants,
                g.SeatingOrder,
                g.CurrentHandId
            })
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

        if (game is null)
        {
            return GameErrors.GameNotFound;
        }

        Dictionary<ParticipantId, Participant> participantsById = game.Participants.ToDictionary(p => p.Id);

        List<GameStateParticipant> participants = [.. game.SeatingOrder.Select(id =>
        {
            Participant participant = participantsById[id];
            return new GameStateParticipant(
                id.Value,
                participant.Name,
                participant.Chips.Value,
                id == game.HostParticipantId,
                participant.IsSittingOut,
                id == game.DealerButtonParticipantId);
        })];

        return new GameStateResponse(
            query.GameId,
            game.JoinCode.Value,
            game.IsFinished,
            game.SmallBlind.Value,
            game.BigBlind.Value,
            participants,
            game.CurrentHandId?.Value);
    }
}
