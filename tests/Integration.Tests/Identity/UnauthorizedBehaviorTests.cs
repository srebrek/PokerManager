using Contracts.Api.Http;
using Identity.Infrastructure;

namespace Integration.Tests.Identity;

public sealed class UnauthorizedBehaviorTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task RequestToProtectedEndpoint_NoCookie_Returns401()
    {
        using HttpResponseMessage response = await Client.GetAsync(
            new Uri("identity/me", UriKind.Relative),
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestToProtectedEndpoint_InvalidCookie_Returns401()
    {
        Client.DefaultRequestHeaders.Add("Cookie", $"{IdentityCookies.AuthCookieName}=invalid-token");

        using HttpResponseMessage response = await Client.GetAsync(
            new Uri("identity/me", UriKind.Relative),
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StateChangingRequest_NoAntiCsrfHeader_Returns403()
    {
        Client.DefaultRequestHeaders.Remove(HttpDefenseHeaders.AntiCsrf);

        using HttpResponseMessage response = await Client.PostAsync(
            new Uri("identity/logout", UriKind.Relative),
            content: null,
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
