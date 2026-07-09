using Identity.Abstractions;
using Shared.Domain;

namespace Identity.Features.Login;

public sealed class LoginCommandHandler(IUserAccountService userAccountService)
{
    public async Task<Result> Handle(LoginCommand command)
    {
        return await userAccountService.LoginAsync(command.Email, command.Password, command.RememberMe);
    }
}
