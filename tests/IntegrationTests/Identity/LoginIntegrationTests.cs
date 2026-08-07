using Contracts.Api.Authentication;
using Identity.Infrastructure;

namespace IntegrationTests.Identity;

public sealed class LoginIntegrationTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    private static readonly RegisterRequest s_validRegisterRequest = new(
        "login-test@example.com",
        "VeryStrongPassword123!");

    // TODO: add more complex cookie validation
    [Fact]
    public async Task Login_RememberMe_SetsPersistentAuthCookie()
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
    public async Task Login_NoRememberMe_SetsSessionAuthCookie()
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
    public async Task Login_InvalidCredentials_ReturnsProblem()
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
