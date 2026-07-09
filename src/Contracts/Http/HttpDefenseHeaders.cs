namespace Contracts.Http;

/// <summary>
/// Headers shared between the WASM client and the API.
/// </summary>
public static class HttpDefenseHeaders
{
    /// <summary>
    /// Anti-CSRF marker header. The API rejects state-changing requests without it;
    /// the client attaches it to every request. Cross-site attackers cannot set custom
    /// headers without a CORS preflight, which blocks CSRF on cookie-authenticated endpoints.
    /// </summary>
    public const string AntiCsrf = "X-CSRF";

    /// <summary>Required value of the <see cref="AntiCsrf"/> header.</summary>
    public const string AntiCsrfValue = "1";
}
