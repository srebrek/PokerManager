using FluentValidation.TestHelper;
using Gameplay.Features.Rebuy;

namespace IntegrationTests.Gameplay;

public sealed class RebuyCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly RebuyCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId, ValidId, 500)]
    [InlineData(ValidId, EmptyId, ValidId, 500)]
    [InlineData(ValidId, ValidId, EmptyId, 500)]
    [InlineData(ValidId, ValidId, ValidId, 0)]
    public void RebuyCommandValidator_InvalidInput_Fails(
        string gameId,
        string actingParticipantId,
        string targetParticipantId,
        int amount)
    {
        // Arrange
        RebuyCommand command = new(
            Guid.Parse(gameId),
            Guid.Parse(actingParticipantId),
            Guid.Parse(targetParticipantId),
            amount);

        // Act
        TestValidationResult<RebuyCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RebuyCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        RebuyCommand command = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), -500);

        // Act
        TestValidationResult<RebuyCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
