using Contracts.Api.Gameplay;
using FluentValidation.TestHelper;
using Gameplay.Features.DeclareWinners;

namespace IntegrationTests.Gameplay;

public sealed class DeclareWinnersCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly DeclareWinnersCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId, 0, ValidId)]
    [InlineData(ValidId, EmptyId, 0, ValidId)]
    [InlineData(ValidId, ValidId, -1, ValidId)]
    [InlineData(ValidId, ValidId, 0, EmptyId)]
    [InlineData(ValidId, ValidId, 0, null)]
    public void DeclareWinnersCommandValidator_InvalidInput_Fails(
        string handId,
        string actingParticipantId,
        int potIndex,
        string? winnerParticipantId)
    {
        // Arrange
        DeclareWinnersCommand command = new(
            Guid.Parse(handId),
            Guid.Parse(actingParticipantId),
            winnerParticipantId is null ? [] : [new PotWinner(potIndex, Guid.Parse(winnerParticipantId))]);

        // Act
        TestValidationResult<DeclareWinnersCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void DeclareWinnersCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        DeclareWinnersCommand command = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            [new PotWinner(0, Guid.NewGuid()), new PotWinner(0, Guid.NewGuid()), new PotWinner(1, Guid.NewGuid())]);

        // Act
        TestValidationResult<DeclareWinnersCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
