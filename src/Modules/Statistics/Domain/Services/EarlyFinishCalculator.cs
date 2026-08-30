using Statistics.Domain.Entities;
using Statistics.Domain.ValueObjects;

namespace Statistics.Domain.Services;

internal static class EarlyFinishCalculator
{
    public static bool FinishedEarly(IEnumerable<HandAction> liveActions)
    {
        int liveCount = liveActions
            .GroupBy(action => action.ParticipantId)
            .Count(seat => seat.All(action => action.Type is not HandActionType.Fold));

        return liveCount < 2;
    }
}
