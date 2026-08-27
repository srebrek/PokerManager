namespace Contracts.IntegrationEvents.Gameplay;

public sealed record HandFinishedIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid HandId,
    IReadOnlyList<HandFinishedPot> Pots,
    IReadOnlyList<HandFinishedPotWinner> PotWinners,
    IReadOnlyList<HandFinishedResult> Results) : IIntegrationEvent;

public sealed record HandFinishedPot(int Index, int Amount);

public sealed record HandFinishedPotWinner(int PotIndex, Guid ParticipantId);

public sealed record HandFinishedResult(Guid ParticipantId, string Name, int Net, int EndingChips);
