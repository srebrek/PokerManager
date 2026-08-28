using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Statistics.Domain.Entities;

namespace Statistics.Infrastructure;

internal sealed class RebuyConfiguration : IEntityTypeConfiguration<Rebuy>
{
    public void Configure(EntityTypeBuilder<Rebuy> builder)
    {
        builder.ToTable("rebuy");

        builder.HasKey(r => r.EventId);
    }
}
