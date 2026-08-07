using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Identity.Infrastructure.Data;
using MartinCostello.Logging.XUnit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace E2ETests;

public sealed class AspireFixture : IAsyncLifetime
{
    private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(60);

    public DistributedApplication? App { get; private set; }

    internal TestOutputAccessor OutputAccessor { get; } = new();

    internal IdentityDbContext IdentityDbContext
    {
        get => field ?? throw new InvalidOperationException("IdentityDbContext is not initialized.");
        private set;
    }

    public async ValueTask InitializeAsync()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Aspire_AppHost>(ct);

        appHost.Services.AddSingleton<ITestOutputHelperAccessor>(OutputAccessor);
        appHost.Services.AddLogging(logging =>
        {
            logging.AddXUnit(OutputAccessor);
            logging.SetMinimumLevel(LogLevel.Debug);
            logging.AddFilter(appHost.Environment.ApplicationName, LogLevel.Debug);
            logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
            logging.AddFilter("Aspire.", LogLevel.Information);
        });

        appHost.Services.ConfigureHttpClientDefaults(clientBuilder => clientBuilder.AddStandardResilienceHandler());

        App = await appHost
            .BuildAsync(ct)
            .WaitAsync(s_defaultTimeout, ct);

        await App
            .StartAsync(ct)
            .WaitAsync(s_defaultTimeout, ct);

        await App.ResourceNotifications.WaitForResourceHealthyAsync("apiservice", ct).WaitAsync(s_defaultTimeout, ct);

        await AddDatabaseAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }
    }

    private async Task AddDatabaseAsync(CancellationToken ct)
    {
        string connectionString = await GetConnectionStringAsync(ct);
        DbContextOptions<IdentityDbContext> options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        IdentityDbContext = new(options);
    }

    private async Task<string> GetConnectionStringAsync(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(App);
        string? connectionString = await App.GetConnectionStringAsync("PokerManager-db", ct);
        return connectionString
            ?? throw new InvalidOperationException("Connection string for 'PokerManager-db' is not available.");
    }

    public async Task ResetIdentitySchemaAsync(CancellationToken ct)
    {
        const string sql = """
            DO $$
            DECLARE truncate_commands text;
            BEGIN
                SELECT string_agg(format('TRUNCATE TABLE %I.%I RESTART IDENTITY CASCADE;', schemaname, tablename), ' ')
                INTO truncate_commands
                FROM pg_tables
                WHERE schemaname NOT IN ('pg_catalog', 'information_schema')
                AND tablename NOT IN ('__EFMigrationsHistory', 'data_protection_keys');
                             
                IF truncate_commands IS NOT NULL THEN
                EXECUTE truncate_commands;
                END IF;
            END $$;
            """;

        await IdentityDbContext.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
