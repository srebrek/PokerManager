using Identity.Abstractions;
using Microsoft.AspNetCore.Identity;
using Shared.Domain;

namespace Identity.Infrastructure;

internal sealed class UserAccountService(
    UserManager<User> userManager,
    SignInManager<User> signInManager) : IUserAccountService
{
    public async Task<Result<Guid>> RegisterAsync(string email, string password)
    {
        User user = User.Create(email);

        IdentityResult result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            IdentityError identityError = result.Errors.First();
            Error error = identityError.Code is "DuplicateUserName" or "DuplicateEmail"
                ? IdentityErrors.DuplicateEmail
                : IdentityErrors.RegistrationFailed(identityError.Description);
            return Result.Failure<Guid>(error);
        }

        return Result.Success(user.Id);
    }

    public async Task<Result> LoginAsync(string email, string password, bool rememberMe)
    {
        SignInResult result = await signInManager.PasswordSignInAsync(
            email,
            password,
            rememberMe,
            lockoutOnFailure: true);

        if (result.IsLockedOut)
        {
            return Result.Failure(IdentityErrors.AccountLockedOut);
        }

        if (!result.Succeeded)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        return Result.Success();
    }

    public async Task LogoutAsync()
    {
        await signInManager.SignOutAsync();
    }
}
