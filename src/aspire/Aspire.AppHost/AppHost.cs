using Microsoft.Extensions.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

bool production = !builder.Environment.IsDevelopment();

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

IResourceBuilder<ProjectResource> migrations = builder
    .AddProject<Projects.Api_Bootstrapper>("migrations", launchProfileName: null)
    .WithArgs("migrate")
    .WithReference(database)
    .WaitFor(database);

// apiservice is the single browser-facing endpoint: it serves both the API
// and the Blazor WASM frontend (proxied to webfrontend in dev, static files in prod).
IResourceBuilder<ProjectResource> apiService = builder
    .AddProject<Projects.Api_Bootstrapper>("apiservice")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", production ? "Production" : "Development");

// The standalone WASM dev server exists only during development (hot reload, debug).
// In publish mode the API serves the published WASM static assets itself.
if (!production)
{
    IResourceBuilder<ProjectResource> webFrontend = builder
        .AddProject<Projects.Web_Frontend>("webfrontend");

    apiService
        .WithReference(webFrontend)
        .WaitFor(webFrontend)
        .WithEnvironment("ASPNETCORE_PREVENTHOSTINGSTARTUP", "true"); // Required for frontend hot reload
}

await builder.Build().RunAsync();
