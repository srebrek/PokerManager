using FluentValidation;

namespace Gameplay.Features.GetGameByJoinCode;

internal sealed class GetGameByJoinCodeQueryValidator : AbstractValidator<GetGameByJoinCodeQuery>
{
    public GetGameByJoinCodeQueryValidator()
    {
        RuleFor(q => q.JoinCode)
            .Length(6);
    }
}
