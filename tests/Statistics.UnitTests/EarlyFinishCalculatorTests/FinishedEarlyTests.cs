using Statistics.Domain.Entities;
using Statistics.Domain.Services;
using Statistics.Domain.ValueObjects;

namespace Statistics.UnitTests.EarlyFinishCalculatorTests;

public sealed class FinishedEarlyTests
{
    private static readonly Guid s_seatOne = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid s_seatTwo = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid s_seatThree = Guid.Parse("00000000-0000-0000-0000-000000000003");

    [Fact]
    public void FinishedEarly_NobodyFolded_IsFalse()
    {
        bool result = EarlyFinishCalculator.FinishedEarly([
            Action(s_seatOne, HandActionType.Check),
            Action(s_seatTwo, HandActionType.Check),
            Action(s_seatThree, HandActionType.Check)]);

        result.ShouldBeFalse();
    }

    [Fact]
    public void FinishedEarly_AllButOneFolded_IsTrue()
    {
        bool result = EarlyFinishCalculator.FinishedEarly([
            Action(s_seatOne, HandActionType.Fold),
            Action(s_seatTwo, HandActionType.Fold),
            Action(s_seatThree, HandActionType.PostBigBlind)]);

        result.ShouldBeTrue();
    }

    [Fact]
    public void FinishedEarly_OneOfThreeFolded_IsFalse()
    {
        bool result = EarlyFinishCalculator.FinishedEarly([
            Action(s_seatOne, HandActionType.Fold),
            Action(s_seatTwo, HandActionType.Call),
            Action(s_seatThree, HandActionType.Check)]);

        result.ShouldBeFalse();
    }

    [Fact]
    public void FinishedEarly_AllInsAreNotFolds_IsFalse()
    {
        bool result = EarlyFinishCalculator.FinishedEarly([
            Action(s_seatOne, HandActionType.AllIn),
            Action(s_seatTwo, HandActionType.Call)]);

        result.ShouldBeFalse();
    }

    private static HandAction Action(Guid participantId, HandActionType type) => new(
        Guid.NewGuid(),
        0,
        participantId,
        type,
        null,
        DateTimeOffset.UnixEpoch);
}
