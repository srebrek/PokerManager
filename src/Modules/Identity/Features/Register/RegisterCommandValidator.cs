using FluentValidation;

namespace Identity.Features.Register;

internal sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress();

        // Password policy itself (length, character classes) is enforced by ASP.NET Identity
        // options in IdentityModule — the single source of truth. Don't duplicate it here.
        RuleFor(c => c.Password)
            .NotEmpty();
    }
}
