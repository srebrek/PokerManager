using Contracts.Api.Gameplay;
using FluentValidation;
using Gameplay.Domain.Entities;
using Gameplay.Domain.Services;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;

namespace Gameplay.Features.HandState;

internal sealed class HandStateQueryHandler(GameplayDbContext db)
{
    public async Task<Result<HandStateResponse>> Handle(HandStateQuery query, CancellationToken ct)
    {
        var hand = await db.Hands
            .Where(h => h.Id == HandId.From(query.HandId))
            .Select(h => new
            {
                h.GameId,
                h.Seats,
                h.Actions,
                h.Status,
                h.Street
            })
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

        if (hand is null)
        {
            return Result.Failure<HandStateResponse>(HandErrors.HandNotFound);
        }

        Domain.Services.HandState handState = HandStateCalculator.Calculate(hand.Seats, hand.Actions);

        // TODO: consider better enum mapping
        List<HandStateSeat> handStateSeats = [.. hand.Seats.Select(handSeat => new HandStateSeat(
            handSeat.ParticipantId.Value,
            handState.RemainingStacks[handSeat.ParticipantId].Value,
            handState.Contributions[handSeat.ParticipantId].Value,
            (Contracts.Api.Gameplay.SeatState)handState.SeatStates[handSeat.ParticipantId]))];

        return new HandStateResponse(
            query.HandId,
            hand.GameId.Value,
            (Contracts.Api.Gameplay.HandStatus)hand.Status,
            (Contracts.Api.Gameplay.Street)hand.Street,
            handState.Pot.Value,
            handStateSeats);
    }
}
