using Contracts.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Shared.Domain;

namespace Gameplay.Features.CreateGame;

public sealed class CreateGameCommandHandler(GameplayDbContext context)
{
    public async Task<Result<CreateGameResponse>> Handle(CreateGameCommand command)
    {
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

        context.Games.Add(game);
        return new CreateGameResponse(game.Id.Value, game.JoinCode.Value);
    }
}
