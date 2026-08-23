using Gameplay.Domain.ValueObjects;

namespace Gameplay.Domain.Services;

internal static class PotCalculator
{
    public static List<HandPot> Layer(IReadOnlyList<HandSeatState> seatStates)
    {
        List<HandPot> pots = [];
        int previousLevel = 0;

        IEnumerable<int> allInLevels = seatStates
            .Where(ss => ss.State is SeatState.AllIn)
            .Select(ss => ss.TotalContribution.Value)
            .Distinct()
            .Order();

        foreach (int level in allInLevels)
        {
            AddLayer(pots, seatStates, previousLevel, level);
            previousLevel = level;
        }

        AddLayer(pots, seatStates, previousLevel, level: null);
        return pots;
    }

    private static void AddLayer(
        List<HandPot> pots,
        IReadOnlyList<HandSeatState> seatStates,
        int previousLevel,
        int? level)
    {
        int amount = seatStates.Sum(ss => level is { } capped
            ? Math.Min(ss.TotalContribution, capped) - Math.Min(ss.TotalContribution, previousLevel)
            : Math.Max(0, ss.TotalContribution - previousLevel));

        if (amount is 0)
        {
            return;
        }

        List<ParticipantId> eligible = [.. seatStates
            .Where(ss => IsEligible(ss, previousLevel, level))
            .Select(ss => ss.ParticipantId)];

        if (pots.Count > 0 && eligible.SequenceEqual(pots[^1].EligibleParticipantIds))
        {
            pots[^1] = pots[^1] with { Amount = (ChipsStack)(pots[^1].Amount + amount) };
        }
        else
        {
            pots.Add(new HandPot(pots.Count, (ChipsStack)amount, eligible));
        }
    }

    // for closed pots (level not null) returns if seat plays for that pot
    // for open pots (ongoing) returns if seat may play for that pot
    private static bool IsEligible(HandSeatState seat, int previousLevel, int? level)
    {
        if (seat.State is SeatState.Folded)
        {
            return false;
        }

        if (level is { } capped)
        {
            return seat.TotalContribution >= capped;
        }

        return seat.TotalContribution > previousLevel
            || (seat.TotalContribution == previousLevel && seat.State is SeatState.Active);
    }
}
