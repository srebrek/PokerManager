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
            return GameErrors.GameNotFound;
        }

        if (!game.AddParticipant(command.Name).TryGetValue(out ParticipantId participantId, out Error? error))
        {
            return error;
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);
        return new AddParticipantResponse(participantId.Value);
    }
}
