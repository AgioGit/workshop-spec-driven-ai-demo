using Core.Domain.EquipmentAggregate;
using Core.Domain.OperatorAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Persistence.Configurations;

public class OperatorConfiguration : IEntityTypeConfiguration<Operator>
{
    public void Configure(EntityTypeBuilder<Operator> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasConversion(id => id.Value, v => new OperatorId(v));

        builder.Property(o => o.LocationId)
            .HasConversion(id => id.Value, v => new LocationId(v));

        builder.Property(o => o.FullName).IsRequired().HasMaxLength(200);
        builder.Property(o => o.CertificationLevel).IsRequired().HasMaxLength(100);
    }
}
