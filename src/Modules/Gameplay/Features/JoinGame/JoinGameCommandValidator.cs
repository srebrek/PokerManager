using FluentValidation;

namespace Gameplay.Features.JoinGame;

internal sealed class JoinGameCommandValidator : AbstractValidator<JoinGameCommand>
{
    public JoinGameCommandValidator()
    {
        RuleFor(c => c.ParticipantName)
            .NotEmpty();

        RuleFor(c => c.JoinCode)
            .Length(6);
    }
}
