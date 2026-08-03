using Identity.Abstractions;
using Shared.Domain;

namespace Identity.Features.Login;

internal sealed class LoginCommandHandler(IUserAccountService userAccountService)
{
    public async Task<Result> Handle(LoginCommand command, CancellationToken ct)
    {
        return await userAccountService.LoginAsync(command.Email, command.Password, command.RememberMe);
    }
}
