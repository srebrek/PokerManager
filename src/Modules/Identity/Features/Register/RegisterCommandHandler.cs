using Contracts.IntegrationEvents.Identity;
using Identity.Abstractions;
using Identity.Infrastructure.Data;
using Shared.Domain;
using Wolverine.EntityFrameworkCore;

namespace Identity.Features.Register;

internal sealed class RegisterCommandHandler(
    IDbContextOutbox<IdentityDbContext> outbox,
    IUserAccountService userAccountService,
    TimeProvider timeProvider)
{
    public async Task<Result> Handle(RegisterCommand command, CancellationToken ct)
    {
        await outbox.DbContext.Database.BeginTransactionAsync(ct);

        Result<Guid> result = await userAccountService.RegisterAsync(command.Email, command.Password);
        if (!result.TryGetValue(out Guid userId, out Error? error))
        {
            return error;
        }

        await outbox.PublishAsync(new UserRegisteredIntegrationEvent(
            Guid.NewGuid(),
            timeProvider.GetUtcNow(),
            userId,
            command.Email));

        await outbox.SaveChangesAndFlushMessagesAsync(ct);

        return Result.Success();
    }
}
