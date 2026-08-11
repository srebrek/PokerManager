using Contracts.Api.Gameplay;
using FluentValidation;
using Gameplay.Domain.Entities;
using Gameplay.Domain.Services;
using Gameplay.Domain.ValueObjects;
using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;

namespace Gameplay.Features.GetHandState;

internal sealed class GetHandStateQueryHandler(GameplayDbContext db)
{
    public async Task<Result<GetHandStateResponse>> Handle(GetHandStateQuery query, CancellationToken ct)
    {
        var hand = await db.Hands
            .Where(h => h.Id == HandId.From(query.HandId))
            .Select(h => new
            {
                h.GameId,
                h.Seats,
                Actions = h.Actions.OrderBy(a => a.SequenceNumber).ToList(),
                h.Status,
            })
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

        if (hand is null)
        {
            return Result.Failure<GetHandStateResponse>(HandErrors.HandNotFound);
        }

        Result<HandState> handStateResult = HandStateCalculator.Calculate(hand.Seats, hand.Actions);
        if (handStateResult.IsFailure)
        {
            return Result.Failure<GetHandStateResponse>(handStateResult.Error);
        }

        // TODO: consider better enum mapping
        List<HandStateSeat> handStateSeats = [.. handStateResult.Value.SeatStates.Select(ss => new HandStateSeat(
            ss.ParticipantId.Value,
            ss.StreetContribution,
            ss.RemainingStack,
            (Contracts.Api.Gameplay.SeatState)ss.State))];

        return new GetHandStateResponse(
            query.HandId,
            hand.GameId.Value,
            (Contracts.Api.Gameplay.HandStatus)hand.Status,
            (Contracts.Api.Gameplay.Street)handStateResult.Value.Street,
            handStateResult.Value.Pot.Value,
            handStateSeats);
    }
}
