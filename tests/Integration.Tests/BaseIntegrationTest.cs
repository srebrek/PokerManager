using Contracts.Api.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.Tests;

public abstract class BaseIntegrationTest(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
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
