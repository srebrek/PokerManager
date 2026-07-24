using System.Net;
using System.Net.Http.Json;
using Contracts.Api.Authentication;
using Identity.Infrastructure;
using Shouldly;

namespace Integration.Tests.Identity;

public sealed class LoginIntegrationTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly RegisterRequest s_validRegisterRequest = new(
        "login-test@example.com",
        "VeryStrongPassword123!");

    [Fact]
    public async Task Login_WithRememberMe_ShouldSetPersistentAuthCookie()
    {
        // Arrange
        using HttpResponseMessage registerResponse = await Client.PostAsJsonAsync(
            "identity/register", s_validRegisterRequest, CancellationToken);
        registerResponse.EnsureSuccessStatusCode();

        using HttpClient noCookieClient = WithAntiCsrfHeader(Factory.CreateClientNoCookies());
        LoginRequest loginRequest = new(s_validRegisterRequest.Email, s_validRegisterRequest.Password, true);

        // Act
        using HttpResponseMessage response = await noCookieClient.PostAsJsonAsync(
            "identity/login",
            loginRequest,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies).ShouldBeTrue();
        string authCookie = cookies!.Single(c =>
            c.StartsWith($"{IdentityCookies.AuthCookieName}=", StringComparison.Ordinal));
        authCookie.ToUpperInvariant().ShouldContain("EXPIRES=");
    }

    [Fact]
    public async Task Login_WithoutRememberMe_ShouldSetSessionAuthCookie()
    {
        // Arrange
        using HttpResponseMessage registerResponse = await Client.PostAsJsonAsync(
            "identity/register", s_validRegisterRequest, CancellationToken);
        registerResponse.EnsureSuccessStatusCode();

        using HttpClient noCookieClient = WithAntiCsrfHeader(Factory.CreateClientNoCookies());
        LoginRequest loginRequest = new(s_validRegisterRequest.Email, s_validRegisterRequest.Password, false);

        // Act
        using HttpResponseMessage response = await noCookieClient.PostAsJsonAsync(
            "identity/login",
            loginRequest,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies).ShouldBeTrue();
        string authCookie = cookies!.Single(c =>
            c.StartsWith($"{IdentityCookies.AuthCookieName}=", StringComparison.Ordinal));
        authCookie.ToUpperInvariant().ShouldNotContain("EXPIRES=");
    }

    [Fact]
    public async Task Login_WithInvalidCredentials_ShouldReturnProblem()
    {
        // Arrange
        LoginRequest request = new("nonexistent@example.com", "WrongPassword1!", false);

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            "identity/login",
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
