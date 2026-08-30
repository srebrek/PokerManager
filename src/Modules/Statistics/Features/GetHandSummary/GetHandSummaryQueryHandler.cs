using Contracts.Api.Statistics;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;
using Statistics.Domain;
using Statistics.Domain.Services;
using Statistics.Domain.ValueObjects;
using Statistics.Infrastructure.Data;

namespace Statistics.Features.GetHandSummary;

internal sealed class GetHandSummaryQueryHandler(StatisticsDbContext db)
{
    // TODO: add tests
    public async Task<Result<HandSummaryResponse>> Handle(GetHandSummaryQuery query, CancellationToken ct)
    {
        var hand = await db.Hands
            .Where(h => h.Id == query.HandId && h.FinishedAt != null)
            .Select(h => new
            {
                h.Id,
                h.GameId,
                h.StartedAt,
                FinishedAt = h.FinishedAt!.Value,
                SeatedParticipantIds = h.Seats.Select(seat => seat.ParticipantId).ToList(),
                LiveActions = h.Actions.Where(action => action.UndoneAt == null).ToList(),
                Results = h.Results.ToList(),
                Pots = h.Pots
                    .OrderBy(pot => pot.PotIndex)
                    .Select(pot => new
                    {
                        pot.PotIndex,
                        pot.Amount,
                        WinnerParticipantIds = pot.Winners.Select(winner => winner.ParticipantId).ToList(),
                    })
                    .ToList(),
            })
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

        if (hand is null)
        {
            return StatisticsErrors.HandSummaryNotAvailable;
        }

        HashSet<Guid> seated = [.. hand.SeatedParticipantIds];
        int voluntaryActionCount = hand.LiveActions.Count(action =>
            action.Type is not (HandActionType.PostSmallBlind or HandActionType.PostBigBlind));

        return new HandSummaryResponse(
            hand.Id,
            hand.GameId,
            hand.StartedAt,
            hand.FinishedAt,
            hand.FinishedAt - hand.StartedAt,
            voluntaryActionCount,
            !EarlyFinishCalculator.FinishedEarly(hand.LiveActions),
            [.. hand.Pots.Select(pot => new HandSummaryPot(
                pot.PotIndex,
                pot.Amount,
                pot.WinnerParticipantIds))],
            [.. hand.Results
                .OrderByDescending(result => result.Net)
                .Select(result => new HandSummaryResult(
                    result.ParticipantId,
                    result.ParticipantName,
                    result.Net,
                    result.EndingChips,
                    seated.Contains(result.ParticipantId)))]);
    }
}
