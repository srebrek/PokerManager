using FluentValidation;

namespace Identity.Features.Register;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress();

        // Password policy is enforced by ASP.NET Identity
        RuleFor(c => c.Password)
            .NotEmpty();
    }
}
