namespace Identity.Features.Login;

public sealed record LoginCommand(string Email, string Password, bool RememberMe);
