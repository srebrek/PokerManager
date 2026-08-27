namespace Contracts.IntegrationEvents.Identity;

public sealed record UserRegisteredIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAt,
    Guid UserId,
    string Email) : IIntegrationEvent;
