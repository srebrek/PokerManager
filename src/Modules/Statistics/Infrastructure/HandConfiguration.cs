using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Statistics.Domain.Entities;
using Statistics.Domain.ValueObjects;

namespace Statistics.Infrastructure;

internal sealed class HandConfiguration : IEntityTypeConfiguration<Hand>
{
    public void Configure(EntityTypeBuilder<Hand> builder)
    {
        builder.ToTable("hand");

        builder.HasKey(h => h.Id);

        builder.OwnsMany(h => h.Seats, seat =>
        {
            seat.ToTable("hand_seat");

            seat.Property<Guid>("HandId");
            seat.HasKey("HandId", nameof(HandSeat.ParticipantId));
            seat.WithOwner().HasForeignKey("HandId");
        });

        builder.OwnsMany(h => h.Actions, action =>
        {
            action.ToTable("hand_action");

            action.Property<Guid>("HandId");
            action.HasKey(nameof(HandAction.EventId), nameof(HandAction.SequenceNumber));
            action.WithOwner().HasForeignKey("HandId");

            action.Property(a => a.Type).HasConversion<string>();
        });

        builder.OwnsMany(h => h.Results, result =>
        {
            result.ToTable("hand_result");

            result.Property<Guid>("HandId");
            result.HasKey("HandId", nameof(HandResult.ParticipantId));
            result.WithOwner().HasForeignKey("HandId");
        });

        builder.OwnsMany(h => h.Pots, pot =>
        {
            pot.ToTable("hand_pot");

            pot.Property<Guid>("HandId");
            pot.HasKey("HandId", nameof(HandPot.PotIndex));
            pot.WithOwner().HasForeignKey("HandId");

            pot.Property(p => p.PotIndex).ValueGeneratedNever();

            pot.OwnsMany(p => p.Winners, winner =>
            {
                winner.ToTable("hand_pot_winner");

                winner.Property<Guid>("HandId");
                winner.Property<int>("PotIndex");
                winner.HasKey("HandId", "PotIndex", nameof(HandPotWinner.ParticipantId));
                winner.WithOwner().HasForeignKey("HandId", "PotIndex");
            });
        });
    }
}
