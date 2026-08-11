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
            return Result.Failure<Game>(GameErrors.BigBlindLessThanSmallBlind);
        }

        Result<Participant> hostResult = Participant.Create(hostName, ChipsStack.Create(DefaultStartingStack).Value);

        if (hostResult.IsFailure)
        {
            return Result.Failure<Game>(hostResult.Error);
        }

        Game game = new(GameId.New(), JoinCode.Generate(), hostResult.Value.Id, smallBlind, bigBlind);
        game._participants.Add(hostResult.Value);
        game._seatingOrder.Add(hostResult.Value.Id);

        return game;
    }

    public Result<ParticipantId> Join(string participantName)
    {
        Result<Participant> participantResult =
            Participant.Create(participantName, ChipsStack.Create(DefaultStartingStack).Value);

        if (participantResult.IsFailure)
        {
            return Result.Failure<ParticipantId>(participantResult.Error);
        }

        _participants.Add(participantResult.Value);
        _seatingOrder.Add(participantResult.Value.Id);

        return participantResult.Value.Id;
    }

    public Result<IReadOnlyList<HandSeat>> PrepareNextHand(ParticipantId actingParticipantId)
    {
        Result validationResult = ValidatePrepareNextHandInput(actingParticipantId);
        if (validationResult.IsFailure)
        {
            return Result.Failure<IReadOnlyList<HandSeat>>(validationResult.Error);
        }

        return BuildSeats();
    }

    private Result ValidatePrepareNextHandInput(ParticipantId actingParticipantId)
    {
        if (actingParticipantId != HostParticipantId)
        {
            return Result.Failure(GameErrors.NotHost);
        }

        if (IsFinished)
        {
            return Result.Failure(GameErrors.GameFinished);
        }

        if (CurrentHandId is not null)
        {
            return Result.Failure(GameErrors.HandIsRunning);
        }

        if (_participants.Count < MinimumParticipantsToStartAHand)
        {
            return Result.Failure(GameErrors.NotEnoughParticipants);
        }

        if (_participants.Any(p => p.Chips.Value < BigBlind.Value))
        {
            return Result.Failure(GameErrors.InsufficientChipsForBigBlind);
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
            return Result.Failure(GameErrors.HandIsRunning);
        }

        CurrentHandId = handId;
        return Result.Success();
    }

    public Result ApplyHandAwards(IReadOnlyList<HandAward> handAwards, ParticipantId actingParticipant, HandId handId)
    {
        if (HostParticipantId != actingParticipant)
        {
            return Result.Failure(GameErrors.NotHost);
        }

        if (CurrentHandId is null)
        {
            return Result.Failure(GameErrors.ApplyAwardsWithoutCurrentHand);
        }

        if (CurrentHandId != handId)
        {
            return Result.Failure(GameErrors.InvalidHandId);
        }

        CurrentHandId = null;

        foreach (HandAward award in handAwards)
        {
            Participant participant = Participants.Single(p => p.Id == award.ParticipantId);
            Result result = participant.AddChips(award.Net);
            if (result.IsFailure)
            {
                return Result.Failure(result.Error);
            }
        }

        return Result.Success();
    }
}
