using FluentValidation.TestHelper;
using Gameplay.Features.ChangeGameRules;

namespace IntegrationTests.Gameplay;

public sealed class ChangeGameRulesCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly ChangeGameRulesCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId, 25, 50)]
    [InlineData(ValidId, EmptyId, 25, 50)]
    [InlineData(ValidId, ValidId, -1, 50)]
    [InlineData(ValidId, ValidId, 25, -1)]
    [InlineData(ValidId, ValidId, 50, 25)]
    public void ChangeGameRulesCommandValidator_InvalidInput_Fails(
        string gameId,
        string actingParticipantId,
        int smallBlind,
        int bigBlind)
    {
        // Arrange
        ChangeGameRulesCommand command = new(
            Guid.Parse(gameId),
            Guid.Parse(actingParticipantId),
            smallBlind,
            bigBlind);

        // Act
        TestValidationResult<ChangeGameRulesCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void ChangeGameRulesCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        ChangeGameRulesCommand command = new(Guid.NewGuid(), Guid.NewGuid(), 25, 50);

        // Act
        TestValidationResult<ChangeGameRulesCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
