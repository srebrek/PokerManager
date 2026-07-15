using Shared.Domain;

namespace Identity.Events;

public sealed record UserRegisteredIntegrationEvent(Guid UserId, string Email) : IIntegrationEvent;
