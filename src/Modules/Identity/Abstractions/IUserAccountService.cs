using Shared.Domain;

namespace Identity.Abstractions;

public interface IUserAccountService
{
    Task<Result<Guid>> RegisterAsync(string email, string password);

    Task<Result> LoginAsync(string email, string password, bool rememberMe);

    Task LogoutAsync();
}
