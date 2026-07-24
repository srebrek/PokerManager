using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Contracts.Api.Authentication;
using Microsoft.AspNetCore.Components.Authorization;

namespace Web.Frontend.Features.Authentication;

internal sealed partial class CookieAuthenticationStateProvider(
    HttpClient httpClient,
    ILogger<CookieAuthenticationStateProvider> logger)
    : AuthenticationStateProvider
{
    private static readonly AuthenticationState s_anonymous = new(new ClaimsPrincipal());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(new Uri("identity/me", UriKind.Relative));

            if (response.StatusCode is HttpStatusCode.Unauthorized)
            {
                return s_anonymous;
            }

            response.EnsureSuccessStatusCode();

            CurrentUserResponse? user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

            if (user is null)
            {
                return s_anonymous;
            }

            return new AuthenticationState(CreatePrincipal(user));
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogFailedToRetrieveAuthState(ex);
            return s_anonymous;
        }
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsPrincipal CreatePrincipal(CurrentUserResponse user)
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Email),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "cookie"));
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Failed to retrieve authentication state.")]
    public partial void LogFailedToRetrieveAuthState(Exception ex);
}
