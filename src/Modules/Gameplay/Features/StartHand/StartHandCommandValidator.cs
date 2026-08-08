using FluentValidation;

namespace Gameplay.Features.StartHand;

internal sealed class StartHandCommandValidator : AbstractValidator<StartHandCommand>
{
    public StartHandCommandValidator()
    {
        RuleFor(c => c.GameId)
            .NotEmpty();

        RuleFor(c => c.ActingParticipantId)
            .NotEmpty();
    }
}
