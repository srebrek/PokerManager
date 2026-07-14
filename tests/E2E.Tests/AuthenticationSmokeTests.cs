using System.Net.Http.Json;
using Contracts.Authentication;
using Contracts.Http;
using Shouldly;

namespace E2E.Tests;

public sealed class AuthenticationSmokeTests(AspireFixture fixture, ITestOutputHelper output)
    : AspireIntegrationTestBase(fixture, output)
{
    [Fact]
    public async Task RegisterLoginMe_RoundTrip_ShouldSucceed()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        using HttpClient client = CreateApiClient();
        client.DefaultRequestHeaders.Add(HttpDefenseHeaders.AntiCsrf, HttpDefenseHeaders.AntiCsrfValue);

        RegisterRequest registerRequest = new("smoke-test@example.com", "VeryStrongPassword123!");
        using HttpResponseMessage registerResponse = await client.PostAsJsonAsync(
            "identity/register", registerRequest, ct);
        registerResponse.EnsureSuccessStatusCode();

        LoginRequest loginRequest = new(registerRequest.Email, registerRequest.Password, false);
        using HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            "identity/login", loginRequest, ct);
        loginResponse.EnsureSuccessStatusCode();

        CurrentUserResponse? currentUser = await client.GetFromJsonAsync<CurrentUserResponse>(
            new Uri("identity/me", UriKind.Relative), ct);

        currentUser.ShouldNotBeNull();
        currentUser.Email.ShouldBe(registerRequest.Email);
    }
}
