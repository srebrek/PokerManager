using Shared.Domain;

namespace Identity.Events;

internal sealed record UserRegisteredIntegrationEvent(Guid UserId, string Email) : IIntegrationEvent;
