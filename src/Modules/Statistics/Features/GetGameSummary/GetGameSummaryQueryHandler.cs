using Contracts.Api.Statistics;
using Microsoft.EntityFrameworkCore;
using Shared.Domain;
using Statistics.Domain.Entities;
using Statistics.Domain.Services;
using Statistics.Domain.ValueObjects;
using Statistics.Infrastructure.Data;

namespace Statistics.Features.GetGameSummary;

internal sealed class GetGameSummaryQueryHandler(StatisticsDbContext db)
{
    // TODO: add tests
    public async Task<Result<GameSummaryResponse>> Handle(GetGameSummaryQuery query, CancellationToken ct)
    {
        var hands = await db.Hands
            .Where(hand => hand.GameId == query.GameId && hand.FinishedAt != null)
            .OrderBy(hand => hand.StartedAt)
            .Select(hand => new
            {
                hand.Id,
                hand.StartedAt,
                FinishedAt = hand.FinishedAt!.Value,
                SeatedParticipantIds = hand.Seats.Select(seat => seat.ParticipantId).ToList(),
                LiveActions = hand.Actions.Where(action => action.UndoneAt == null).ToList(),
                Results = hand.Results.ToList(),
            })
            .AsNoTracking()
            .ToListAsync(ct);

        List<Rebuy> rebuys = await db.Rebuys
            .AsNoTracking()
            .Where(rebuy => rebuy.GameId == query.GameId)
            .ToListAsync(ct);

        Dictionary<Guid, bool> showdownByHandId = hands.ToDictionary(
            hand => hand.Id,
            hand => !EarlyFinishCalculator.FinishedEarly(hand.LiveActions));

        List<GameSummaryHand> summaryHands = [.. hands.Select((hand, index) => new GameSummaryHand(
            hand.Id,
            index + 1,
            hand.StartedAt,
            hand.FinishedAt,
            showdownByHandId[hand.Id]))];

        List<Guid> orderedHandIds = [.. hands.Select(hand => hand.Id)];

        List<GameSummaryParticipant> participants = [.. hands
            .SelectMany(hand => hand.Results.Select(result => (HandId: hand.Id, Result: result)))
            .GroupBy(row => row.Result.ParticipantId)
            .Select(group => BuildParticipant(
                group.Key,
                group.ToDictionary(row => row.HandId, row => row.Result),
                orderedHandIds,
                hands.Count(hand => hand.SeatedParticipantIds.Contains(group.Key)),
                rebuys.Where(rebuy => rebuy.ParticipantId == group.Key).Sum(rebuy => rebuy.Amount)))
            .OrderByDescending(participant => participant.Net)];

        int showdownCount = showdownByHandId.Values.Count(wentToShowdown => wentToShowdown);

        return new GameSummaryResponse(
            query.GameId,
            hands.Count,
            showdownCount,
            hands.Count is 0 ? 0 : (double)showdownCount / hands.Count,
            summaryHands,
            participants);
    }

    private static GameSummaryParticipant BuildParticipant(
        Guid participantId,
        IReadOnlyDictionary<Guid, HandResult> resultByHandId,
        IReadOnlyList<Guid> orderedHandIds,
        int handsSeated,
        int totalRebuys)
    {
        List<HandResult?> timeline = [.. orderedHandIds.Select(resultByHandId.GetValueOrDefault)];
        HandResult latest = timeline.OfType<HandResult>().Last();

        return new GameSummaryParticipant(
            participantId,
            latest.ParticipantName,
            handsSeated,
            resultByHandId.Values.Sum(result => result.Net),
            totalRebuys,
            latest.EndingChips,
            [.. timeline.Select(result => result?.EndingChips)]);
    }
}
