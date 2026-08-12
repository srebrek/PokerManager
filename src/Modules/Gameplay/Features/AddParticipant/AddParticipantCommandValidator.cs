using FluentValidation;

namespace Gameplay.Features.AddParticipant;

internal sealed class AddParticipantCommandValidator : AbstractValidator<AddParticipantCommand>
{
    public AddParticipantCommandValidator()
    {
        RuleFor(c => c.GameId)
            .NotEmpty();

        RuleFor(c => c.Name)
            .NotEmpty();
    }
}
