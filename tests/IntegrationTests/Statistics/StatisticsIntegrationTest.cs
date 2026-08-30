// using Contracts.Api.Statistics;
using Contracts.IntegrationEvents;
using Contracts.IntegrationEvents.Gameplay;
using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace IntegrationTests.Statistics;

public abstract class StatisticsIntegrationTest(ApiFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly DateTimeOffset s_origin = new(2026, 1, 1, 20, 0, 0, TimeSpan.Zero);

    protected const int SmallBlindAmount = 5;
    protected const int BigBlindAmount = 10;

    protected static DateTimeOffset At(int minutesFromTheStart) => s_origin.AddMinutes(minutesFromTheStart);

    protected async Task IngestAsync(params IIntegrationEvent[] integrationEvents)
    {
        ArgumentNullException.ThrowIfNull(integrationEvents);

        using IServiceScope scope = Services.CreateScope();
        IMessageBus bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        foreach (IIntegrationEvent integrationEvent in integrationEvents)
        {
            await bus.InvokeAsync(integrationEvent, CancellationToken);
        }
    }

    protected static HandStartedIntegrationEvent HandStarted(
        Table table,
        DateTimeOffset occurredAt,
        params (Guid ParticipantId, string Name, int StartingChips)[] seats)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(seats);

        return new HandStartedIntegrationEvent(
            Guid.NewGuid(),
            occurredAt,
            table.HandId,
            table.GameId,
            [.. seats.Select((seat, position) =>
                new HandStartedSeat(seat.ParticipantId, seat.Name, position, seat.StartingChips))],
            [
                new HandStartedBlind(0, seats[0].ParticipantId, HandActionType.PostSmallBlind, SmallBlindAmount),
                new HandStartedBlind(1, seats[1].ParticipantId, HandActionType.PostBigBlind, BigBlindAmount),
            ]);
    }

    protected static HandActionRecordedIntegrationEvent ActionRecorded(
        Table table,
        DateTimeOffset occurredAt,
        int sequenceNumber,
        Guid participantId,
        HandActionType type,
        int? amountTo = null)
    {
        ArgumentNullException.ThrowIfNull(table);

        return new HandActionRecordedIntegrationEvent(
            Guid.NewGuid(),
            occurredAt,
            table.HandId,
            sequenceNumber,
            participantId,
            type,
            amountTo);
    }

    protected static HandActionUndoneIntegrationEvent ActionUndone(
        Table table,
        DateTimeOffset occurredAt,
        int sequenceNumber)
    {
        ArgumentNullException.ThrowIfNull(table);

        return new HandActionUndoneIntegrationEvent(
            Guid.NewGuid(),
            occurredAt,
            table.HandId,
            sequenceNumber);
    }

    protected static HandFinishedIntegrationEvent HandFinished(
        Table table,
        DateTimeOffset occurredAt,
        IReadOnlyList<HandFinishedPot> pots,
        IReadOnlyList<HandFinishedPotWinner> potWinners,
        IReadOnlyList<HandFinishedResult> results)
    {
        ArgumentNullException.ThrowIfNull(table);

        return new HandFinishedIntegrationEvent(
            Guid.NewGuid(),
            occurredAt,
            table.HandId,
            pots,
            potWinners,
            results);
    }

    protected static HandAbortedIntegrationEvent HandAborted(Table table, DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(table);

        return new HandAbortedIntegrationEvent(Guid.NewGuid(), occurredAt, table.HandId);
    }

    protected static RebuyRecordedIntegrationEvent RebuyRecorded(
        Table table,
        DateTimeOffset occurredAt,
        Guid participantId,
        int amount)
    {
        ArgumentNullException.ThrowIfNull(table);

        return new RebuyRecordedIntegrationEvent(
            Guid.NewGuid(),
            occurredAt,
            table.GameId,
            participantId,
            amount);
    }

    // protected async Task<HttpStatusCode> GetHandSummaryStatusAsync(Guid handId)
    // {
    //     using HttpResponseMessage response = await Client.GetAsync(
    //         StatisticsRoutes.HandSummaryFor(handId),
    //         CancellationToken);
    //     return response.StatusCode;
    // }

    // protected async Task<HandSummaryResponse> GetHandSummaryAsync(Guid handId)
    // {
    //     HandSummaryResponse? summary = await Client.GetFromJsonAsync<HandSummaryResponse>(
    //         StatisticsRoutes.HandSummaryFor(handId),
    //         CancellationToken);
    //     summary.ShouldNotBeNull();
    //     return summary;
    // }

    // protected async Task<GameSummaryResponse> GetGameSummaryAsync(Guid gameId)
    // {
    //     GameSummaryResponse? summary = await Client.GetFromJsonAsync<GameSummaryResponse>(
    //         StatisticsRoutes.GameSummaryFor(gameId),
    //         CancellationToken);
    //     summary.ShouldNotBeNull();
    //     return summary;
    // }

    protected sealed record Table(Guid GameId, Guid HandId, Guid Host, Guid SmallBlind, Guid BigBlind)
    {
        public static Table New() => new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid());

        public Table NextHand() => this with { HandId = Guid.NewGuid() };

        public (Guid ParticipantId, string Name, int StartingChips)[] Seats(
            int hostChips = 1000,
            int smallBlindChips = 1000,
            int bigBlindChips = 1000) =>
        [
            (SmallBlind, "TestSmallBlindName", smallBlindChips),
            (BigBlind, "TestBigBlindName", bigBlindChips),
            (Host, "TestHostName", hostChips),
        ];
    }
}
