using Contracts.Api.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Tracking;

namespace IntegrationTests;

// TODO: add csrf tests
public abstract class BaseIntegrationTest(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly TimeSpan s_trackingTimeout = TimeSpan.FromSeconds(30);

    protected ApiFactory Factory => factory;
    protected HttpClient Client { get; } = WithAntiCsrfHeader(factory.CreateClient());
    protected IServiceProvider Services { get; } = factory.Services;
    protected CancellationToken CancellationToken { get; } = TestContext.Current.CancellationToken;

    protected static HttpClient WithAntiCsrfHeader(HttpClient client)
    {
        client.BaseAddress = new Uri(client.BaseAddress!, "api/");
        client.DefaultRequestHeaders.Add(HttpDefenseHeaders.AntiCsrf, HttpDefenseHeaders.AntiCsrfValue);
        return client;
    }

    public async ValueTask InitializeAsync()
    {
        await factory.ResetDatabaseAsync();
    }

    protected Task<ITrackedSession> TrackAsync(Func<Task> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return Factory.Host
            .TrackActivity()
            .Timeout(s_trackingTimeout)
            .ExecuteAndWaitAsync(_ => action());
    }

    protected async Task<(ITrackedSession Session, TResult Result)> TrackAsync<TResult>(Func<Task<TResult>> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        TResult result = default!;
        ITrackedSession session = await TrackAsync(async () =>
        {
            result = await action();
        });

        return (session, result);
    }

    protected async Task ExecuteWithContextAsync<TContext>(Func<TContext, Task> action)
        where TContext : DbContext
    {
        using IServiceScope scope = Services.CreateScope();
        TContext dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        await action(dbContext);
    }

    protected async Task<TResult> ExecuteWithContextAsync<TContext, TResult>(Func<TContext, Task<TResult>> action)
        where TContext : DbContext
    {
        using IServiceScope scope = Services.CreateScope();
        TContext dbContext = scope.ServiceProvider.GetRequiredService<TContext>();
        return await action(dbContext);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            Client.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
