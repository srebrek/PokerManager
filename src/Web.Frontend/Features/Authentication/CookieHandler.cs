using Contracts.Api.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Web.Frontend.Features.Authentication;

// TODO: Consider removing cookies as they should be provided by default
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
