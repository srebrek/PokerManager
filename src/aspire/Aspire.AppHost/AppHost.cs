using Azure.Provisioning.AppContainers;
using Azure.Provisioning.ContainerRegistry;
using Microsoft.Extensions.Hosting;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

bool production = !builder.Environment.IsDevelopment();

IResourceBuilder<Aspire.Hosting.Azure.AzureContainerRegistryResource> containerRegistry = builder
    .AddAzureContainerRegistry("acr")
    .ConfigureInfrastructure(infrastructure =>
    {
        ContainerRegistryService registry = infrastructure
            .GetProvisionableResources()
            .OfType<ContainerRegistryService>()
            .Single();

        registry.Sku = new ContainerRegistrySku { Name = ContainerRegistrySkuName.Standard };
    });

builder.AddAzureContainerAppEnvironment("aca-env")
    .WithAzureContainerRegistry(containerRegistry);

IResourceBuilder<ParameterResource> databaseUsername = builder.AddParameter("db-username", "pokeradmin");

IResourceBuilder<ParameterResource> databasePassword = builder.AddParameter(
    "db-password",
    new GenerateParameterDefault
    {
        MinLength = 24,
        Lower = true,
        Upper = true,
        Numeric = true,
        Special = false,
    },
    secret: true,
    persist: true);

IResourceBuilder<IResourceWithConnectionString> database = builder
    .AddAzurePostgresFlexibleServer("db-server")
    .WithPasswordAuthentication(databaseUsername, databasePassword)
    .RunAsContainer(container => container
        .WithImageTag("16")
        .WithHostPort(5433))
    .AddDatabase("PokerManager-db", databaseName: "pokermanager");

IResourceBuilder<IResource> migrations;

if (builder.ExecutionContext.IsRunMode)
{
    string configuration;
#if DEBUG
    configuration = "Debug";
#elif RELEASE
    configuration = "Release";
#endif
    // debug reason
    migrations = builder
        .AddExecutable(
            "migrations",
            "dotnet",
            "../../Api.Bootstrapper",
            "run",
            "--no-launch-profile",
            "--no-build",
            "--configuration",
            configuration,
            "--",
            "migrate")
        .WithReference(database)
        .WaitFor(database);
}
else
{
    migrations = builder
        .AddProject<Projects.Api_Bootstrapper>("migrations", launchProfileName: null)
        .WithArgs("migrate")
        .WithReference(database)
        .WaitFor(database)
        .PublishAsAzureContainerAppJob();
}

// apiservice is the single browser-facing endpoint: it serves both the API
// and the Blazor WASM frontend (proxied to webfrontend in dev, static files in prod).
IResourceBuilder<ProjectResource> apiService = builder
    .AddProject<Projects.Api_Bootstrapper>("apiservice")
    .WithReference(database)
    .WaitFor(database)
    .WaitForCompletion(migrations)
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", production ? "Production" : "Development")
    .PublishAsAzureContainerApp((_, containerApp) =>
    {
        containerApp.Template.Scale = new ContainerAppScale
        {
            MinReplicas = 0,
            MaxReplicas = 1,
        };

        containerApp.Template.Containers[0].Value!.Resources.Cpu = 0.25;
        containerApp.Template.Containers[0].Value!.Resources.Memory = "0.5Gi";
    });

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
