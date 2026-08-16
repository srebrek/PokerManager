using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Web.Frontend;
using Web.Frontend.Common;
using Web.Frontend.Common.Http;

WebAssemblyHostBuilder builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddMudServices();

// The Gateway serves both the WASM app and the API under a single origin.
// All API calls go through the /api prefix (stripped by the Gateway before forwarding).
// BaseAddress = current browser origin + "api/" so that relative URIs like
// "gameplay/games" resolve to "/api/gameplay/games" without changing any call sites.
builder.Services.AddTransient<ApiRequestHandler>();
builder.Services.AddHttpClient(
        "Backend",
        client => client.BaseAddress = new Uri($"{builder.HostEnvironment.BaseAddress}api/"))
    .AddHttpMessageHandler<ApiRequestHandler>();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Backend"));

builder.Services.AddScoped<IGameplayApi, GameplayApi>();
builder.Services.AddScoped<ParticipantSession>();
builder.Services.AddScoped<GameHubConnection>();

await builder.Build().RunAsync();
