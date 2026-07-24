using Contracts.IntegrationEvents.Identity;
using Identity.Abstractions;
using Shared.Domain;

namespace Identity.Features.Register;

public sealed class RegisterCommandHandler(IUserAccountService userAccountService)
{
    public async Task<(Result, UserRegisteredIntegrationEvent?)> Handle(RegisterCommand command)
    {
        Result<Guid> result = await userAccountService.RegisterAsync(command.Email, command.Password);

        if (result.IsFailure)
        {
            return (Result.Failure(result.Error), null);
        }

        return (Result.Success(), new UserRegisteredIntegrationEvent(result.Value, command.Email));
    }
}
