using Gameplay.Domain.Entities;
using Gameplay.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared.Infrastructure;

namespace Gameplay.Infrastructure;

internal sealed class HandConfiguration : IEntityTypeConfiguration<Hand>
{
    public void Configure(EntityTypeBuilder<Hand> builder)
    {
        builder.ToTable("hands");

        builder.Property(h => h.Id).HasConversion(new StronglyTypedIdValueConverter<HandId>());
        builder.Property(h => h.GameId).HasConversion(new StronglyTypedIdValueConverter<GameId>());
        builder.Property(h => h.Street).HasConversion<string>();
        builder.Property(h => h.Status).HasConversion<string>();
        builder.Property<uint>("Version").IsRowVersion();

        builder.HasOne<Game>().WithMany().OnDelete(DeleteBehavior.Restrict);

        builder.OwnsMany(h => h.Seats, seat =>
        {
            seat.ToTable("hand_seats");

            seat.Property<HandId>("HandId").HasConversion(new StronglyTypedIdValueConverter<HandId>());
            seat.HasKey("HandId", nameof(HandSeat.ParticipantId));
            seat.WithOwner().HasForeignKey("HandId");

            seat.Property(s => s.ParticipantId).HasConversion(new StronglyTypedIdValueConverter<ParticipantId>());
            seat.Property(s => s.StartingStack).HasConversion(new ChipsStackValueConverter());

            seat.HasOne<Participant>().WithMany().OnDelete(DeleteBehavior.Restrict);
        });

        builder.OwnsMany(h => h.Actions, action =>
        {
            action.ToTable("hand_actions");

            action.Property<HandId>("HandId").HasConversion(new StronglyTypedIdValueConverter<HandId>());
            action.HasKey("HandId", nameof(HandAction.SequenceNumber));
            action.WithOwner().HasForeignKey("HandId");

            action.Property(a => a.SequenceNumber).ValueGeneratedNever();
            action.Property(a => a.ParticipantId).HasConversion(new StronglyTypedIdValueConverter<ParticipantId>());
            action.Property(a => a.Type).HasConversion<string>();
            action.Property(a => a.Street).HasConversion<string>();
            action.Property(a => a.Amount).HasConversion(new ChipsStackValueConverter());

            action.HasOne<Participant>().WithMany().OnDelete(DeleteBehavior.Restrict);
        });
    }
}
