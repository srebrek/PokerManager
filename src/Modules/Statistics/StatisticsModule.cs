using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Application;
using Shared.Infrastructure;
using Shared.Presentation.Extensions;
using Statistics.Infrastructure.Data;
using Wolverine.EntityFrameworkCore;

namespace Statistics;

public static class StatisticsModule
{
    public static IServiceCollection AddStatisticsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        string connectionString = configuration.GetConnectionString(DatabaseConstants.ConnectionStringName)
            ?? throw new InvalidOperationException(
                $"Connection string '{DatabaseConstants.ConnectionStringName}' is not configured.");

        services
            .AddDatabase(connectionString)
            .AddApplication(typeof(StatisticsModule).Assembly)
            .AddEndpoints(typeof(StatisticsModule).Assembly);

        return services;
    }

    public static async Task<IServiceProvider> ApplyStatisticsMigrationsAsync(this IServiceProvider services)
    {
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        StatisticsDbContext dbContext = scope.ServiceProvider.GetRequiredService<StatisticsDbContext>();
        await dbContext.Database.MigrateAsync();
        return services;
    }

    private static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        services.AddDbContextWithWolverineIntegration<StatisticsDbContext>((sp, options) => options
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName, StatisticsDbContext.Schema))
            .UseSnakeCaseNamingConvention());

        return services;
    }
}
