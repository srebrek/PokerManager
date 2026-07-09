using Identity;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(Integration.Tests.GlobalDbFixture))]

namespace Integration.Tests;

public class GlobalDbFixture : IAsyncLifetime
{
    public const string TemplateDbName = "template_db";

    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:18").Build();

    public async ValueTask InitializeAsync()
    {
        await Container.StartAsync();

        string masterConnectionString = Container.GetConnectionString();

        await using NpgsqlConnection connection = new(masterConnectionString);
        await connection.OpenAsync();
        await using NpgsqlCommand command = new($"CREATE DATABASE {TemplateDbName}", connection);
        await command.ExecuteNonQueryAsync();

        await MigrateAndSeedTemplateAsync();
    }

    private async Task MigrateAndSeedTemplateAsync()
    {
        string templateConnString = GetConnectionString(TemplateDbName);

        string migrationConnectionString = $"{templateConnString};Pooling=false;";

        ServiceCollection services = new();

        services.AddDbContext<IdentityDbContext>(options => options
            .UseNpgsql(migrationConnectionString)
            .UseSnakeCaseNamingConvention());

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        await serviceProvider.ApplyIdentityMigrationsAsync();
    }

    public string GetConnectionString(string dbName)
    {
        NpgsqlConnectionStringBuilder builder = new(Container.GetConnectionString())
        {
            Database = dbName
        };
        return builder.ConnectionString;
    }

    public async ValueTask DisposeAsync()
    {
        await Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async Task Dispose(bool disposing)
    {
        if (disposing)
        {
            await Container.DisposeAsync();
        }
    }
}
