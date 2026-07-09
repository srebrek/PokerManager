using Shared.Domain;

namespace Identity.Infrastructure;

internal static class IdentityErrors
{
    public static readonly Error DuplicateEmail =
        Error.Conflict("Identity.Registration.DuplicateEmail", "An account with this email address already exists.");

    public static Error RegistrationFailed(string description) =>
        Error.Problem("Identity.Registration.RegistrationFailed", description);

    public static readonly Error InvalidCredentials =
        Error.Problem("Identity.Login.InvalidCredentials", "Invalid credentials.");

    public static readonly Error AccountLockedOut =
        Error.Problem("Identity.Login.LockedOut", "Account is temporarily locked. Try again later.");
}
