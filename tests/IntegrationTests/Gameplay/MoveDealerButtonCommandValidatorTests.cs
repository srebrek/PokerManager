using FluentValidation.TestHelper;
using Gameplay.Features.MoveDealerButton;

namespace IntegrationTests.Gameplay;

public sealed class MoveDealerButtonCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly MoveDealerButtonCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId, ValidId)]
    [InlineData(ValidId, EmptyId, ValidId)]
    [InlineData(ValidId, ValidId, EmptyId)]
    public void MoveDealerButtonCommandValidator_InvalidInput_Fails(
        string gameId,
        string actingParticipantId,
        string dealerParticipantId)
    {
        // Arrange
        MoveDealerButtonCommand command = new(
            Guid.Parse(gameId),
            Guid.Parse(actingParticipantId),
            Guid.Parse(dealerParticipantId));

        // Act
        TestValidationResult<MoveDealerButtonCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void MoveDealerButtonCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        MoveDealerButtonCommand command = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        TestValidationResult<MoveDealerButtonCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
