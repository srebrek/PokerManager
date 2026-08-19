using FluentValidation.TestHelper;
using Gameplay.Features.SetParticipantSittingOut;

namespace IntegrationTests.Gameplay;

public sealed class SetParticipantSittingOutCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly SetParticipantSittingOutCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId, ValidId)]
    [InlineData(ValidId, EmptyId, ValidId)]
    [InlineData(ValidId, ValidId, EmptyId)]
    public void SetParticipantSittingOutCommandValidator_InvalidInput_Fails(
        string gameId,
        string actingParticipantId,
        string targetParticipantId)
    {
        // Arrange
        SetParticipantSittingOutCommand command = new(
            Guid.Parse(gameId),
            Guid.Parse(actingParticipantId),
            Guid.Parse(targetParticipantId),
            true);

        // Act
        TestValidationResult<SetParticipantSittingOutCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void SetParticipantSittingOutCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        SetParticipantSittingOutCommand command = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), true);

        // Act
        TestValidationResult<SetParticipantSittingOutCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
