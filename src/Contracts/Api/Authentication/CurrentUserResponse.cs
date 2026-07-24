namespace Contracts.Api.Authentication;

public sealed record CurrentUserResponse(Guid Id, string Email);
