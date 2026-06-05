using Core.Domain.EquipmentAggregate;
using Core.Domain.MaintenanceEventAggregate;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Data.Persistence.Configurations;

public class MaintenanceEventConfiguration : IEntityTypeConfiguration<MaintenanceEvent>
{
    public void Configure(EntityTypeBuilder<MaintenanceEvent> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasConversion(id => id.Value, v => new MaintenanceEventId(v));

        builder.Property(m => m.EquipmentId)
            .HasConversion(id => id.Value, v => new EquipmentId(v));

        builder.Property(m => m.Status).HasConversion<string>();
    }
}
