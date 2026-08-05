using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;
using Wolverine.EntityFrameworkCore;

namespace Gameplay.Features.JoinGame;

internal sealed class JoinGameCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result<JoinGameResponse>> Handle(JoinGameCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games
            .SingleOrDefaultAsync(g => g.JoinCode == JoinCode.From(command.JoinCode), ct);

        if (game is null)
        {
            return Result.Failure<JoinGameResponse>(GameErrors.GameNotFound);
        }

        Result<ParticipantId> joinGameResult = game.Join(command.ParticipantName);
        if (joinGameResult.IsFailure)
        {
            return Result.Failure<JoinGameResponse>(joinGameResult.Error);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return new JoinGameResponse(game.Id.Value, joinGameResult.Value.Value);
    }
}
