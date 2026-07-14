using Gameplay.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Infrastructure;
using Shared.Presentation.Extensions;
using Wolverine.EntityFrameworkCore;

namespace Gameplay;

public static class GameplayModule
{
    public static IServiceCollection AddGameplayModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(DatabaseConstants.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DatabaseConstants.ConnectionStringName}' is not configured.");

        services
            .AddDatabase(connectionString)
            .AddEndpoints(typeof(GameplayModule).Assembly);

        return services;
    }

    public static async Task<IServiceProvider> ApplyGameplayMigrationsAsync(this IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        GameplayDbContext dbContext = scope.ServiceProvider.GetRequiredService<GameplayDbContext>();
        await dbContext.Database.MigrateAsync();
        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextWithWolverineIntegration<GameplayDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName, GameplayDbContext.Schema))
            .UseSnakeCaseNamingConvention()
            .AddDomainEventsClearing());

        return services;
    }
}
