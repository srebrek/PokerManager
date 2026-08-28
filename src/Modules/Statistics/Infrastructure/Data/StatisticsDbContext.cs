using Microsoft.EntityFrameworkCore;
using Statistics.Domain.Entities;

namespace Statistics.Infrastructure.Data;

public sealed class StatisticsDbContext(DbContextOptions<StatisticsDbContext> options) : DbContext(options)
{
    public const string Schema = "statistics";

    internal DbSet<Hand> Hands { get; set; } = null!;

    internal DbSet<Rebuy> Rebuys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StatisticsDbContext).Assembly);
    }
}
