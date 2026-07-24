using System.Net;
using System.Net.Http.Json;
using Contracts.Api.Authentication;
using Identity.Infrastructure;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace Integration.Tests.Identity;

public sealed class RegisterUserIntegrationTests(ApiFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Register_ShouldCreateUserInDatabase()
    {
        // Arrange
        RegisterRequest request = new(
            $"user-{Guid.NewGuid():N}@example.com",
            "VeryStrongPassword123!");

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            "identity/register",
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        await ExecuteWithContextAsync<IdentityDbContext>(async context =>
        {
            User? createdUser = await context.Users
                .SingleOrDefaultAsync(u => u.Email == request.Email, CancellationToken);

            createdUser.ShouldNotBeNull();
            createdUser.UserName.ShouldBe(request.Email);
            createdUser.PasswordHash.ShouldNotBeNullOrWhiteSpace();
        });
    }

    [Fact]
    public async Task Register_ShouldNotSetAuthCookie()
    {
        // Arrange
        using HttpClient noCookieClient = WithAntiCsrfHeader(Factory.CreateClientNoCookies());
        RegisterRequest request = new(
            $"user-{Guid.NewGuid():N}@example.com",
            "VeryStrongPassword123!");

        // Act
        using HttpResponseMessage response = await noCookieClient.PostAsJsonAsync(
            "identity/register",
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        bool hasAuthCookie = response.Headers.TryGetValues("Set-Cookie", out IEnumerable<string>? cookies)
            && cookies.Any(c => c.StartsWith($"{IdentityCookies.AuthCookieName}=", StringComparison.Ordinal));

        hasAuthCookie.ShouldBeFalse();
    }

    [Fact]
    public async Task Register_WithInvalidPassword_ShouldReturnBadRequestAndNotCreateUser()
    {
        // Arrange
        RegisterRequest request = new(
            "invalid-user@example.com",
            "abc");

        // Act
        using HttpResponseMessage response = await Client.PostAsJsonAsync(
            "identity/register",
            request,
            CancellationToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        await ExecuteWithContextAsync<IdentityDbContext>(async context =>
        {
            bool userExists = await context.Users.AnyAsync(u => u.Email == request.Email, CancellationToken);
            userExists.ShouldBeFalse();
        });
    }
}
