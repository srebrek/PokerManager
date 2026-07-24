using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Web.Frontend;
using Web.Frontend.Features.Authentication;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

// The Gateway serves both the WASM app and the API under a single origin.
// All API calls go through the /api prefix (stripped by the Gateway before forwarding).
// BaseAddress = current browser origin + "api/" so that relative URIs like
// "identity/me" resolve to "/api/identity/me" without changing any call sites.
builder.Services.AddTransient<CookieHandler>();
builder.Services.AddHttpClient("Backend", client =>
        client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/"))
    .AddHttpMessageHandler<CookieHandler>();
builder.Services.AddScoped(sp =>
    sp.GetRequiredService<IHttpClientFactory>().CreateClient("Backend"));

builder.Services.AddScoped<CookieAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<CookieAuthenticationStateProvider>());

await builder.Build().RunAsync();
