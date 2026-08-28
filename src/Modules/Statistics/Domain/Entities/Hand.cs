using Statistics.Domain.ValueObjects;

namespace Statistics.Domain.Entities;

internal sealed class Hand
{
    public Guid Id { get; private init; }
    public Guid GameId { get; private init; }
    public DateTimeOffset StartedAt { get; private init; }
    public DateTimeOffset? FinishedAt { get; private set; }
    public DateTimeOffset? AbortedAt { get; private set; }

    // Navigation Property
    public IReadOnlyList<HandSeat> Seats => _seats.AsReadOnly();

    // Navigation Property
    public IReadOnlyList<HandAction> Actions => _actions.AsReadOnly();

    // Navigation Property
    public IReadOnlyList<HandResult> Results => _results.AsReadOnly();

    // Navigation Property
    public IReadOnlyList<HandPot> Pots => _pots.AsReadOnly();

    private readonly List<HandSeat> _seats;
    private readonly List<HandAction> _actions;
    private readonly List<HandResult> _results;
    private readonly List<HandPot> _pots;

    private Hand(Guid id, Guid gameId, DateTimeOffset startedAt)
    {
        Id = id;
        GameId = gameId;
        StartedAt = startedAt;
        _seats = [];
        _actions = [];
        _results = [];
        _pots = [];
    }

    public static Hand Start(
        Guid id,
        Guid gameId,
        DateTimeOffset startedAt,
        IEnumerable<HandSeat> seats,
        IEnumerable<HandAction> blinds)
    {
        Hand hand = new(id, gameId, startedAt);
        hand._seats.AddRange(seats);
        hand._actions.AddRange(blinds);

        return hand;
    }

    public void RecordAction(HandAction action)
    {
        if (_actions.Any(a => a.EventId == action.EventId && a.SequenceNumber == action.SequenceNumber))
        {
            return;
        }

        _actions.Add(action);
    }

    public bool TryUndoAction(int sequenceNumber, DateTimeOffset undoneAt)
    {
        if (_actions.Any(a => a.SequenceNumber == sequenceNumber && a.UndoneAt == undoneAt))
        {
            return true;
        }

        HandAction? action = _actions.SingleOrDefault(a =>
            a.SequenceNumber == sequenceNumber
            && a.UndoneAt is null
            && a.OccurredAt < undoneAt);

        if (action is null)
        {
            return false;
        }

        action.MarkUndone(undoneAt);

        return true;
    }

    public void Finish(
        DateTimeOffset finishedAt,
        IEnumerable<HandResult> results,
        IEnumerable<HandPot> pots)
    {
        if (FinishedAt is not null)
        {
            return;
        }

        FinishedAt = finishedAt;
        _results.AddRange(results);
        _pots.AddRange(pots);
    }

    public void Abort(DateTimeOffset abortedAt) => AbortedAt ??= abortedAt;
}
