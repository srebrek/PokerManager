using Gameplay.Domain.Events;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Game : AggregateRoot<GameId>
{
    public const int DefaultStartingStack = 1000;
    public const int MinimumParticipantsToStartAHand = 3;

    public JoinCode JoinCode { get; }
    public ParticipantId HostParticipantId { get; }
    public ChipsStack SmallBlind { get; }
    public ChipsStack BigBlind { get; }
    public bool IsFinished { get; private set; }
    public IReadOnlyList<ParticipantId> SeatingOrder => _seatingOrder.AsReadOnly();
    public HandId? CurrentHandId { get; private set; }

    // Navigation Property
    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();

    private readonly List<ParticipantId> _seatingOrder;
    private readonly List<Participant> _participants;
    private const int DealerSeatIndex = 0;

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

        if (!Participant.Create(hostName, (ChipsStack)DefaultStartingStack)
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
        if (!Participant.Create(participantName, (ChipsStack)DefaultStartingStack)
                .TryGetValue(out Participant? participant, out Error? error))
        {
            return error;
        }

        _participants.Add(participant);
        _seatingOrder.Add(participant.Id);

        Raise(new ParticipantAddedDomainEvent(Id.Value, participant.Id.Value));

        return participant.Id;
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

        if (_participants.Count < MinimumParticipantsToStartAHand)
        {
            return GameErrors.NotEnoughParticipants;
        }

        if (_participants.Any(p => p.Chips.Value < BigBlind.Value))
        {
            return GameErrors.InsufficientChipsForBigBlind;
        }

        return Result.Success();
    }

    private List<HandSeat> BuildSeats()
    {
        Dictionary<ParticipantId, Participant> participantsById = _participants.ToDictionary(p => p.Id);
        int count = _seatingOrder.Count;

        List<HandSeat> seats = new(count);
        for (int offset = 0; offset < count; offset++)
        {
            ParticipantId participantId = _seatingOrder[(DealerSeatIndex + 1 + offset) % count];
            Participant participant = participantsById[participantId];
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

        Raise(new HandAwardsAppliedDomainEvent(Id.Value, handId.Value));

        return Result.Success();
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
