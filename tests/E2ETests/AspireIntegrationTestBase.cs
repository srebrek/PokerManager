using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Identity.Infrastructure.Data;

namespace E2ETests;

[Collection("Aspire Collection")]
public abstract class AspireIntegrationTestBase : IAsyncDisposable
{
    private readonly AspireFixture _fixture;

    private protected IdentityDbContext IdentityDbContext => _fixture.IdentityDbContext;

    protected AspireIntegrationTestBase(AspireFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.OutputAccessor.OutputHelper = output;
    }

    protected DistributedApplication App => _fixture.App
        ?? throw new InvalidOperationException("App is not initialized.");

    // The auth cookie is marked Secure, so cookie round-trips only work over HTTPS.
    // The dev certificate isn't trusted on CI machines, hence the validation override.
    protected HttpClient CreateApiClient() => CreateClient(useCookies: true);

    protected HttpClient CreateApiClientNoCookies() => CreateClient(useCookies: false);

    [SuppressMessage("Reliability", "CA2000", Justification = "HttpClient is responsible for disposing the handler.")]
    [SuppressMessage("Security", "CA5359", Justification = "Dev certificate in tests only.")]
    private HttpClient CreateClient(bool useCookies)
    {
        SocketsHttpHandler? handler = null;
        try
        {
#pragma warning disable S4830 // Server certificates should be verified — dev cert in tests only
            handler = new SocketsHttpHandler
            {
                UseCookies = useCookies,
                AllowAutoRedirect = false,
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    RemoteCertificateValidationCallback = (_, _, _, _) => true
                }
            };
#pragma warning restore S4830

            // All API endpoints are mounted under /api — append the prefix so that
            // relative URIs like "identity/me" resolve to /api/identity/me.
            Uri apiBase = new(App.GetEndpoint("apiservice", "https"), "api/");
            HttpClient client = new(handler) { BaseAddress = apiBase };

            handler = null;
            return client;
        }
        finally
        {
            handler?.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async Task DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            _fixture.OutputAccessor.OutputHelper = null;
            await _fixture.ResetIdentitySchemaAsync(TestContext.Current.CancellationToken);
        }
    }
}
