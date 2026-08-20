using Gameplay.Domain.Events;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Game : AggregateRoot<GameId>
{
    public const int MinimumParticipantsToStartAHand = 3;

    public JoinCode JoinCode { get; }
    public ParticipantId HostParticipantId { get; }
    public ParticipantId DealerButtonParticipantId { get; private set; }
    public ChipsStack SmallBlind { get; }
    public ChipsStack BigBlind { get; }
    public bool IsFinished { get; private set; }
    public IReadOnlyList<ParticipantId> SeatingOrder => _seatingOrder.AsReadOnly();
    public HandId? CurrentHandId { get; private set; }

    // Navigation Property
    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();

    private readonly List<ParticipantId> _seatingOrder;
    private readonly List<Participant> _participants;

    private Game(
        GameId id,
        JoinCode joinCode,
        ParticipantId hostParticipantId,
        ChipsStack smallBlind,
        ChipsStack bigBlind)
        : base(id)
    {
        JoinCode = joinCode;
        HostParticipantId = hostParticipantId;
        DealerButtonParticipantId = hostParticipantId;
        SmallBlind = smallBlind;
        BigBlind = bigBlind;
        IsFinished = false;
        _seatingOrder = [];
        _participants = [];
    }

    public static Result<Game> Create(string hostName, ChipsStack smallBlind, ChipsStack bigBlind)
    {
        if (bigBlind.Value < smallBlind.Value)
        {
            return GameErrors.BigBlindLessThanSmallBlind;
        }

        if (!Participant.Create(hostName)
                .TryGetValue(out Participant? host, out Error? error))
        {
            return error;
        }

        Game game = new(GameId.New(), JoinCode.Generate(), host.Id, smallBlind, bigBlind);
        game._participants.Add(host);
        game._seatingOrder.Add(host.Id);

        return game;
    }

    public Result<ParticipantId> AddParticipant(string participantName)
    {
        if (!Participant.Create(participantName)
                .TryGetValue(out Participant? participant, out Error? error))
        {
            return error;
        }

        _participants.Add(participant);
        _seatingOrder.Add(participant.Id);

        Raise(new ParticipantAddedDomainEvent(Id.Value, participant.Id.Value));

        return participant.Id;
    }

    public Result Rebuy(ParticipantId actingParticipantId, ParticipantId targetParticipantId, int amount)
    {
        if (actingParticipantId != HostParticipantId)
        {
            return GameErrors.NotHost;
        }

        if (IsFinished)
        {
            return GameErrors.GameFinished;
        }

        Participant? participant = _participants.SingleOrDefault(p => p.Id == targetParticipantId);
        if (participant is null)
        {
            return GameErrors.ParticipantNotFound;
        }

        if (participant.Rebuy(amount).TryGetError(out Error? error))
        {
            return error;
        }

        Raise(new RebuyRecordedDomainEvent(Id.Value, targetParticipantId.Value));

        return Result.Success();
    }

    public Result MoveParticipantDown(ParticipantId actingParticipantId, ParticipantId targetParticipantId)
    {
        if (actingParticipantId != HostParticipantId)
        {
            return GameErrors.NotHost;
        }

        if (IsFinished)
        {
            return GameErrors.GameFinished;
        }

        if (CurrentHandId is not null)
        {
            return GameErrors.HandIsRunning;
        }

        int index = _seatingOrder.IndexOf(targetParticipantId);
        if (index < 0)
        {
            return GameErrors.ParticipantNotFound;
        }

        int below = (index + 1) % _seatingOrder.Count;
        (_seatingOrder[index], _seatingOrder[below]) = (_seatingOrder[below], _seatingOrder[index]);

        Raise(new SeatingOrderChangedDomainEvent(Id.Value, targetParticipantId.Value));

        return Result.Success();
    }

    public Result SetParticipantSittingOut(
        ParticipantId actingParticipantId,
        ParticipantId targetParticipantId,
        bool isSittingOut)
    {
        if (actingParticipantId != HostParticipantId)
        {
            return GameErrors.NotHost;
        }

        if (IsFinished)
        {
            return GameErrors.GameFinished;
        }

        if (CurrentHandId is not null)
        {
            return GameErrors.HandIsRunning;
        }

        Participant? participant = _participants.SingleOrDefault(p => p.Id == targetParticipantId);
        if (participant is null)
        {
            return GameErrors.ParticipantNotFound;
        }

        if (isSittingOut && targetParticipantId == DealerButtonParticipantId)
        {
            return GameErrors.DealerCannotSitOut;
        }

        if (participant.SetSittingOut(isSittingOut).TryGetError(out Error? error))
        {
            return error;
        }

        Raise(new ParticipantSittingOutChangedDomainEvent(Id.Value, targetParticipantId.Value));

        return Result.Success();
    }

    public Result<IReadOnlyList<HandSeat>> PrepareNextHand(ParticipantId actingParticipantId)
    {
        if (ValidatePrepareNextHandInput(actingParticipantId).TryGetError(out Error? error))
        {
            return error;
        }

        return BuildSeats();
    }

    private Result ValidatePrepareNextHandInput(ParticipantId actingParticipantId)
    {
        if (actingParticipantId != HostParticipantId)
        {
            return GameErrors.NotHost;
        }

        if (IsFinished)
        {
            return GameErrors.GameFinished;
        }

        if (CurrentHandId is not null)
        {
            return GameErrors.HandIsRunning;
        }

        if (_participants.Count(p => !p.IsSittingOut) < MinimumParticipantsToStartAHand)
        {
            return GameErrors.NotEnoughParticipants;
        }

        if (_participants.Any(p => !p.IsSittingOut && p.Chips.Value < BigBlind.Value))
        {
            return GameErrors.InsufficientChipsForBigBlind;
        }

        return Result.Success();
    }

    private List<HandSeat> BuildSeats()
    {
        Dictionary<ParticipantId, Participant> participantsById = _participants.ToDictionary(p => p.Id);
        List<Participant> seated = [.. _seatingOrder
            .Select(id => participantsById[id])
            .Where(p => !p.IsSittingOut)];
        int count = seated.Count;
        int dealerIndex = seated.FindIndex(p => p.Id == DealerButtonParticipantId);

        List<HandSeat> seats = new(count);
        for (int offset = 0; offset < count; offset++)
        {
            Participant participant = seated[(dealerIndex + 1 + offset) % count];
            seats.Add(new HandSeat(participant.Id, participant.Chips, offset));
        }

        return seats;
    }

    public Result AttachHand(HandId handId)
    {
        if (CurrentHandId is not null)
        {
            return GameErrors.HandIsRunning;
        }

        CurrentHandId = handId;

        Raise(new HandAttachedDomainEvent(Id.Value, handId.Value));

        return Result.Success();
    }

    public Result DetachHand(ParticipantId actingParticipantId, HandId handId)
    {
        if (HostParticipantId != actingParticipantId)
        {
            return GameErrors.NotHost;
        }

        if (CurrentHandId is null)
        {
            return GameErrors.NoCurrentHand;
        }

        if (CurrentHandId != handId)
        {
            return GameErrors.InvalidHandId;
        }

        CurrentHandId = null;

        Raise(new HandDetachedDomainEvent(Id.Value, handId.Value));

        return Result.Success();
    }

    public Result ApplyHandAwards(IReadOnlyList<HandAward> handAwards, ParticipantId actingParticipant, HandId handId)
    {
        if (HostParticipantId != actingParticipant)
        {
            return GameErrors.NotHost;
        }

        if (CurrentHandId is null)
        {
            return GameErrors.NoCurrentHand;
        }

        if (CurrentHandId != handId)
        {
            return GameErrors.InvalidHandId;
        }

        CurrentHandId = null;

        foreach (HandAward award in handAwards)
        {
            Participant participant = Participants.Single(p => p.Id == award.ParticipantId);
            if (participant.AddChips(award.Net).TryGetError(out Error? error))
            {
                return error;
            }
        }

        AdvanceDealerButton();

        Raise(new HandAwardsAppliedDomainEvent(Id.Value, handId.Value));

        return Result.Success();
    }

    private void AdvanceDealerButton()
    {
        int dealerIndex = _seatingOrder.IndexOf(DealerButtonParticipantId);

        for (int offset = 1; offset < _seatingOrder.Count; offset++)
        {
            ParticipantId candidate = _seatingOrder[(dealerIndex + offset) % _seatingOrder.Count];
            if (!_participants.Single(p => p.Id == candidate).IsSittingOut)
            {
                DealerButtonParticipantId = candidate;
                return;
            }
        }
    }

    public Result Finish(ParticipantId actingParticipantId)
    {
        if (HostParticipantId != actingParticipantId)
        {
            return GameErrors.NotHost;
        }

        if (IsFinished)
        {
            return GameErrors.GameFinished;
        }

        if (CurrentHandId is not null)
        {
            return GameErrors.HandIsRunning;
        }

        IsFinished = true;

        Raise(new GameFinishedDomainEvent(Id.Value));

        return Result.Success();
    }
}
