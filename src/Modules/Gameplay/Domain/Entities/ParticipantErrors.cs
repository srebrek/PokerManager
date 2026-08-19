using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal static class ParticipantErrors
{
    public static readonly Error InvalidName =
        Error.Problem("Gameplay.Participant.InvalidName", "Participant name cannot be empty.");

    public static readonly Error RebuyLeavesNegativeChips =
        Error.Conflict(
            "Gameplay.Participant.RebuyLeavesNegativeChips",
            "Rebuy would leave the participant with negative chips.");

    public static readonly Error RebuyLeavesNegativeTotalBuyIn =
        Error.Conflict(
            "Gameplay.Participant.RebuyLeavesNegativeTotalBuyIn",
            "Rebuy would take back more chips than the participant ever bought in for.");

    public static readonly Error AlreadySittingOut =
        Error.Conflict("Gameplay.Participant.AlreadySittingOut", "Participant is already sitting out.");

    public static readonly Error AlreadySittingIn =
        Error.Conflict("Gameplay.Participant.AlreadySittingIn", "Participant is already sitting in.");
}
