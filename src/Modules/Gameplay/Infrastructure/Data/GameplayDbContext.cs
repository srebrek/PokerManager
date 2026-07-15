using Gameplay.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Gameplay.Infrastructure.Data;

public sealed class GameplayDbContext(DbContextOptions<GameplayDbContext> options) : DbContext(options)
{
    public const string Schema = "gameplay";

    internal DbSet<Game> Games { get; set; } = null!;

    internal DbSet<Hand> Hands { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GameplayDbContext).Assembly);
    }
}
