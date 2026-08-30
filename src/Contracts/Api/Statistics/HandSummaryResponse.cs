namespace Contracts.Api.Statistics;

public sealed record HandSummaryResponse(
    Guid HandId,
    Guid GameId,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    TimeSpan Duration,
    int ActionCount,
    bool WentToShowdown,
    IReadOnlyList<HandSummaryPot> Pots,
    IReadOnlyList<HandSummaryResult> Results);

public sealed record HandSummaryPot(int Index, int Amount, IReadOnlyList<Guid> WinnerParticipantIds);

public sealed record HandSummaryResult(
    Guid ParticipantId,
    string Name,
    int Net,
    int EndingChips,
    bool WasSeated);
