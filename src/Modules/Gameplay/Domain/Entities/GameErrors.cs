using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal static class GameErrors
{
    public static readonly Error BigBlindLessThanSmallBlind =
        Error.Problem("Gameplay.Game.BigBlindLessThanSmallBlind", "Big blind can not be less than small blind.");

    public static readonly Error GameNotFound =
        Error.NotFound("Gameplay.Game.GameNotFound", "Game not found.");
}
