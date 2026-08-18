using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Shared.Domain;
using Wolverine.EntityFrameworkCore;

namespace Gameplay.Features.CreateGame;

internal sealed class CreateGameCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result<CreateGameResponse>> Handle(CreateGameCommand command, CancellationToken ct)
    {
        // TODO: remove later
        // temporary hardcoded values
        ChipsStack smallBlind = ChipsStack.Create(5).Value;
        ChipsStack bigBlind = ChipsStack.Create(10).Value;

        if (!Game.Create(command.HostName, smallBlind, bigBlind).TryGetValue(out Game? game, out Error? error))
        {
            return error;
        }

        outbox.DbContext.Games.Add(game);

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return new CreateGameResponse(game.Id.Value, game.HostParticipantId.Value);
    }
}
