using Shared.Domain;

namespace Gameplay.Domain.Entities;

public static class ParticipantErrors
{
    public static readonly Error InvalidName =
        Error.Problem("Gameplay.Participant.InvalidName", "Participant name cannot be empty.");
}
