using Api.Bootstrapper;
using Aspire.ServiceDefaults;
using Identity;
using JasperFx;
using JasperFx.CodeGeneration;
using Scalar.AspNetCore;
using Shared.Domain;
using Shared.Infrastructure;
using Shared.Infrastructure.Messaging;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.Postgresql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

bool isRunningCodegen = args.Contains("codegen");
string? databaseConnectionString = builder.Configuration.GetConnectionString(DatabaseConstants.ConnectionStringName);

if (!isRunningCodegen && databaseConnectionString is null)
{
    throw new InvalidOperationException(
        $"Connection string '{DatabaseConstants.ConnectionStringName}' is not configured.");
}

builder.AddServiceDefaults();
builder.AddDatabaseAndMessagingTelemetry();

// Wire YARP's direct forwarder with Aspire service discovery so that
// "http://webfrontend" resolves at runtime in development mode.
builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddIdentityModule(builder.Configuration);

if (!isRunningCodegen)
{
    builder.Services
        .AddHealthChecks()
        .AddNpgSql(databaseConnectionString!);
}

builder.Host.UseWolverine(opts =>
{
    if (builder.Environment.IsProduction())
    {
        opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
    }

    if (!isRunningCodegen)
    {
        opts.PersistMessagesWithPostgresql(databaseConnectionString!);
        opts.Durability.MessageStorageSchemaName = "wolverine";
        opts.Policies.UseDurableLocalQueues();
        opts.Policies.AutoApplyTransactions();
        opts.UseEntityFrameworkCoreTransactions();
        opts.PublishDomainEventsFromEntityFrameworkCore<Entity>(x => x.Events);
    }

    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
    opts.Policies.Add<ValidationMiddlewarePolicy>();

    opts.Discovery.IncludeAssembly(typeof(IdentityModule).Assembly);
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    await app.Services.ApplyIdentityMigrationsAsync();
}

// Serve the frontend only in real run modes — not under "Testing" (integration tests
// exercise the API in isolation and never hit non-/api routes).
bool serveFrontend = app.Environment.IsDevelopment() || app.Environment.IsProduction();

if (serveFrontend && !app.Environment.IsDevelopment())
{
    // Production: serve the published WASM static assets (including precompressed
    // .br/.gz) directly, before API middleware so static files short-circuit fast.
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();
}

app.UseAntiCsrf();
app.UseRequestContext();
app.UseExceptionHandler();
app.UseIdentityModule();

app.MapDefaultEndpoints();

// All API endpoints are grouped under /api so the single-origin host can
// distinguish them from frontend routes. The WASM client already uses
// BaseAddress = origin + "api/" so no frontend code changes are needed.
app.MapEndpoints(app.MapGroup("api"));

if (serveFrontend)
{
    if (app.Environment.IsDevelopment())
    {
        // Dev: forward everything else to the standalone WASM dev server.
        // This keeps hot reload and WASM debugging working while the API and
        // WASM share a single browser origin (required for HttpOnly cookie auth).
        // ASPNETCORE_PREVENTHOSTINGSTARTUP=true on this process (set in AppHost.cs)
        // prevents dotnet watch from injecting its browser-refresh script here —
        // the WASM dev server handles its own refresh, and we must not overwrite it.
        app.MapForwarder("/{**catch-all}", "http://webfrontend");
    }
    else
    {
        // Production: the dev server doesn't exist; fall back to index.html for
        // all unmatched routes so Blazor's client-side router handles them.
        app.MapFallbackToFile("index.html");
    }
}

if (isRunningCodegen)
{
    return await app.RunJasperFxCommands(args);
}

await app.RunAsync();
return 0;
