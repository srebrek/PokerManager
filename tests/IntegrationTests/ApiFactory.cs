using Identity.Infrastructure.Data;
using JasperFx.CommandLine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace IntegrationTests;

public class ApiFactory(GlobalDbFixture dbFixture) : WebApplicationFactory<Program>, IAsyncLifetime
{
    static ApiFactory()
    {
        // Wolverine reason
        JasperFxEnvironment.AutoStartHost = true;
    }

    private readonly string _dbName = $"test_db_{Guid.NewGuid():N}";
    private string _connectionString = string.Empty;
    private Respawner _respawner = null!;

    public IHost Host { get; private set; } = null!;

    protected override IHost CreateHost(IHostBuilder builder)
    {
        Host = base.CreateHost(builder);
        return Host;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:PokerManager-db", _connectionString);

        builder.UseEnvironment(TestEnvironment.Name);
    }

    public async ValueTask InitializeAsync()
    {
        _connectionString = dbFixture.GetConnectionString(_dbName);

        await using NpgsqlConnection connection = new(dbFixture.Container.GetConnectionString());
        await connection.OpenAsync();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        await using NpgsqlCommand command = new(
            $"CREATE DATABASE {_dbName} TEMPLATE {GlobalDbFixture.TemplateDbName}",
            connection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        await command.ExecuteNonQueryAsync();

        using IServiceScope scope = Services.CreateScope();
        IdentityDbContext context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        _ = context.Model;

        await using NpgsqlConnection testDbConn = new(_connectionString);
        await testDbConn.OpenAsync();

        _respawner = await Respawner.CreateAsync(testDbConn, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            TablesToIgnore =
            [
                new Table(IdentityDbContext.Schema, "__EFMigrationsHistory"),
                new Table(IdentityDbContext.Schema, "data_protection_keys"),
            ]
        });
    }

    public async Task ResetDatabaseAsync()
    {
        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public HttpClient CreateClientNoCookies()
    {
        return CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
    }

    public new async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await base.DisposeAsync();

            await using NpgsqlConnection connection = new(dbFixture.Container.GetConnectionString());
            await connection.OpenAsync();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
            await using NpgsqlCommand command = new($"DROP DATABASE {_dbName} WITH (FORCE)", connection);
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

            await command.ExecuteNonQueryAsync();
        }
    }
}
