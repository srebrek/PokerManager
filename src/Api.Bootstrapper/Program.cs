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
using Shared.Infrastructure.Messaging;
using Shared.Presentation.Extensions;
using Shared.Presentation.Infrastructure;
using Wolverine;
using Wolverine.EntityFrameworkCore;
using Wolverine.ErrorHandling;
using Wolverine.Postgresql;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

builder.Host.UseWolverine(opts =>
{
    if (builder.Environment.IsProduction())
    {
        opts.CodeGeneration.TypeLoadMode = TypeLoadMode.Static;
    }

    opts.PersistMessagesWithPostgresql(databaseConnectionString);
    opts.Durability.MessageStorageSchemaName = "wolverine";
    opts.Policies.UseDurableLocalQueues();
    opts.Policies.AutoApplyTransactions();
    opts.UseEntityFrameworkCoreTransactions();
    opts.PublishDomainEventsFromEntityFrameworkCore<IHasDomainEvents, IDomainEvent>(x => x.Events);

    opts.MultipleHandlerBehavior = MultipleHandlerBehavior.Separated;
    opts.CodeGeneration.AlwaysUseServiceLocationFor<IUserAccountService>();
    opts.Policies.Add<ValidationMiddlewarePolicy>();
    opts.OnException<DbUpdateConcurrencyException>()
        .RetryWithCooldown(50.Milliseconds(), 100.Milliseconds(), 250.Milliseconds());
    opts.Discovery.IncludeAssembly(typeof(IdentityModule).Assembly);
    opts.Discovery.IncludeAssembly(typeof(GameplayModule).Assembly);
});

WebApplication app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();

    await app.Services.ApplyIdentityMigrationsAsync();
    await app.Services.ApplyGameplayMigrationsAsync();
}

if (app.Environment.IsProduction())
{
    app.UseBlazorFrameworkFiles();
    app.UseStaticFiles();
}

app.UseAntiCsrf();
app.UseRequestContext();
app.UseExceptionHandler();
app.UseIdentityModule();
app.MapDefaultEndpoints();
app.MapEndpoints(app.MapGroup("api"));

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    if (app.Environment.IsDevelopment())
    {
        // YARP so hotreload and dotnet watch work in dev
        app.MapForwarder("/{**catch-all}", "http://webfrontend");
    }
    else
    {
        app.MapFallbackToFile("index.html");
    }
}

// For codegen
return await app.RunJasperFxCommands(args);
