using Core.Domain.BookingAggregate;
using Core.Domain.EquipmentAggregate;
using Core.Domain.LocationAggregate;
using Core.Domain.MaintenanceEventAggregate;
using Core.Domain.OperatorAggregate;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Persistence;

public class BookingDbContext : DbContext
{
    public BookingDbContext(DbContextOptions<BookingDbContext> options) : base(options) { }

    public DbSet<Equipment>         Equipment          { get; set; } = null!;
    public DbSet<Location>          Locations          { get; set; } = null!;
    public DbSet<Operator>          Operators          { get; set; } = null!;
    public DbSet<Booking>           Bookings           { get; set; } = null!;
    public DbSet<MaintenanceEvent>  MaintenanceEvents  { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingDbContext).Assembly);
    }
}
