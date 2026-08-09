using Shared.Domain;

namespace Gameplay.Domain.Entities;

internal static class HandErrors
{
    public static readonly Error InsufficientParticipantCount =
        Error.Problem("Gameplay.Hand.InsufficientParticipantCount", "Participants count can not be less than 2.");

    public static readonly Error DuplicatedParticipants =
        Error.Problem("Gameplay.Hand.DuplicatedParticipants", "Participants must be unique.");

    public static readonly Error BigBlindLessThanSmallBlind =
        Error.Problem("Gameplay.Hand.BigBlindLessThanSmallBlind", "Big blind can not be less than small blind.");

    public static readonly Error ParticipantIdNotInSeats =
        Error.Conflict("Gameplay.Hand.ParticipantIdNotInSeats", "Participant must be seated.");

    public static readonly Error HandNotFound =
        Error.NotFound("Gameplay.Hand.HandNotFound", "Hand not found.");
}
