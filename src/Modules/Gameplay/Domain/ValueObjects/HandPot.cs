using Api = Contracts.Api.Gameplay;

namespace Gameplay.Domain.ValueObjects;

internal readonly record struct HandPot(
    int Index,
    ChipsStack Amount,
    IReadOnlyList<ParticipantId> EligibleParticipantIds);

internal static class HandPotMappings
{
    public static Api.HandPotState ToContract(this HandPot pot, IReadOnlyList<ParticipantId> winnerParticipantIds) =>
        new(
            pot.Index,
            pot.Amount,
            [.. pot.EligibleParticipantIds.Select(id => id.Value)],
            [.. winnerParticipantIds.Select(id => id.Value)]);
}
