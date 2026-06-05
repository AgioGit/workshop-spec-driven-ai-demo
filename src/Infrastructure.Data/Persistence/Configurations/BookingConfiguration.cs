using Core.Domain.BookingAggregate;
using Core.Domain.EquipmentAggregate;
using Core.Domain.OperatorAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasConversion(id => id.Value, v => new BookingId(v));

        builder.Property(b => b.EquipmentId)
            .HasConversion(id => id.Value, v => new EquipmentId(v));

        builder.Property(b => b.OperatorId)
            .HasConversion(id => id.Value, v => new OperatorId(v));
    }
}
