namespace Identity.Features.Login;

internal sealed record LoginCommand(string Email, string Password, bool RememberMe);
