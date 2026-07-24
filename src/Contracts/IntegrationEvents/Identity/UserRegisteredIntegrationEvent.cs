namespace Contracts.IntegrationEvents.Identity;

public sealed record UserRegisteredIntegrationEvent(Guid UserId, string Email) : IIntegrationEvent;
