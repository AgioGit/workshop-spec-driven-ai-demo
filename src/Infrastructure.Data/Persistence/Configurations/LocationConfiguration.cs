using Core.Domain.EquipmentAggregate;
using Core.Domain.LocationAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id)
            .HasConversion(id => id.Value, v => new LocationId(v));

        builder.Property(l => l.Name).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Zone).IsRequired().HasMaxLength(100);
    }
}
