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
        if (ValidateStartInput(seats, smallBlind, bigBlind) is { IsFailure: true, Error: var error })
        {
            return Result.Failure<Hand>(error);
        }

        Hand hand = new(HandId.New(), gameId);

        List<HandSeat> orderedSeats = [.. seats.OrderBy(s => s.Position)];
        hand._seats.AddRange(orderedSeats);

        hand._actions.AddRange(
            HandAction.Create(
                0,
                orderedSeats[0].ParticipantId,
                HandActionType.PostSmallBlind,
                ChipsStack.Create(smallBlind.Value).Value),
            HandAction.Create(
                1,
                orderedSeats[1].ParticipantId,
                HandActionType.PostBigBlind,
                ChipsStack.Create(bigBlind.Value).Value)
        );

        return hand;
    }

    private static Result ValidateStartInput(IReadOnlyList<HandSeat> seats, ChipsStack smallBlind, ChipsStack bigBlind)
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

    public Result RecordAction(ParticipantId participantId, HandActionType type, ChipsStack? amountTo)
    {
        HandSeat? seat = _seats.SingleOrDefault(s => s.ParticipantId == participantId);
        if (seat is null)
        {
            return Result.Failure(HandErrors.ParticipantIdNotInSeats);
        }

        Result<HandAction> handActionResult = HandAction.Create(
            _actions[^1].SequenceNumber + 1,
            participantId,
            type,
            amountTo);
        if (handActionResult.IsFailure)
        {
            return Result.Failure(handActionResult.Error);
        }

        _actions.Add(handActionResult.Value);
        Result<HandState> handState = HandStateCalculator.Calculate(_seats, _actions);
        if (handState.IsFailure)
        {
            return Result.Failure(handState.Error);
        }

        return Result.Success();
    }

    public Result<List<HandAward>> Finish(ParticipantId winnerParticipantId)
    {
        if (Status is not HandStatus.InProgress)
        {
            return Result.Failure<List<HandAward>>(HandErrors.NotInProgressHandFinish);
        }

        Status = HandStatus.Finished;

        Result<HandState> handState = HandStateCalculator.Calculate(_seats, _actions);
        if (handState.IsFailure)
        {
            return Result.Failure<List<HandAward>>(handState.Error);
        }

        if (handState.Value.Street is not Street.Finished)
        {
            return Result.Failure<List<HandAward>>(HandErrors.NotFinishedStreetFinish);
        }

        HandSeatState winner =
            handState.Value.SeatStates.SingleOrDefault(ss => ss.ParticipantId == winnerParticipantId);
        if (winner == default)
        {
            return Result.Failure<List<HandAward>>(HandErrors.ParticipantIdNotInSeats);
        }

        if (winner.State is SeatState.Folded)
        {
            return Result.Failure<List<HandAward>>(HandErrors.FoldedWinner);
        }

        List<HandAward> awards = new(handState.Value.SeatStates.Count);
        foreach (HandSeatState seatState in handState.Value.SeatStates)
        {
            ChipsStack startingStack = _seats.Single(s => s.ParticipantId == seatState.ParticipantId).StartingStack;
            int net = seatState.RemainingStack - startingStack;
            if (seatState.ParticipantId == winnerParticipantId)
            {
                awards.Add(new(seatState.ParticipantId, handState.Value.Pot + net));
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

        return awards;
    }

    public Result Abort()
    {
        if (Status is not HandStatus.InProgress)
        {
            return Result.Failure(HandErrors.NotInProgressHandAbort);
        }

        Status = HandStatus.Aborted;
        return Result.Success();
    }
}
