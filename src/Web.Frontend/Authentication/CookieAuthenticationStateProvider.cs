using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Contracts.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Web.Frontend.Authentication;

internal sealed partial class CookieAuthenticationStateProvider(
    HttpClient httpClient,
    IJSRuntime jsRuntime,
    ILogger<CookieAuthenticationStateProvider> logger)
    : AuthenticationStateProvider
{
    // PWA offline support: the last confirmed user is cached so the app can keep
    // rendering an authenticated UI while offline. The API still authorizes every
    // request with the cookie once connectivity returns, so this is display-only state.
    private const string CachedUserStorageKey = "pokermanager.last-user";

    private static readonly AuthenticationState s_anonymous = new(new ClaimsPrincipal());

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(
                new Uri("identity/me", UriKind.Relative));

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                await ClearCachedUserAsync();
                return s_anonymous;
            }

            response.EnsureSuccessStatusCode();

            CurrentUserResponse? user = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();

            if (user is null)
            {
                return s_anonymous;
            }

            await CacheUserAsync(user);
            return new AuthenticationState(CreatePrincipal(user, authenticationType: "cookie"));
        }
        catch (HttpRequestException ex)
        {
            // Network unreachable — likely offline. Fall back to the cached user, if any.
            LogFailedToRetrieveAuthState(ex);
            CurrentUserResponse? cachedUser = await ReadCachedUserAsync();
            return cachedUser is null
                ? s_anonymous
                : new AuthenticationState(CreatePrincipal(cachedUser, authenticationType: "cookie-offline"));
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

    private static ClaimsPrincipal CreatePrincipal(CurrentUserResponse user, string authenticationType)
    {
        Claim[] claims =
        [
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Name, user.Email),
        ];

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType));
    }

    private async Task CacheUserAsync(CurrentUserResponse user)
    {
        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem", CachedUserStorageKey, JsonSerializer.Serialize(user));
    }

    private async Task ClearCachedUserAsync()
    {
        await jsRuntime.InvokeVoidAsync("localStorage.removeItem", CachedUserStorageKey);
    }

    private async Task<CurrentUserResponse?> ReadCachedUserAsync()
    {
        try
        {
            string? json = await jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem", CachedUserStorageKey);

            return json is null ? null : JsonSerializer.Deserialize<CurrentUserResponse>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Error,
        Message = "Failed to retrieve authentication state.")]
    public partial void LogFailedToRetrieveAuthState(Exception ex);
}
