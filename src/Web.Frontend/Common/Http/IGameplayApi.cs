using System.Net.Http.Json;
using Contracts.Api.Gameplay;

namespace Web.Frontend.Common.Http;

internal interface IGameplayApi
{
    Task<ApiResult<CreateGameResponse>> CreateGameAsync(CreateGameRequest request, CancellationToken ct = default);
    Task<ApiResult<GameLookupResponse>> GetGameByJoinCodeAsync(string joinCode, CancellationToken ct = default);
    Task<ApiResult<AddParticipantResponse>> AddParticipantAsync(
      Guid gameId, AddParticipantRequest request, CancellationToken ct = default);
    Task<ApiResult<GameStateResponse>> GetGameStateAsync(Guid gameId, CancellationToken ct = default);
    Task<ApiResult<GetHandStateResponse>> GetHandStateAsync(Guid handId, CancellationToken ct = default);
    Task<ApiResult<StartHandResponse>> StartHandAsync(
      Guid gameId, StartHandRequest request, CancellationToken ct = default);
    Task<ApiResult> RecordActionAsync(Guid handId, RecordActionRequest request, CancellationToken ct = default);
    Task<ApiResult> DeclareWinnersAsync(
      Guid handId, DeclareWinnersRequest request, CancellationToken ct = default);
    Task<ApiResult> FinishHandAsync(Guid handId, FinishHandRequest request, CancellationToken ct = default);
    Task<ApiResult> AbortHandAsync(Guid handId, AbortHandRequest request, CancellationToken ct = default);
    Task<ApiResult> UndoLastActionAsync(Guid handId, UndoLastActionRequest request, CancellationToken ct = default);
    Task<ApiResult> FinishGameAsync(Guid gameId, FinishGameRequest request, CancellationToken ct = default);
    Task<ApiResult> RebuyAsync(
      Guid gameId, Guid participantId, RebuyRequest request, CancellationToken ct = default);
    Task<ApiResult> MoveParticipantDownAsync(
      Guid gameId, Guid participantId, MoveParticipantDownRequest request, CancellationToken ct = default);
    Task<ApiResult> SetParticipantSittingOutAsync(
      Guid gameId, Guid participantId, SetParticipantSittingOutRequest request, CancellationToken ct = default);
    Task<ApiResult> MoveDealerButtonAsync(
      Guid gameId, MoveDealerButtonRequest request, CancellationToken ct = default);
    Task<ApiResult> ChangeGameRulesAsync(
      Guid gameId, ChangeGameRulesRequest request, CancellationToken ct = default);
}

internal sealed class GameplayApi(HttpClient httpClient, ILogger<GameplayApi> logger)
    : ApiClient(logger), IGameplayApi
{
    public Task<ApiResult<CreateGameResponse>> CreateGameAsync(
        CreateGameRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<CreateGameResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.Games, request, ct),
            ct);

    public Task<ApiResult<GameLookupResponse>> GetGameByJoinCodeAsync(
        string joinCode,
        CancellationToken ct = default) =>
        ExecuteAsync<GameLookupResponse>(
            ct => httpClient.GetAsync(GameplayRoutes.GamesByJoinCode(joinCode), ct),
            ct);

    public Task<ApiResult<AddParticipantResponse>> AddParticipantAsync(
        Guid gameId,
        AddParticipantRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<AddParticipantResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.GameParticipantsFor(gameId), request, ct),
            ct);

    public Task<ApiResult<GameStateResponse>> GetGameStateAsync(
        Guid gameId,
        CancellationToken ct = default) =>
        ExecuteAsync<GameStateResponse>(
            ct => httpClient.GetAsync(GameplayRoutes.GameFor(gameId), ct),
            ct);

    public Task<ApiResult<GetHandStateResponse>> GetHandStateAsync(
        Guid handId,
        CancellationToken ct = default) =>
        ExecuteAsync<GetHandStateResponse>(
            ct => httpClient.GetAsync(GameplayRoutes.HandFor(handId), ct),
            ct);

    public Task<ApiResult<StartHandResponse>> StartHandAsync(
        Guid gameId,
        StartHandRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync<StartHandResponse>(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.GameHandsFor(gameId), request, ct),
            ct);

    public Task<ApiResult> RecordActionAsync(
        Guid handId,
        RecordActionRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.HandActionsFor(handId), request, ct),
            ct);

    public Task<ApiResult> DeclareWinnersAsync(
        Guid handId,
        DeclareWinnersRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.HandWinnersFor(handId), request, ct),
            ct);

    public Task<ApiResult> FinishHandAsync(
        Guid handId,
        FinishHandRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.FinishHandFor(handId), request, ct),
            ct);

    public Task<ApiResult> AbortHandAsync(
        Guid handId,
        AbortHandRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.AbortHandFor(handId), request, ct),
            ct);

    public Task<ApiResult> UndoLastActionAsync(
        Guid handId,
        UndoLastActionRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.UndoLastActionFor(handId), request, ct),
            ct);

    public Task<ApiResult> FinishGameAsync(
        Guid gameId,
        FinishGameRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.FinishGameFor(gameId), request, ct),
            ct);

    public Task<ApiResult> RebuyAsync(
        Guid gameId,
        Guid participantId,
        RebuyRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.RebuyFor(gameId, participantId), request, ct),
            ct);

    public Task<ApiResult> MoveParticipantDownAsync(
        Guid gameId,
        Guid participantId,
        MoveParticipantDownRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(
                GameplayRoutes.MoveParticipantDownFor(gameId, participantId), request, ct),
            ct);

    public Task<ApiResult> SetParticipantSittingOutAsync(
        Guid gameId,
        Guid participantId,
        SetParticipantSittingOutRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(
                GameplayRoutes.SetParticipantSittingOutFor(gameId, participantId), request, ct),
            ct);

    public Task<ApiResult> MoveDealerButtonAsync(
        Guid gameId,
        MoveDealerButtonRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.DealerButtonFor(gameId), request, ct),
            ct);

    public Task<ApiResult> ChangeGameRulesAsync(
        Guid gameId,
        ChangeGameRulesRequest request,
        CancellationToken ct = default) =>
        ExecuteAsync(
            ct => httpClient.PostAsJsonAsync(GameplayRoutes.RulesFor(gameId), request, ct),
            ct);
}
