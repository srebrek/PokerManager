namespace Contracts.Api.Gameplay;

public static class GameplayRoutes
{
    public const string Tag = "Gameplay";

    public const string Games = "gameplay/games";
    public static Uri GamesByJoinCode(string joinCode) =>
        new($"{Games}?joinCode={Uri.EscapeDataString(joinCode)}", UriKind.Relative);
    public const string Game = $"{Games}/{{gameId:guid}}";
    public static Uri GameFor(Guid gameId) => new($"{Games}/{gameId}", UriKind.Relative);
    public const string GameRouteName = "Game";
    public const string GameParticipants = $"{Game}/participants";
    public static Uri GameParticipantsFor(Guid gameId) => new($"{Games}/{gameId}/participants", UriKind.Relative);
    public const string Rebuy = $"{GameParticipants}/{{participantId:guid}}/rebuy";
    public static Uri RebuyFor(Guid gameId, Guid participantId) =>
        new($"{Games}/{gameId}/participants/{participantId}/rebuy", UriKind.Relative);
    public const string MoveParticipantDown = $"{GameParticipants}/{{participantId:guid}}/move-down";
    public static Uri MoveParticipantDownFor(Guid gameId, Guid participantId) =>
        new($"{Games}/{gameId}/participants/{participantId}/move-down", UriKind.Relative);
    public const string SetParticipantSittingOut = $"{GameParticipants}/{{participantId:guid}}/sitting-out";
    public static Uri SetParticipantSittingOutFor(Guid gameId, Guid participantId) =>
        new($"{Games}/{gameId}/participants/{participantId}/sitting-out", UriKind.Relative);
    public const string DealerButton = $"{Game}/dealer-button";
    public static Uri DealerButtonFor(Guid gameId) => new($"{Games}/{gameId}/dealer-button", UriKind.Relative);
    public const string Rules = $"{Game}/rules";
    public static Uri RulesFor(Guid gameId) => new($"{Games}/{gameId}/rules", UriKind.Relative);
    public const string GameHands = $"{Game}/hands";
    public static Uri GameHandsFor(Guid gameId) => new($"{Games}/{gameId}/hands", UriKind.Relative);
    public const string FinishGame = $"{Game}/finish";
    public static Uri FinishGameFor(Guid gameId) => new($"{Games}/{gameId}/finish", UriKind.Relative);

    private const string Hands = "gameplay/hands";
    public const string Hand = $"{Hands}/{{handId:guid}}";
    public static Uri HandFor(Guid handId) => new($"{Hands}/{handId}", UriKind.Relative);
    public const string HandRouteName = "Hand";
    public const string HandActions = $"{Hand}/actions";
    public static Uri HandActionsFor(Guid handId) => new($"{Hands}/{handId}/actions", UriKind.Relative);
    public const string HandWinners = $"{Hand}/winners";
    public static Uri HandWinnersFor(Guid handId) => new($"{Hands}/{handId}/winners", UriKind.Relative);
    public const string FinishHand = $"{Hand}/finish";
    public static Uri FinishHandFor(Guid handId) => new($"{Hands}/{handId}/finish", UriKind.Relative);
    public const string AbortHand = $"{Hand}/abort";
    public static Uri AbortHandFor(Guid handId) => new($"{Hands}/{handId}/abort", UriKind.Relative);
    public const string UndoLastAction = $"{Hand}/undo";
    public static Uri UndoLastActionFor(Guid handId) => new($"{Hands}/{handId}/undo", UriKind.Relative);

    public const string Hub = "gameplay/hub";
    public const string HubJoinGameMethod = "JoinGame";
    public const string HubLeaveGameMethod = "LeaveGame";
    public const string HubGameUpdatedMethod = "GameUpdated";
    public const string HubJoinHandMethod = "JoinHand";
    public const string HubLeaveHandMethod = "LeaveHand";
    public const string HubApplyHandActionEffectMethod = "ApplyHandActionEffect";
    public const string HubHandUpdatedMethod = "HandUpdated";
}
