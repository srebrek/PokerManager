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
                h.Seats,
                Actions = h.Actions.OrderBy(a => a.SequenceNumber).ToList(),
                h.PotWinners,
                h.Status,
            })
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

        if (hand is null)
        {
            return HandErrors.HandNotFound;
        }

        if (!HandStateCalculator.Calculate(hand.Seats, hand.Actions)
                .TryGetValue(out HandState handState, out Error? error))
        {
            return error;
        }

        List<HandPotState> pots = [.. handState.Pots.Select(pot => pot.ToContract(
            [.. hand.PotWinners.Where(pw => pw.PotIndex == pot.Index).Select(pw => pw.ParticipantId)]))];

        List<HandStateSeat> handStateSeats = [.. handState.SeatStates.Select(ss => new HandStateSeat(
            ss.ParticipantId.Value,
            ss.StreetContribution,
            ss.RemainingStack,
            ss.State.ToContract()))];

        return new GetHandStateResponse(
            query.HandId,
            hand.Status.ToContract(),
            handState.Street.ToContract(),
            pots,
            handStateSeats,
            hand.Actions[^1].SequenceNumber);
    }
}
