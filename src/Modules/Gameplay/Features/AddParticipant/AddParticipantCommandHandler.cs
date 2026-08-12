using Contracts.Api.Gameplay;
using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;
using Wolverine.EntityFrameworkCore;

namespace Gameplay.Features.AddParticipant;

internal sealed class AddParticipantCommandHandler(IDbContextOutbox<GameplayDbContext> outbox)
{
    public async Task<Result<AddParticipantResponse>> Handle(AddParticipantCommand command, CancellationToken ct)
    {
        Game? game = await outbox.DbContext.Games
            .SingleOrDefaultAsync(g => g.Id == GameId.From(command.GameId), ct);

        if (game is null)
        {
            return Result.Failure<AddParticipantResponse>(GameErrors.GameNotFound);
        }

        Result<ParticipantId> addParticipantResult = game.AddParticipant(command.Name);
        if (addParticipantResult.IsFailure)
        {
            return Result.Failure<AddParticipantResponse>(addParticipantResult.Error);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return new AddParticipantResponse(addParticipantResult.Value.Value);
    }
}
