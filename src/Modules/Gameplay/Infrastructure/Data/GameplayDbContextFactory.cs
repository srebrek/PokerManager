using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Gameplay.Infrastructure.Data;

internal sealed class GameplayDbContextFactory : IDesignTimeDbContextFactory<GameplayDbContext>
{
    public GameplayDbContext CreateDbContext(string[] args)
    {
        // Design-time only (dotnet ef migrations). The fallback targets a local dev
        // database and is never used at runtime; override with EF_CONNECTIONSTRING.
        string connectionString = Environment.GetEnvironmentVariable("EF_CONNECTIONSTRING")
            ?? "Server=localhost;Port=5432;Database=pokermanager;User Id=postgres;";

        DbContextOptionsBuilder<GameplayDbContext> optionsBuilder = new();
        optionsBuilder
            .UseNpgsql(connectionString, npgsqlOptions =>
                npgsqlOptions.MigrationsHistoryTable(
                    HistoryRepository.DefaultTableName, GameplayDbContext.Schema))
            .UseSnakeCaseNamingConvention();

        return new GameplayDbContext(optionsBuilder.Options);
    }
}
