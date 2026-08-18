IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<IResourceWithConnectionString> database;

if (builder.ExecutionContext.IsRunMode)
{
    database = builder
        .AddPostgres("db-server", password: builder.AddParameter("password", "password"), port: 5433)
        .WithImageTag("18")
        .AddDatabase("PokerManager-db");
}
else
{
    database = builder.AddConnectionString("PokerManager-db");
}

// apiservice is the single browser-facing endpoint: it serves both the API
// and the Blazor WASM frontend (proxied to webfrontend in dev, static files in prod).
IResourceBuilder<ProjectResource> apiService = builder
    .AddProject<Projects.Api_Bootstrapper>("apiservice")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithEnvironment("ASPNETCORE_PREVENTHOSTINGSTARTUP", "true"); // Required for frontend hot reload

// The standalone WASM dev server exists only during development (hot reload, debug).
// In publish mode the API serves the published WASM static assets itself.
if (builder.ExecutionContext.IsRunMode)
{
    IResourceBuilder<ProjectResource> webFrontend = builder
        .AddProject<Projects.Web_Frontend>("webfrontend");

    apiService.WithReference(webFrontend).WaitFor(webFrontend);
}

await builder.Build().RunAsync();
