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
        ChipsStack hostChips = ChipsStack.Create(1000).Value;
        ChipsStack smallBlind = ChipsStack.Create(5).Value;
        ChipsStack bigBlind = ChipsStack.Create(10).Value;

        Result<Game> createGameResult = Game.Create(command.HostName, hostChips, smallBlind, bigBlind);
        if (createGameResult.IsFailure)
        {
            return Result.Failure<CreateGameResponse>(createGameResult.Error);
        }

        Game game = createGameResult.Value;

        outbox.DbContext.Games.Add(game);

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return new CreateGameResponse(game.Id.Value, game.HostParticipantId.Value);
    }
}
