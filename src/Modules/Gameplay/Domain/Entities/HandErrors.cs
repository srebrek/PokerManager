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

    public static readonly Error NotInProgressHandFinish =
        Error.Conflict("Gameplay.Hand.NotInProgressHandFinish", "Can not finish hand that is not in progress.");

    public static readonly Error PotWinnersMismatch =
        Error.Conflict("Gameplay.Hand.PotWinnersMismatch", "Exactly one winner list per pot is required.");

    public static readonly Error DuplicatedPotWinners =
        Error.Conflict("Gameplay.Hand.DuplicatedPotWinners", "Pot winners must be unique.");

    public static readonly Error IneligibleWinner =
        Error.Conflict("Gameplay.Hand.IneligibleWinner", "Winner must be eligible for the pot.");

    public static readonly Error NotFinishedStreetFinish =
        Error.Conflict("Gameplay.Hand.NotFinishedStreetFinish", "Can not finish hand that is still in play.");

    public static readonly Error WinnersNotDeclared =
        Error.Conflict("Gameplay.Hand.WinnersNotDeclared", "Pot winners have to be declared first.");

    public static readonly Error NotInProgressWinnersDeclaration =
        Error.Conflict(
            "Gameplay.Hand.NotInProgressWinnersDeclaration",
            "Can not declare winners of a hand that is not in progress.");

    public static readonly Error NotFinishedStreetWinnersDeclaration =
        Error.Conflict(
            "Gameplay.Hand.NotFinishedStreetWinnersDeclaration",
            "Can not declare winners of a hand that is still in play.");

    public static readonly Error NotInProgressHandUndo =
        Error.Conflict("Gameplay.Hand.NotInProgressHandUndo", "Can not undo an action of a hand that is not in play.");

    public static readonly Error NothingToUndo =
        Error.Conflict("Gameplay.Hand.NothingToUndo", "Blinds can not be undone.");

    public static readonly Error NotInProgressHandAbort =
        Error.Conflict("Gameplay.Hand.NotInProgressHandAbort", "Can not abort hand that is not in play.");
}
