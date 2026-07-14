using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal sealed class Game : AggregateRoot<GameId>
{
    private readonly List<Participant> _participants;

    public JoinCode JoinCode { get; }
    public ParticipantId HostParticipantId { get; }
    public ChipsStack SmallBlind { get; }
    public ChipsStack BigBlind { get; }
    public GameStatus Status { get; private set; }
    public IReadOnlyList<Participant> Participants => _participants.AsReadOnly();

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
        _participants = [];
        Status = GameStatus.NotStarted;
    }

    public static Result<Game> Create(string hostName, ChipsStack hostChips, ChipsStack smallBlind, ChipsStack bigBlind)
    {
        if (bigBlind.Value < smallBlind.Value)
        {
            return Result.Failure<Game>(GameErrors.BigBlindLessThanSmallBlind);
        }

        Result<Participant> hostResult = Participant.Create(hostName, hostChips);

        if (hostResult.IsFailure)
        {
            return Result.Failure<Game>(hostResult.Error);
        }

        Game game = new(GameId.New(), JoinCode.Generate(), hostResult.Value.Id, smallBlind, bigBlind);
        game._participants.Add(hostResult.Value);

        return game;
    }

    public Result Join(string name, ChipsStack chips)
    {
        if (Status is not GameStatus.NotStarted)
        {
            return Result.Failure(GameErrors.JoinStartedGame);
        }

        Result<Participant> participantResult = Participant.Create(name, chips);

        if (participantResult.IsFailure)
        {
            return Result.Failure(participantResult.Error);
        }

        _participants.Add(participantResult.Value);

        return Result.Success();
    }

    public Result Start()
    {
        if (Status is not GameStatus.NotStarted)
        {
            return Result.Failure(GameErrors.GameAlreadyStarted);
        }

        if (_participants.Count < 2)
        {
            return Result.Failure(GameErrors.InsufficientParticipantCount);
        }

        Status = GameStatus.InProgress;

        return Result.Success();
    }

    public Result End()
    {
        if (Status is not GameStatus.InProgress)
        {
            return Result.Failure(GameErrors.EndNotInProgressGame);
        }

        Status = GameStatus.Ended;

        return Result.Success();
    }

    public Result<List<HandSeat>> BuildSeatsForNextHand()
    {
        if (Status is not GameStatus.InProgress)
        {
            return Result.Failure<List<HandSeat>>(GameErrors.BuildSeatsForNotInProgressGame);
        }

        List<Participant> activeParticipants = [.. _participants.Where(p => !p.SittingOut)];

        if (activeParticipants.Count < 2)
        {
            return Result.Failure<List<HandSeat>>(GameErrors.InsufficientActiveParticipantCount);
        }

        List<HandSeat> seats = [.. activeParticipants
            .Select((participant, index) => new HandSeat(participant.Id, participant.Chips, index))];

        return seats;
    }
}
