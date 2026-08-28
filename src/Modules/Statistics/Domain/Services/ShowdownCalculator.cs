using Statistics.Domain.Entities;
using Statistics.Domain.ValueObjects;

namespace Statistics.Domain.Services;

internal static class ShowdownCalculator
{
    public static bool WentToShowdown(int seatCount, IEnumerable<HandAction> liveActions)
    {
        int foldedCount = liveActions
            .Where(action => action.Type is HandActionType.Fold)
            .Select(action => action.ParticipantId)
            .Distinct()
            .Count();

        return seatCount - foldedCount >= 2;
    }
}
