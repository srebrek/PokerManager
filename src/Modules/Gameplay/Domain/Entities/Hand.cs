using Gameplay.Domain.Events;
using Gameplay.Domain.Services;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Hand : AggregateRoot<HandId>
{
    public GameId GameId { get; }
    public HandStatus Status { get; private set; }

    // Navigation Property
    public IReadOnlyList<HandSeat> Seats => _seats.AsReadOnly();

    // Navigation Property
    public IReadOnlyList<HandAction> Actions => _actions.AsReadOnly();

    private readonly List<HandSeat> _seats;
    private readonly List<HandAction> _actions = [];

    private Hand(HandId id, GameId gameId) : base(id)
    {
        GameId = gameId;
        _seats = [];
        Status = HandStatus.InProgress;
    }

    public static Result<Hand> Start(
        GameId gameId,
        IReadOnlyList<HandSeat> seats,
        ChipsStack smallBlind,
        ChipsStack bigBlind)
    {
        if (ValidateStartInput(seats, smallBlind, bigBlind).TryGetError(out Error? error))
        {
            return error;
        }

        Hand hand = new(HandId.New(), gameId);

        List<HandSeat> orderedSeats = [.. seats.OrderBy(s => s.Position)];
        hand._seats.AddRange(orderedSeats);

        hand._actions.AddRange(
            HandAction.Create(
                0,
                orderedSeats[0].ParticipantId,
                HandActionType.PostSmallBlind,
                smallBlind).Value,
            HandAction.Create(
                1,
                orderedSeats[1].ParticipantId,
                HandActionType.PostBigBlind,
                bigBlind).Value
        );

        hand.Raise(new HandStartedDomainEvent(gameId.Value, hand.Id.Value));

        return hand;
    }

    private static Result ValidateStartInput(IReadOnlyList<HandSeat> seats, ChipsStack smallBlind, ChipsStack bigBlind)
    {
        if (seats.Count < 2)
        {
            return HandErrors.InsufficientParticipantCount;
        }

        if (seats.DistinctBy(s => s.ParticipantId).Count() != seats.Count)
        {
            return HandErrors.DuplicatedParticipants;
        }

        if (bigBlind.Value < smallBlind.Value)
        {
            return HandErrors.BigBlindLessThanSmallBlind;
        }

        return Result.Success();
    }

    public Result RecordAction(ParticipantId participantId, HandActionType type, ChipsStack? amountTo)
    {
        HandSeat? seat = _seats.SingleOrDefault(s => s.ParticipantId == participantId);
        if (seat is null)
        {
            return HandErrors.ParticipantIdNotInSeats;
        }

        if (!HandAction.Create(_actions[^1].SequenceNumber + 1, participantId, type, amountTo)
                .TryGetValue(out HandAction? handAction, out Error? error))
        {
            return error;
        }

        _actions.Add(handAction);

        if (!HandStateCalculator.Calculate(_seats, _actions).TryGetValue(out HandState handState, out error))
        {
            return error;
        }

        HandActionEffect effect = handState.LastActionEffect;

        Raise(new HandActionRecordedDomainEvent(
            Id.Value,
            handAction.SequenceNumber,
            effect.PotDelta,
            (int?)effect.NewStreet,
            new HandActionSeatEffect(
                effect.SeatEffect.ParticipantId.Value,
                effect.SeatEffect.ChipsDelta,
                (int?)effect.SeatEffect.NewState)));

        return Result.Success();
    }

    public Result<List<HandAward>> Finish(ParticipantId winnerParticipantId)
    {
        if (Status is not HandStatus.InProgress)
        {
            return HandErrors.NotInProgressHandFinish;
        }

        Status = HandStatus.Finished;

        if (!HandStateCalculator.Calculate(_seats, _actions).TryGetValue(out HandState handState, out Error? error))
        {
            return error;
        }

        if (handState.Street is not Street.Finished)
        {
            return HandErrors.NotFinishedStreetFinish;
        }

        HandSeatState winner = handState.SeatStates.SingleOrDefault(ss => ss.ParticipantId == winnerParticipantId);
        if (winner == default)
        {
            return HandErrors.ParticipantIdNotInSeats;
        }

        if (winner.State is SeatState.Folded)
        {
            return HandErrors.FoldedWinner;
        }

        List<HandAward> awards = new(handState.SeatStates.Count);
        foreach (HandSeatState seatState in handState.SeatStates)
        {
            ChipsStack startingStack = _seats.Single(s => s.ParticipantId == seatState.ParticipantId).StartingStack;
            int net = seatState.RemainingStack - startingStack;
            if (seatState.ParticipantId == winnerParticipantId)
            {
                awards.Add(new(seatState.ParticipantId, handState.Pot + net));
            }
            else
            {
                awards.Add(new(seatState.ParticipantId, net));
            }
        }

        if (awards.Sum(a => a.Net) is not 0)
        {
            throw new InvalidOperationException("Awards does not sum up to zero.");
        }

        Raise(new HandFinishedDomainEvent(GameId.Value, Id.Value));

        return awards;
    }

    public Result Abort()
    {
        if (Status is not HandStatus.InProgress)
        {
            return HandErrors.NotInProgressHandAbort;
        }

        Status = HandStatus.Aborted;

        Raise(new HandAbortedDomainEvent(GameId.Value, Id.Value));

        return Result.Success();
    }
}
