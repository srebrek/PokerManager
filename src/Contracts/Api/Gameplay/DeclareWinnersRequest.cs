namespace Contracts.Api.Gameplay;

public sealed record DeclareWinnersRequest(Guid ActingParticipantId, IReadOnlyList<PotWinner> Winners);

public sealed record PotWinner(int PotIndex, Guid ParticipantId);
