using Gameplay;
using Gameplay.Infrastructure.Data;
using Identity;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Statistics;
using Statistics.Infrastructure.Data;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(IntegrationTests.GlobalDbFixture))]

namespace IntegrationTests;

public class GlobalDbFixture : IAsyncLifetime
{
    public const string TemplateDbName = "template_db";

    public PostgreSqlContainer Container { get; } = new PostgreSqlBuilder("postgres:18")
        .WithCommand("-c", "max_connections=300")
        .Build();

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

        services.AddDbContext<GameplayDbContext>(options => options
            .UseNpgsql(migrationConnectionString)
            .UseSnakeCaseNamingConvention());

        services.AddDbContext<StatisticsDbContext>(options => options
            .UseNpgsql(migrationConnectionString)
            .UseSnakeCaseNamingConvention());

        ServiceProvider serviceProvider = services.BuildServiceProvider();

        await serviceProvider.ApplyIdentityMigrationsAsync();
        await serviceProvider.ApplyGameplayMigrationsAsync();
        await serviceProvider.ApplyStatisticsMigrationsAsync();
    }

    public string GetConnectionString(string dbName)
    {
        NpgsqlConnectionStringBuilder builder = new(Container.GetConnectionString())
        {
            Database = dbName,
            MaxPoolSize = 8
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
