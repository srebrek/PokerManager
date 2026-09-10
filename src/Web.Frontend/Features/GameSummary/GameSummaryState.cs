using Contracts.Api.Statistics;

namespace Web.Frontend.Features.GameSummary;

internal interface IGameSummaryState;

internal sealed record Loading : IGameSummaryState;

internal sealed record Loaded(GameSummaryResponse Summary) : IGameSummaryState;

internal sealed record Failed(string Message) : IGameSummaryState;
