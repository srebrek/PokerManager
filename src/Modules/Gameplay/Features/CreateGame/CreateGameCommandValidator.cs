using FluentValidation;

namespace Gameplay.Features.CreateGame;

internal sealed class CreateGameCommandValidator : AbstractValidator<CreateGameCommand>
{
    public CreateGameCommandValidator()
    {
        RuleFor(c => c.HostName)
            .NotEmpty();
    }
}

