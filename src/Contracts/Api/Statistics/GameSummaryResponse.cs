namespace Contracts.Api.Statistics;

public sealed record GameSummaryResponse(
    Guid GameId,
    int HandCount,
    int ShowdownCount,
    double ShowdownRate,
    IReadOnlyList<GameSummaryHand> Hands,
    IReadOnlyList<GameSummaryParticipant> Participants);

public sealed record GameSummaryHand(
    Guid HandId,
    int Number,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    bool WentToShowdown);

public sealed record GameSummaryParticipant(
    Guid ParticipantId,
    string Name,
    int HandsSeated,
    int Net,
    int TotalRebuys,
    int EndingChips,
    IReadOnlyList<int?> ChipsTimeline);
