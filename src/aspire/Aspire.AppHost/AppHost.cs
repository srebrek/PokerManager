IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres = builder
    .AddPostgres("db-server")
    .WithImageTag("18")
    .PublishAsConnectionString();

IResourceBuilder<PostgresDatabaseResource> database = postgres
    .AddDatabase("PokerManager-db");

// apiservice is the single browser-facing endpoint: it serves both the API
// and the Blazor WASM frontend (proxied to webfrontend in dev, static files in prod).
IResourceBuilder<ProjectResource> apiService = builder
    .AddProject<Projects.Api_Bootstrapper>("apiservice")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
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
