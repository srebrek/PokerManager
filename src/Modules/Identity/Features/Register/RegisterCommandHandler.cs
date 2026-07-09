using Identity.Abstractions;
using Identity.Events;
using Shared.Domain;
using Wolverine;

namespace Identity.Features.Register;

public sealed class RegisterCommandHandler(IUserAccountService userAccountService)
{
    public async Task<Result> Handle(RegisterCommand command, IMessageContext context)
    {
        Result<Guid> result = await userAccountService.RegisterAsync(command.Email, command.Password);

        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        await context.PublishAsync(new UserRegisteredIntegrationEvent(result.Value, command.Email));

        return Result.Success();
    }
}
