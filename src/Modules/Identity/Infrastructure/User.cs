using Microsoft.AspNetCore.Identity;

namespace Identity.Infrastructure;

// Public due to wolverine
public sealed class User : IdentityUser<Guid>
{
    public static User Create(string email)
    {
        return new User(email);
    }

    private User() { } // AspNetCore.Identity purpose.

    private User(string email)
    {
        Id = Guid.NewGuid();
        Email = email;
        UserName = email;
    }
}
