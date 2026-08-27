using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Shared.Domain;

namespace Gameplay.UnitTests.HandTests;

public sealed class StartTests
{
    private static HandSeat Seat(int position, int startingStack = 1000) =>
        new(ParticipantId.New(), (ChipsStack)startingStack, position);

    private static Result<Hand> Start(
        IReadOnlyList<HandSeat> seats,
        int smallBlind = Hands.SmallBlind,
        int bigBlind = Hands.BigBlind) =>
        Hand.Start(GameId.New(), seats, (ChipsStack)smallBlind, (ChipsStack)bigBlind);

    [Fact]
    public void Start_SeatsOutOfOrder_OrdersSeatsByPositionAndPostsBlinds()
    {
        // Arrange
        GameId gameId = GameId.New();
        HandSeat smallBlind = Seat(0, 1000);
        HandSeat bigBlind = Seat(1, 800);
        HandSeat dealer = Seat(2, 600);

        // Act
        Result<Hand> result = Hand.Start(
            gameId,
            [bigBlind, dealer, smallBlind],
            (ChipsStack)Hands.SmallBlind,
            (ChipsStack)Hands.BigBlind);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        Hand hand = result.Value;
        hand.GameId.ShouldBe(gameId);
        hand.Status.ShouldBe(HandStatus.InProgress);
        hand.Seats.ShouldBe([smallBlind, bigBlind, dealer]);
        hand.Actions
            .Select(a => (a.SequenceNumber, a.ParticipantId, a.Type, a.AmountTo))
            .ShouldBe([
                (0, smallBlind.ParticipantId, HandActionType.PostSmallBlind, (ChipsStack?)Hands.SmallBlind),
                (1, bigBlind.ParticipantId, HandActionType.PostBigBlind, (ChipsStack?)Hands.BigBlind)]);
    }

    [Fact]
    public void Start_FewerSeatsThanTheMinimum_ReturnsInsufficientParticipantCountFailure()
    {
        // Arrange
        HandSeat smallBlind = Seat(0);
        HandSeat bigBlind = Seat(1);

        // Act
        Result<Hand> result = Start([smallBlind, bigBlind]);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(HandErrors.InsufficientParticipantCount);
    }

    [Fact]
    public void Start_SameParticipantSeatedTwice_ReturnsDuplicatedParticipantsFailure()
    {
        // Arrange
        HandSeat smallBlind = Seat(0);
        HandSeat bigBlind = smallBlind with { Position = 1 };

        // Act
        Result<Hand> result = Start([smallBlind, bigBlind, Seat(2)]);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(HandErrors.DuplicatedParticipants);
    }

    [Fact]
    public void Start_BigBlindLessThanSmallBlind_ReturnsBigBlindLessThanSmallBlindFailure()
    {
        // Arrange
        // Act
        Result<Hand> result = Start([Seat(0), Seat(1), Seat(2)], 20, 10);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(HandErrors.BigBlindLessThanSmallBlind);
    }

    [Fact]
    public void Start_SeatCanNotCoverTheBigBlind_ReturnsInsufficientChipsForBigBlindFailure()
    {
        // Act
        Result<Hand> result = Start([Seat(0), Seat(1), Seat(2, Hands.BigBlind - 1)]);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(HandErrors.InsufficientChipsForBigBlind);
    }
}
