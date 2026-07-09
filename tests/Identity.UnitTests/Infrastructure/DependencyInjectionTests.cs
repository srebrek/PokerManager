using Identity.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Identity.UnitTests.Infrastructure;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddIdentityModule_WithValidConnection_ShouldConfigure_PasswordPolicy()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PokerManager-db"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        // Act
        services.AddIdentityModule(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IdentityOptions options = provider.GetRequiredService<IOptions<IdentityOptions>>().Value;

        options.User.RequireUniqueEmail.ShouldBeTrue();
        options.Password.RequireDigit.ShouldBeTrue();
        options.Password.RequireLowercase.ShouldBeTrue();
        options.Password.RequireUppercase.ShouldBeTrue();
        options.Password.RequireNonAlphanumeric.ShouldBeTrue();
        options.Password.RequiredLength.ShouldBe(8);
    }

    [Fact]
    public void AddIdentityModule_ShouldConfigure_ApplicationCookie()
    {
        // Arrange
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PokerManager-db"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        // Act
        services.AddIdentityModule(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        IOptionsMonitor<CookieAuthenticationOptions> optionsMonitor =
            provider.GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>();
        CookieAuthenticationOptions options = optionsMonitor.Get(IdentityConstants.ApplicationScheme);

        options.Cookie.Name.ShouldBe(IdentityCookies.AuthCookieName);
        options.Cookie.HttpOnly.ShouldBeTrue();
        options.Cookie.SecurePolicy.ShouldBe(CookieSecurePolicy.Always);
    }
}
