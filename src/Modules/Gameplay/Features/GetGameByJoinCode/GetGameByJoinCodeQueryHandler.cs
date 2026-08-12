using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;

namespace Gameplay.Features.GetGameByJoinCode;

internal sealed class GetGameByJoinCodeQueryHandler(GameplayDbContext db)
{
    public async Task<Result<GameLookupResponse>> Handle(GetGameByJoinCodeQuery query, CancellationToken ct)
    {
        GameId gameId = await db.Games
            .AsNoTracking()
            .Where(g => g.JoinCode == JoinCode.From(query.JoinCode))
            .Select(g => g.Id)
            .SingleOrDefaultAsync(ct);

        return gameId == default
            ? Result.Failure<GameLookupResponse>(GameErrors.GameNotFound)
            : new GameLookupResponse(gameId.Value);
    }
}
