using Contracts.IntegrationEvents.Identity;
using Identity.Abstractions;
using Identity.Infrastructure.Data;
using Shared.Domain;
using Wolverine.EntityFrameworkCore;

namespace Identity.Features.Register;

internal sealed class RegisterCommandHandler(
    IDbContextOutbox<IdentityDbContext> outbox,
    IUserAccountService userAccountService)
{
    public async Task<Result> Handle(RegisterCommand command, CancellationToken ct)
    {
        await outbox.DbContext.Database.BeginTransactionAsync(ct);

        Result<Guid> result = await userAccountService.RegisterAsync(command.Email, command.Password);
        if (result.IsFailure)
        {
            return Result.Failure(result.Error);
        }

        await outbox.PublishAsync(new UserRegisteredIntegrationEvent(result.Value, command.Email));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return Result.Success();
    }
}
