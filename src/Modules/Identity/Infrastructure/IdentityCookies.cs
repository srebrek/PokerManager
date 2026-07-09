using Microsoft.AspNetCore.Http;

namespace Identity.Infrastructure;

internal static class IdentityCookies
{
    public const string AuthCookieName = "pokermanager.auth";
    public const bool AuthCookieHttpOnly = true;
    public const CookieSecurePolicy AuthCookieSecure = CookieSecurePolicy.Always;
    public const SameSiteMode AuthCookieSameSite = SameSiteMode.Lax;
}
