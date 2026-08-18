using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure;

namespace Gameplay.Infrastructure;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("games");

        builder.Property(g => g.Id).HasConversion(new StronglyTypedIdValueConverter<GameId>());
        builder.Property(g => g.HostParticipantId).HasConversion(new StronglyTypedIdValueConverter<ParticipantId>());
        builder.Property(g => g.JoinCode).HasConversion(code => code.Value, value => JoinCode.From(value));
        builder.Property(g => g.SmallBlind).HasConversion(new ChipsStackValueConverter());
        builder.Property(g => g.BigBlind).HasConversion(new ChipsStackValueConverter());
        builder.PrimitiveCollection<List<ParticipantId>>("_seatingOrder")
            .ElementType(e => e.HasConversion<StronglyTypedIdValueConverter<ParticipantId>>())
            .HasColumnName("seating_order");
        builder.Property(g => g.CurrentHandId).HasConversion(new StronglyTypedIdValueConverter<HandId>());
        builder.Property<uint>("Version").IsRowVersion();
    }
}

internal sealed class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("participants");

        builder.Property(p => p.Id).HasConversion(new StronglyTypedIdValueConverter<ParticipantId>());
        builder.Property(p => p.Chips).HasConversion(new ChipsStackValueConverter());
        builder.Property(p => p.TotalBuyIn).HasConversion(new ChipsStackValueConverter());
        builder.Property<GameId>("GameId").HasConversion(new StronglyTypedIdValueConverter<GameId>());
    }
}
