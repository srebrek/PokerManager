using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Game : AggregateRoot<GameId>
{
    public const int DefaultStartingStack = 1000;

    public JoinCode JoinCode { get; }
    public ParticipantId HostParticipantId { get; }
    public ChipsStack SmallBlind { get; }
    public ChipsStack BigBlind { get; }
    public bool IsFinished { get; private set; }
    public IReadOnlyList<ParticipantId> SeatingOrder => _seatingOrder.AsReadOnly();
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
}
