using FluentValidation.TestHelper;
using Gameplay.Features.StartHand;

namespace IntegrationTests.Gameplay;

public sealed class StartHandCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly StartHandCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId)]
    [InlineData(ValidId, EmptyId)]
    public void StartHandCommandValidator_InvalidInput_Fails(string gameId, string actingParticipantId)
    {
        // Arrange
        StartHandCommand command = new(Guid.Parse(gameId), Guid.Parse(actingParticipantId));

        // Act
        TestValidationResult<StartHandCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void StartHandCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        StartHandCommand command = new(Guid.NewGuid(), Guid.NewGuid());

        // Act
        TestValidationResult<StartHandCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
