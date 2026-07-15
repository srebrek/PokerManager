using Identity.Events;
using Microsoft.Extensions.Logging;

// Placeholder consumer: logs the event until a real consuming module (e.g. Notifications)
// exists. When one does, move UserRegisteredIntegrationEvent to Contracts and consume there.
namespace Identity.Features.UserRegistered;

public sealed partial class UserRegisteredIntegrationEventHandler(
    ILogger<UserRegisteredIntegrationEventHandler> logger)
{
    public Task Handle(UserRegisteredIntegrationEvent integrationEvent, CancellationToken ct)
    {
        LogUserRegistered(logger, integrationEvent.UserId, integrationEvent.Email);
        return Task.CompletedTask;
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "User registered: {UserId} ({Email}). Placeholder for Notifications module.")]
    private static partial void LogUserRegistered(ILogger logger, Guid userId, string email);
}
