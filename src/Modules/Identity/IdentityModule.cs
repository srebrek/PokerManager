using Identity.Abstractions;
using Identity.Infrastructure;
using Identity.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application;
using Shared.Infrastructure;
using Shared.Presentation.Extensions;
using Wolverine.EntityFrameworkCore;

namespace Identity;

public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(DatabaseConstants.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DatabaseConstants.ConnectionStringName}' is not configured.");

        services.AddAuthorization();
        services.AddScoped<IUserAccountService, UserAccountService>();
        services
            .AddDatabase(connectionString)
            .AddPersistentDataProtection()
            .AddIdentityApiWithCookies()
            .AddApplication(typeof(IdentityModule).Assembly)
            .AddEndpoints(typeof(IdentityModule).Assembly);

        return services;
    }

    public static IApplicationBuilder UseIdentityModule(this IApplicationBuilder app)
    {
        return app
            .UseAuthentication()
            .UseAuthorization();
    }

    public static async Task<IServiceProvider> ApplyIdentityMigrationsAsync(this IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        IdentityDbContext dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        await dbContext.Database.MigrateAsync();
        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextWithWolverineIntegration<IdentityDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName, IdentityDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        return services;
    }

    private static IServiceCollection AddPersistentDataProtection(this IServiceCollection services)
    {
        services
            .AddDataProtection()
            .PersistKeysToDbContext<IdentityDbContext>()
            .SetApplicationName("PokerManager");

        return services;
    }

    private static IServiceCollection AddIdentityApiWithCookies(this IServiceCollection services)
    {
        services
            .AddIdentity<User, IdentityRole<Guid>>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddEntityFrameworkStores<IdentityDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.HttpOnly = IdentityCookies.AuthCookieHttpOnly;
            options.Cookie.SameSite = IdentityCookies.AuthCookieSameSite;
            options.Cookie.SecurePolicy = IdentityCookies.AuthCookieSecure;
            options.Cookie.Name = IdentityCookies.AuthCookieName;
            options.Events.OnRedirectToLogin = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = ctx =>
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        return services;
    }
}
