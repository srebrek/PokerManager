using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;

namespace Gameplay.Features.JoinGame;

public sealed class JoinGameCommandHandler(GameplayDbContext context)
{
    public async Task<Result<JoinGameResponse>> Handle(JoinGameCommand command)
    {
        // TODO: remove later
        // temporary hardcoded values
        ChipsStack participantChips = ChipsStack.Create(1000).Value;

        Game? game = await context.Games.SingleOrDefaultAsync(g => g.JoinCode == JoinCode.From(command.JoinCode));

        if (game is null)
        {
            return Result.Failure<JoinGameResponse>(GameErrors.GameNotFound);
        }

        Result<ParticipantId> joinGameResult = game.Join(command.ParticipantName, participantChips);
        if (joinGameResult.IsFailure)
        {
            return Result.Failure<JoinGameResponse>(joinGameResult.Error);
        }

        return new JoinGameResponse(game.Id.Value, joinGameResult.Value.Value);
    }
}
