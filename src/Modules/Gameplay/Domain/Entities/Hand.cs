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

    // Navigation Property
    public IReadOnlyList<HandPotWinner> PotWinners => _potWinners.AsReadOnly();

    private readonly List<HandSeat> _seats;
    private readonly List<HandAction> _actions = [];
    private readonly List<HandPotWinner> _potWinners = [];

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
            [.. handState.Pots.Select(pot => pot.ToContract([]))],
            effect.NewStreet?.ToContract(),
            new HandActionSeatEffect(
                effect.SeatEffect.ParticipantId.Value,
                effect.SeatEffect.ChipsDelta,
                effect.SeatEffect.NewState?.ToContract())));

        return Result.Success();
    }

    public Result DeclareWinners(IReadOnlyList<HandPotWinner> potWinners)
    {
        if (Status is not HandStatus.InProgress)
        {
            return HandErrors.NotInProgressWinnersDeclaration;
        }

        if (!HandStateCalculator.Calculate(_seats, _actions).TryGetValue(out HandState handState, out Error? error))
        {
            return error;
        }

        if (handState.Street is not Street.Finished)
        {
            return HandErrors.NotFinishedStreetWinnersDeclaration;
        }

        if (ValidatePotWinners(handState.Pots, potWinners).TryGetError(out error))
        {
            return error;
        }

        _potWinners.Clear();
        _potWinners.AddRange(potWinners);

        Raise(new HandWinnersDeclaredDomainEvent(Id.Value));

        return Result.Success();
    }

    public Result<List<HandAward>> Finish()
    {
        if (Status is not HandStatus.InProgress)
        {
            return HandErrors.NotInProgressHandFinish;
        }

        if (!HandStateCalculator.Calculate(_seats, _actions).TryGetValue(out HandState handState, out Error? error))
        {
            return error;
        }

        if (handState.Street is not Street.Finished)
        {
            return HandErrors.NotFinishedStreetFinish;
        }

        if (ValidatePotWinners(handState.Pots, _potWinners).TryGetError(out error))
        {
            return error;
        }

        Status = HandStatus.Finished;

        Dictionary<ParticipantId, int> winningsByParticipantId =
            handState.SeatStates.ToDictionary(ss => ss.ParticipantId, _ => 0);

        foreach (HandPot pot in handState.Pots)
        {
            List<ParticipantId> claimed = [.. _potWinners
                .Where(pw => pw.PotIndex == pot.Index)
                .Select(pw => pw.ParticipantId)];

            List<ParticipantId> ordered = [.. _seats.Select(s => s.ParticipantId).Where(claimed.Contains)];
            int share = pot.Amount / ordered.Count;
            int oddChips = pot.Amount % ordered.Count;

            foreach ((int position, ParticipantId winner) in ordered.Index())
            {
                winningsByParticipantId[winner] += share + (position < oddChips ? 1 : 0);
            }
        }

        List<HandAward> awards = [.. handState.SeatStates.Select(ss => new HandAward(
            ss.ParticipantId,
            winningsByParticipantId[ss.ParticipantId] - ss.TotalContribution))];

        if (awards.Sum(a => a.Net) is not 0)
        {
            throw new InvalidOperationException("Awards does not sum up to zero.");
        }

        return awards;
    }

    private static Result ValidatePotWinners(
        IReadOnlyList<HandPot> pots,
        IReadOnlyList<HandPotWinner> potWinners)
    {
        if (potWinners.Count is 0)
        {
            return HandErrors.WinnersNotDeclared;
        }

        if (potWinners.Distinct().Count() != potWinners.Count)
        {
            return HandErrors.DuplicatedPotWinners;
        }

        HashSet<int> claimedPotIndexes = [.. potWinners.Select(pw => pw.PotIndex)];
        if (claimedPotIndexes.Count != pots.Count
            || claimedPotIndexes.Any(index => index < 0 || index >= pots.Count))
        {
            return HandErrors.PotWinnersMismatch;
        }

        if (potWinners.Any(pw => !pots[pw.PotIndex].EligibleParticipantIds.Contains(pw.ParticipantId)))
        {
            return HandErrors.IneligibleWinner;
        }

        return Result.Success();
    }

    public Result Abort()
    {
        if (Status is not HandStatus.InProgress)
        {
            return HandErrors.NotInProgressHandAbort;
        }

        Status = HandStatus.Aborted;

        return Result.Success();
    }
}
