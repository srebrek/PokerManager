using FluentValidation.TestHelper;
using Gameplay.Features.UndoLastAction;

namespace IntegrationTests.Gameplay;

public sealed class UndoLastActionCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly UndoLastActionCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId)]
    [InlineData(ValidId, EmptyId)]
    public void UndoLastActionCommandValidator_InvalidInput_Fails(string handId, string actingParticipantId)
    {
        // Arrange
        UndoLastActionCommand command = new(Guid.Parse(handId), Guid.Parse(actingParticipantId));

        // Act
        TestValidationResult<UndoLastActionCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void UndoLastActionCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        UndoLastActionCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        TestValidationResult<UndoLastActionCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
