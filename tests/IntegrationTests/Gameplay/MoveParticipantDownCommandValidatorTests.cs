using FluentValidation.TestHelper;
using Gameplay.Features.MoveParticipantDown;

namespace IntegrationTests.Gameplay;

public sealed class MoveParticipantDownCommandValidatorTests
{
    private const string ValidId = "11111111-1111-1111-1111-111111111111";
    private const string EmptyId = "00000000-0000-0000-0000-000000000000";

    private readonly MoveParticipantDownCommandValidator _validator = new();

    [Theory]
    [InlineData(EmptyId, ValidId, ValidId)]
    [InlineData(ValidId, EmptyId, ValidId)]
    [InlineData(ValidId, ValidId, EmptyId)]
    public void MoveParticipantDownCommandValidator_InvalidInput_Fails(
        string gameId,
        string actingParticipantId,
        string targetParticipantId)
    {
        // Arrange
        MoveParticipantDownCommand command = new(
            Guid.Parse(gameId),
            Guid.Parse(actingParticipantId),
            Guid.Parse(targetParticipantId));

        // Act
        TestValidationResult<MoveParticipantDownCommand> result = _validator.TestValidate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void MoveParticipantDownCommandValidator_ValidInput_Succeeds()
    {
        // Arrange
        MoveParticipantDownCommand command = new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        // Act
        TestValidationResult<MoveParticipantDownCommand> result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
