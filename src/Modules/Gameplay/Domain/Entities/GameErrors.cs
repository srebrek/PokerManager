using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal static class GameErrors
{
    public static readonly Error BigBlindLessThanSmallBlind =
        Error.Problem("Gameplay.Game.BigBlindLessThanSmallBlind", "Big blind can not be less than small blind.");

    public static readonly Error GameNotFound =
        Error.NotFound("Gameplay.Game.GameNotFound", "Game not found.");

    public static readonly Error NotHost =
        Error.Conflict("Gameplay.Game.NotHost", "Participant is not a host.");

    public static readonly Error InsufficientChipsForBigBlind =
        Error.Conflict("Gameplay.Game.InsufficientChipsForBigBlind",
        "A seated participant has fewer chips than the big blind.");

    public static readonly Error GameFinished =
        Error.Conflict("Gameplay.Game.GameFinished", "Game is already finished.");

    public static readonly Error NotEnoughParticipants =
        Error.Conflict("Gameplay.Game.NotEnoughParticipants", "Not enough participants to start a hand.");

    public static readonly Error HandIsRunning =
        Error.Conflict("Gameplay.Game.HandIsRunning", "Hand is already running.");
}
