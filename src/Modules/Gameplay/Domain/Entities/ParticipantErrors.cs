using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal static class ParticipantErrors
{
    public static readonly Error InvalidName =
        Error.Problem("Gameplay.Participant.InvalidName", "Participant name cannot be empty.");
}
