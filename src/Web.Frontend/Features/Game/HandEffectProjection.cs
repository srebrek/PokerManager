using Contracts.Api.Gameplay;

namespace Web.Frontend.Features.Game;

internal static class HandEffectProjection
{
    internal static GetHandStateResponse Apply(GetHandStateResponse hand, HandActionEffect effect)
    {
        bool streetRolledOver = effect.NewStreet is not null;

        return hand with
        {
            Pots = effect.Pots,
            Street = effect.NewStreet ?? hand.Street,
            LastActionNumber = effect.ActionNumber,
            Seats = [.. hand.Seats.Select(seat => ApplyToSeat(seat, effect, streetRolledOver))],
        };
    }

    private static HandStateSeat ApplyToSeat(HandStateSeat seat, HandActionEffect effect, bool streetRolledOver)
    {
        if (seat.ParticipantId != effect.Seat.ParticipantId)
        {
            return streetRolledOver ? seat with { StreetContribution = 0 } : seat;
        }

        return seat with
        {
            StreetContribution = streetRolledOver ? 0 : seat.StreetContribution - effect.Seat.ChipsDelta,
            RemainingStack = seat.RemainingStack + effect.Seat.ChipsDelta,
            State = effect.Seat.NewState ?? seat.State,
        };
    }
}
