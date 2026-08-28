using Api.Bootstrapper;
using Aspire.ServiceDefaults;
using Gameplay;
using Identity;
using Identity.Abstractions;
using JasperFx;
using JasperFx.CodeGeneration;
using JasperFx.Core;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Shared.Domain;
using Shared.Infrastructure;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;
using Statistics;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Loads the static web assets manifest outside Development too - without it a Production run from
// build output (E2E) serves no Blazor files. No-op in a published app, which has no manifest.
builder.WebHost.UseStaticWebAssets();

string databaseConnectionString = builder.Configuration.GetConnectionString(DatabaseConstants.ConnectionStringName)
    ?? throw new InvalidOperationException(
        $"Connection string '{DatabaseConstants.ConnectionStringName}' is not configured.");

builder.Services
    .AddHealthChecks()
    .AddNpgSql(databaseConnectionString);

builder.AddServiceDefaults();
builder.AddDatabaseAndMessagingTelemetry();

// YARP so hotreload and dotnet watch work in dev
builder.Services.AddHttpForwarderWithServiceDiscovery();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddIdentityModule(builder.Configuration);
builder.Services.AddGameplayModule(builder.Configuration);
builder.Services.AddStatisticsModule(builder.Configuration);

builder.Host.UseWolverine(opts =>
{
    if (builder.Environment.IsProduction())
    {
        opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
    }

    opts.PersistMessagesWithPostgresql(databaseConnectionString);
    opts.Durability.MessageStorageSchemaName = "wolverine";
    opts.Policies.UseDurableLocalQueues();
    opts.UseEntityFrameworkCoreTransactions();
    opts.PublishDomainEventsFromEntityFrameworkCore<IHasDomainEvents, IDomainEvent>(x => x.Events);
    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IUserAccountService>();
    opts.OnException<DbUpdateConcurrencyException>()
        .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
    opts.Discovery.IncludeAssembly(typeof(IdentityModule).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GameplayModule).Assembly);
    opts.Discovery.IncludeAssembly(typeof(StatisticsModule).Assembly);
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAntiCsrf();
app.UseRequestContext();
app.UseExceptionHandler();
app.UseIdentityModule();
app.MapDefaultEndpoints();

RouteGroupBuilder apiGroup = app.MapGroup("api");
apiGroup.AddEndpointFilter<TelemetryFilter>();
app.MapEndpoints(apiGroup);

if (app.Environment.IsDevelopment())
{
    // YARP so hotreload and dotnet watch work in dev
    app.MapForwarder("/{**catch-all}", "http://webfrontend");
}
else
{
    app.MapStaticAssets();
    app.MapFallbackToFile("index.html");
}

// For codegen
return await app.RunJasperFxCommands(args);
