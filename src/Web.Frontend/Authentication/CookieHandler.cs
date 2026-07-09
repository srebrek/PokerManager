using Contracts.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Web.Frontend.Authentication;

/// <summary>
/// Ensures the browser's fetch API sends the authentication cookie with every request.
/// Required even in the single-origin (BFF) setup so that SameSite=Lax cookies are
/// transmitted correctly by the WASM fetch infrastructure. Also attaches the anti-CSRF
/// marker header required by the API for state-changing requests.
/// </summary>
internal sealed class CookieHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
        request.Headers.TryAddWithoutValidation(HttpDefenseHeaders.AntiCsrf, HttpDefenseHeaders.AntiCsrfValue);
        return base.SendAsync(request, cancellationToken);
    }
}
