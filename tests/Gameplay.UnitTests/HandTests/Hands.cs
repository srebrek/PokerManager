using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using static Gameplay.Domain.ValueObjects.HandActionType;

namespace Gameplay.UnitTests.HandTests;

internal static class Hands
{
    public const int SmallBlind = 10;
    public const int BigBlind = 20;

    public static Hand Start(params int[] startingStacks)
    {
        List<HandSeat> seats = [.. startingStacks.Index().Select(seat => new HandSeat(
            ParticipantId.New(),
            (ChipsStack)seat.Item,
            seat.Index))];

        return Hand.Start(GameId.New(), seats, (ChipsStack)SmallBlind, (ChipsStack)BigBlind).Value;
    }

    public static Hand FoldedToTheBigBlind()
    {
        Hand hand = Start(1000, 1000, 1000);
        Act(hand, 2, Fold);
        Act(hand, 0, Fold);

        return hand;
    }

    public static Hand WithSidePot()
    {
        Hand hand = Start(50, 1000, 1000);
        Act(hand, 2, Raise, 100);
        Act(hand, 0, AllIn);
        Act(hand, 1, Call);
        CheckDownToTheRiver(hand, 1, 2);

        return hand;
    }

    public static ParticipantId Seat(Hand hand, int position) => hand.Seats[position].ParticipantId;

    public static void Act(Hand hand, int position, HandActionType type, int? amountTo = null) =>
        hand.RecordAction(Seat(hand, position), type, amountTo is { } amount ? (ChipsStack)amount : null)
            .IsSuccess.ShouldBeTrue();

    public static void CheckDownToTheRiver(Hand hand, params int[] actingPositions)
    {
        for (int street = 0; street < 3; street++)
        {
            foreach (int position in actingPositions)
            {
                Act(hand, position, Check);
            }
        }
    }

    public static void DeclareWinner(Hand hand, int potIndex, int position) =>
        hand.DeclareWinners([new HandPotWinner(potIndex, Seat(hand, position))]).IsSuccess.ShouldBeTrue();
}
