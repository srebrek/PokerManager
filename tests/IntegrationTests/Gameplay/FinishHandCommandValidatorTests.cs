using FluentValidation.TestHelper;
using Gameplay.Features.FinishHand;

namespace IntegrationTests.Gameplay;

public sealed class FinishHandCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly FinishHandCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId)]
    [InlineData(ValidId, EmptyId)]
    public void FinishHandCommandValidator_InvalidInput_Fails(string handId, string actingParticipantId)
    {
        // Arrange
        FinishHandCommand command = new(Guid.Parse(handId), Guid.Parse(actingParticipantId));

        // Act
        TestValidationResult<FinishHandCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void FinishHandCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        FinishHandCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        TestValidationResult<FinishHandCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
