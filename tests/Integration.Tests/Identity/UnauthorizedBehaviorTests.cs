using System.Net;
using Contracts.Api.Http;
using Identity.Infrastructure;
using Shouldly;

namespace Integration.Tests.Identity;

public sealed class UnauthorizedBehaviorTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task RequestToProtectedEndpoint_WithoutCookie_ShouldReturn_401()
    {
        using HttpResponseMessage response = await Client.GetAsync(
            new Uri("identity/me", UriKind.Relative),
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RequestToProtectedEndpoint_WithInvalidCookie_ShouldReturn_401()
    {
        Client.DefaultRequestHeaders.Add("Cookie", $"{IdentityCookies.AuthCookieName}=invalid-token");

        using HttpResponseMessage response = await Client.GetAsync(
            new Uri("identity/me", UriKind.Relative),
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StateChangingRequest_WithoutAntiCsrfHeader_ShouldReturn_403()
    {
        Client.DefaultRequestHeaders.Remove(HttpDefenseHeaders.AntiCsrf);

        using HttpResponseMessage response = await Client.PostAsync(
            new Uri("identity/logout", UriKind.Relative),
            content: null,
            CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
