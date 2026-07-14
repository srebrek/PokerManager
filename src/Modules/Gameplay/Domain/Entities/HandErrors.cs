using Shared.Domain;

namespace Gameplay.Domain.Entities;

public static class HandErrors
{
    public static readonly Error InsufficientParticipantCount =
        Error.Problem("Gameplay.Hand.InsufficientParticipantCount", "Participants count can not be less than 2.");

    public static readonly Error DuplicatedParticipants =
        Error.Problem("Gameplay.Hand.DuplicatedParticipants", "Participants must be unique.");

    public static readonly Error BigBlindLessThanSmallBlind =
        Error.Problem("Gameplay.Hand.BigBlindLessThanSmallBlind", "Big blind can not be less than small blind.");

    public static readonly Error InsufficientChipsStack =
        Error.Conflict("Gameplay.Hand.InsufficientChipsStack", "Insufficient chips.");

    public static readonly Error ParticipantIdNotInSeats =
        Error.Conflict("Gameplay.Hand.ParticipantIdNotInSeats", "Participant must be seated.");

    public static readonly Error SeatFolded =
        Error.Conflict("Gameplay.Hand.SeatFolded", "Folded seat can not act.");

    public static readonly Error SeatAllIned =
        Error.Conflict("Gameplay.Hand.SeatAllIned", "AllIned seat can not act.");

    public static readonly Error UndoBlind =
        Error.Conflict("Gameplay.Hand.UndoBlind", "Blinds can not be undo.");

    public static readonly Error UndoCompletedHand =
        Error.Conflict("Gameplay.Hand.UndoCompletedHand", "Completed hand can not be undo.");

    public static readonly Error AdvanceStreetOnCompletedHand =
        Error.Conflict("Gameplay.Hand.AdvanceStreetOnCompletedHand", "Can not advance street of completed hand.");

    public static readonly Error NoNextStreet =
        Error.Conflict("Gameplay.Hand.NoNextStreet", "River is the last street.");

    public static readonly Error CompleteCompletedHand =
        Error.Conflict("Gameplay.Hand.CompleteCompletedHand", "Can not complete completed hand.");
}
