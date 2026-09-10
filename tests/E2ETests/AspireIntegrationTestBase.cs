using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Playwright;

namespace E2ETests;

[Collection("Aspire Collection")]
public abstract class AspireIntegrationTestBase : IAsyncLifetime
{
    private readonly AspireFixture _fixture;

    private readonly List<IBrowserContext> _browserContexts = [];

    protected AspireIntegrationTestBase(AspireFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _fixture.OutputAccessor.OutputHelper = output;
        Output = output;
    }

    // TEMP diagnostics for the intermittent E2E failure — remove once the cause is known.
    protected ITestOutputHelper Output { get; }

    protected DistributedApplication App => _fixture.App
        ?? throw new InvalidOperationException("App is not initialized.");

    protected Uri FrontendBaseUri => _fixture.FrontendBaseUri;

    // TEMP diagnostics for the intermittent E2E failure — remove once the cause is known.
    protected Task<string> DescribeDatabaseStateAsync(CancellationToken ct) =>
        _fixture.DescribeDatabaseStateAsync(ct);

    protected IPage Page { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync(TestContext.Current.CancellationToken);
        Page = await NewPageAsync();
    }

    protected async Task<IPage> NewPageAsync()
    {
        IBrowserContext context =
            await _fixture.Browser.NewContextAsync(new BrowserNewContextOptions { IgnoreHTTPSErrors = true });

        _browserContexts.Add(context);

        IPage page = await context.NewPageAsync();
        page.SetDefaultTimeout(AspireFixture.DefaultUiTimeoutMilliseconds);
        return page;
    }

    protected HttpClient CreateApiClientWithCookies() => CreateClient(true);

    protected HttpClient CreateApiClient() => CreateClient(false);

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
            foreach (IBrowserContext context in _browserContexts)
            {
                await context.CloseAsync();
            }

            _browserContexts.Clear();
            _fixture.OutputAccessor.OutputHelper = null;
        }
    }
}
