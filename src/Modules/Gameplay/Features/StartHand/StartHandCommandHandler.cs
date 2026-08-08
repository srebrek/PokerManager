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
            return Result.Failure<StartHandResponse>(GameErrors.GameNotFound);
        }

        Result<IReadOnlyList<HandSeat>> prepareNextHandResult =
            game.PrepareNextHand(ParticipantId.From(command.ActingParticipantId));

        if (prepareNextHandResult.IsFailure)
        {
            return Result.Failure<StartHandResponse>(prepareNextHandResult.Error);
        }

        Result<Hand> startHandResult = Hand.Start(
            GameId.From(command.GameId),
            prepareNextHandResult.Value,
            game.SmallBlind,
            game.BigBlind);

        if (startHandResult.IsFailure)
        {
            return Result.Failure<StartHandResponse>(startHandResult.Error);
        }

        Hand hand = startHandResult.Value;
        outbox.DbContext.Hands.Add(hand);

        Result attachHandResult = game.AttachHand(hand.Id);

        if (attachHandResult.IsFailure)
        {
            return Result.Failure<StartHandResponse>(attachHandResult.Error);
        }

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return new StartHandResponse(hand.Id.Value);
    }
}
