using Contracts.Api.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Web.Frontend.Common.Http;

internal sealed class ApiRequestHandler : DelegatingHandler
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
