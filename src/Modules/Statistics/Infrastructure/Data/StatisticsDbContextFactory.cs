using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Statistics.Infrastructure.Data;

internal sealed class StatisticsDbContextFactory : IDesignTimeDbContextFactory<StatisticsDbContext>
{
    public StatisticsDbContext CreateDbContext(string[] args)
    {
        // Design-time only (dotnet ef migrations). The fallback targets a local dev
        // database and is never used at runtime; override with EF_CONNECTIONSTRING.
        string connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING")
            ?? "Server=localhost;Port=5432;Database=pokermanager;User Id=postgres;";

        DbContextOptionsBuilder<StatisticsDbContext> optionsBuilder = new();
        optionsBuilder
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName, StatisticsDbContext.Schema))
            .UseSnakeCaseNamingConvention();

        return new StatisticsDbContext(optionsBuilder.Options);
    }
}
