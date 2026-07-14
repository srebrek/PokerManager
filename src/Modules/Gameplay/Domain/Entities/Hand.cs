using Gameplay.Domain.Services;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Hand : AggregateRoot<HandId>
{
    private readonly List<HandSeat> _seats;
    private readonly List<HandAction> _actions = [];

    public GameId GameId { get; }
    public IReadOnlyList<HandSeat> Seats => _seats.AsReadOnly();
    public IReadOnlyList<HandAction> Actions => _actions.AsReadOnly();
    public Street Street { get; private set; } = Street.PreFlop;
    public HandStatus Status { get; private set; }

    private Hand(HandId id, GameId gameId) : base(id)
    {
        GameId = gameId;
        _seats = [];
        Status = HandStatus.InProgress;
    }

    public static Result<Hand> Start(GameId gameId, List<HandSeat> seats, ChipsStack smallBlind, ChipsStack bigBlind)
    {
        if (ValidateStartInput(seats, smallBlind, bigBlind) is { IsFailure: true, Error: var error })
        {
            return Result.Failure<Hand>(error);
        }

        Hand hand = new(HandId.New(), gameId);

        List<HandSeat> orderedSeats = [.. seats.OrderBy(s => s.Position)];
        hand._seats.AddRange(orderedSeats);

        hand._actions.AddRange(
            new HandAction(
                0,
                orderedSeats[0].ParticipantId,
                HandActionType.PostSmallBlind,
                ChipsStack.Create(smallBlind.Value).Value,
                Street.PreFlop),
            new HandAction(
                1,
                orderedSeats[1].ParticipantId,
                HandActionType.PostBigBlind,
                ChipsStack.Create(bigBlind.Value).Value,
                Street.PreFlop)
        );

        return hand;
    }

    private static Result ValidateStartInput(List<HandSeat> seats, ChipsStack smallBlind, ChipsStack bigBlind)
    {
        if (seats.Count < 2)
        {
            return Result.Failure(HandErrors.InsufficientParticipantCount);
        }

        if (seats.DistinctBy(s => s.ParticipantId).Count() != seats.Count)
        {
            return Result.Failure(HandErrors.DuplicatedParticipants);
        }

        if (bigBlind.Value < smallBlind.Value)
        {
            return Result.Failure(HandErrors.BigBlindLessThanSmallBlind);
        }

        return Result.Success();
    }

    public Result RecordAction(ParticipantId participantId, HandActionType type, ChipsStack? amount)
    {
        HandState handState = HandStateCalculator.Calculate(_seats, _actions);

        if (ValidateRecordActionInput(participantId, handState) is { IsFailure: true, Error: var error })
        {
            return Result.Failure(error);
        }

        if (amount is not null && handState.RemainingStacks[participantId].Value < amount.Value.Value)
        {
            return Result.Failure(HandErrors.InsufficientChipsStack);
        }

        int sequenceNumber = _actions.Count;
        _actions.Add(new HandAction(sequenceNumber, participantId, type, amount, Street));

        return Result.Success();
    }

    private Result ValidateRecordActionInput(ParticipantId participantId, HandState handState)
    {
        HandSeat? seat = _seats.SingleOrDefault(s => s.ParticipantId == participantId);

        if (seat is null)
        {
            return Result.Failure(HandErrors.ParticipantIdNotInSeats);
        }

        SeatState seatState = handState.SeatStates[participantId];
        if (seatState is SeatState.Folded)
        {
            return Result.Failure(HandErrors.SeatFolded);
        }

        if (seatState is SeatState.AllIn)
        {
            return Result.Failure(HandErrors.SeatAllIned);
        }

        return Result.Success();
    }

    public Result Undo()
    {
        if (Status is HandStatus.Completed)
        {
            return Result.Failure(HandErrors.UndoCompletedHand);
        }

        int actionCount = _actions.Count;
        if (actionCount <= 2)
        {
            return Result.Failure(HandErrors.UndoBlind);
        }

        _actions.RemoveAt(actionCount - 1);

        return Result.Success();
    }

    public Result AdvanceStreet()
    {
        if (Status is HandStatus.Completed)
        {
            return Result.Failure(HandErrors.AdvanceStreetOnCompletedHand);
        }

        if (Street is Street.River)
        {
            return Result.Failure(HandErrors.NoNextStreet);
        }

        Street = (Street)((int)Street + 1);

        return Result.Success();
    }

    public Result Complete()
    {
        if (Status is HandStatus.Completed)
        {
            return Result.Failure(HandErrors.CompleteCompletedHand);
        }

        Status = HandStatus.Completed;

        return Result.Success();
    }
}
