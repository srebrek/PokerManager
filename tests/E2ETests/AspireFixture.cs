using System.Data.Common;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
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

    internal const float DefaultUiTimeoutMilliseconds = 60_000;

    public DistributedApplication? App { get; private set; }

    internal TestOutputAccessor OutputAccessor { get; } = new();

    internal IBrowser Browser => _browser ?? throw new InvalidOperationException("Browser is not initialized.");

    internal Uri FrontendBaseUri => App?.GetEndpoint("apiservice", "https")
        ?? throw new InvalidOperationException("App is not initialized.");

    private IPlaywright? _playwright;
    private IBrowser? _browser;
    private Respawner? _respawner;
    private string _connectionString = string.Empty;

    // TEMP diagnostics for the intermittent E2E failure — remove once the cause is known.
    private CancellationTokenSource? _resourceWatchCts;

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

        // TEMP diagnostics for the intermittent E2E failure — remove once the cause is known.
        WatchResourceStates();
    }

    // TEMP diagnostics for the intermittent E2E failure — remove once the cause is known.
    private void WatchResourceStates()
    {
        _resourceWatchCts = new CancellationTokenSource();
        CancellationToken watchToken = _resourceWatchCts.Token;

        _ = Task.Run(
            async () =>
            {
                await foreach (ResourceEvent resourceEvent in App!.ResourceNotifications.WatchAsync(watchToken))
                {
                    Write($"[resource] {resourceEvent.Resource.Name} -> {resourceEvent.Snapshot.State?.Text}");
                }
            },
            watchToken);

        void Write(string line)
        {
            try
            {
                OutputAccessor.OutputHelper?.WriteLine($"{DateTimeOffset.UtcNow:HH:mm:ss.fff} {line}");
            }
            catch (InvalidOperationException)
            {
                // The test that owned the output helper has already finished.
            }
        }
    }

    // TEMP diagnostics for the intermittent E2E failure — remove once the cause is known.
    internal async Task<string> DescribeDatabaseStateAsync(CancellationToken ct)
    {
        try
        {
            await using NpgsqlConnection connection = new(_connectionString);
            await connection.OpenAsync(ct);

            await using NpgsqlCommand command = new(
                """
                select (select string_agg(datname, ',' order by datname) from pg_database) as databases,
                       (select count(*) from pg_stat_activity) as connections,
                       (select setting from pg_settings where name = 'max_connections') as max_connections,
                       pg_postmaster_start_time() as started_at
                """,
                connection);

            await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(ct);
            await reader.ReadAsync(ct);

            return $"databases=[{reader.GetString(0)}] connections={reader.GetInt64(1)}"
                + $" max={reader.GetString(2)} postgresStartedAt={reader.GetDateTime(3):HH:mm:ss}";
        }
        catch (DbException ex)
        {
            return $"probe failed: {ex.GetType().Name}: {ex.Message}";
        }
        catch (InvalidOperationException ex)
        {
            return $"probe failed: {ex.GetType().Name}: {ex.Message}";
        }
        catch (TimeoutException ex)
        {
            return $"probe failed: {ex.GetType().Name}: {ex.Message}";
        }
    }

    public async ValueTask DisposeAsync()
    {
        // TEMP diagnostics for the intermittent E2E failure — remove once the cause is known.
        if (_resourceWatchCts is not null)
        {
            await _resourceWatchCts.CancelAsync();
            _resourceWatchCts.Dispose();
        }

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

        await WaitForStableDatabaseConnectionAsync(ct);

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

    private async Task WaitForStableDatabaseConnectionAsync(CancellationToken ct)
    {
        const int RequiredConsecutiveSuccesses = 3;
        TimeSpan probeInterval = TimeSpan.FromSeconds(1);

        using CancellationTokenSource timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(s_defaultTimeout);

        int consecutiveSuccesses = 0;
        while (consecutiveSuccesses < RequiredConsecutiveSuccesses)
        {
            try
            {
                await using NpgsqlConnection probe = new(_connectionString);
                await probe.OpenAsync(timeoutCts.Token);
                consecutiveSuccesses++;
            }
            catch (NpgsqlException)
            {
                consecutiveSuccesses = 0;
            }

            if (consecutiveSuccesses < RequiredConsecutiveSuccesses)
            {
                await Task.Delay(probeInterval, timeoutCts.Token);
            }
        }
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
