using Aspire.Hosting;
using Aspire.Hosting.Testing;
using MartinCostello.Logging.XUnit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Npgsql;
using Respawn;
using Respawn.Graph;

namespace E2ETests;

public sealed class AspireFixture : IAsyncLifetime
{
    private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(60);

    internal const float DefaultUiTimeoutMilliseconds = 30_000;

    public DistributedApplication? App { get; private set; }

    internal TestOutputAccessor OutputAccessor { get; } = new();

    internal IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser is not initialized.");

    internal Uri FrontendBaseUri => App?.GetEndpoint("apiservice", "https")
        ?? throw new InvalidOperationException("App is not initialized.");

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private Respawner? _respawner;
    private string _connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        CancellationToken ct = TestContext.Current.CancellationToken;

        int installExitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (installExitCode is not 0)
        {
            throw new InvalidOperationException($"Playwright browser install failed with exit code {installExitCode}.");
        }

        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions());

        Assertions.SetDefaultExpectTimeout(DefaultUiTimeoutMilliseconds);

        string? environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string[] appHostArgs = string.IsNullOrEmpty(environment) ? [] : ["--environment", environment];

        IDistributedApplicationTestingBuilder appHost = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.Aspire_AppHost>(appHostArgs, ct);

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

        await CreateRespawnerAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        if (App is not null)
        {
            await App.DisposeAsync();
        }

        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();
    }

    private async Task CreateRespawnerAsync(CancellationToken ct)
    {
        _connectionString = await GetConnectionStringAsync(ct);

        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(ct);

        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToExclude = ["wolverine"],
            TablesToIgnore =
            [
                new Table("__EFMigrationsHistory"),
                new Table("data_protection_keys"),
            ]
        });
    }

    private async Task<string> GetConnectionStringAsync(CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(App);
        string? connectionString = await App.GetConnectionStringAsync("PokerManager-db", ct);
        return connectionString
            ?? throw new InvalidOperationException("Connection string for 'PokerManager-db' is not available.");
    }

    public async Task ResetDatabaseAsync(CancellationToken ct)
    {
        if (_respawner is null)
        {
            throw new InvalidOperationException("Respawner is not initialized.");
        }

        await using NpgsqlConnection connection = new(_connectionString);
        await connection.OpenAsync(ct);
        await _respawner.ResetAsync(connection);
    }
}
