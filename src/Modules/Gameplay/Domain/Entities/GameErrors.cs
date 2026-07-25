using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal static class GameErrors
{
    public static readonly Error BigBlindLessThanSmallBlind =
        Error.Problem("Gameplay.Game.BigBlindLessThanSmallBlind", "Big blind can not be less than small blind.");

    public static readonly Error JoinEndedGame =
        Error.Conflict("Gameplay.Game.JoinEndedGame", "Can not join a game that has ended.");

    public static readonly Error GameAlreadyStarted =
        Error.Conflict("Gameplay.Game.GameAlreadyStarted", "Game has already started.");

    public static readonly Error InsufficientParticipantCount =
        Error.Conflict("Gameplay.Game.InsufficientParticipantCount", "Participants count can not be less than 2.");

    public static readonly Error EndNotInProgressGame =
        Error.Conflict("Gameplay.Game.EndNotInProgressGame", "Can not end a game that is not in progress.");

    public static readonly Error BuildSeatsForNotInProgressGame =
        Error.Conflict("Gameplay.Game.BuildSeatsForNotInProgressGame", "Game must be in progress to build hand seats.");

    public static readonly Error InsufficientActiveParticipantCount =
        Error.Conflict(
            "Gameplay.Game.InsufficientActiveParticipantCount",
            "Active participants count can not be less than 2.");

    public static readonly Error GameNotFound =
        Error.NotFound("Gameplay.Game.GameNotFound", "Game not found.");
}
