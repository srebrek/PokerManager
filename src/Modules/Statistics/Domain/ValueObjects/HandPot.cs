namespace Statistics.Domain.ValueObjects;

internal sealed class HandPot
{
    public int PotIndex { get; private init; }
    public int Amount { get; private init; }

    // Navigation Property
    public IReadOnlyList<HandPotWinner> Winners => _winners.AsReadOnly();

    private readonly List<HandPotWinner> _winners;

    private HandPot(int potIndex, int amount)
    {
        PotIndex = potIndex;
        Amount = amount;
        _winners = [];
    }

    public static HandPot Create(int potIndex, int amount, IEnumerable<Guid> winnerParticipantIds)
    {
        HandPot pot = new(potIndex, amount);
        pot._winners.AddRange(winnerParticipantIds.Select(id => new HandPotWinner(id)));
        return pot;
    }
}
