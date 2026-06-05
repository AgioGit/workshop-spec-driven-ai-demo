using Core.Domain.BookingAggregate;
using Core.Domain.EquipmentAggregate;
using Core.Domain.LocationAggregate;
using Core.Domain.MaintenanceEventAggregate;
using Core.Domain.OperatorAggregate;
using Infrastructure.Data.Persistence;
using Infrastructure.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Core.Domain.Tests.DataSeeding;

public class DataSeederTests
{
    // ── Aggregate factory tests ────────────────────────────────────────────

    [Fact]
    public void Equipment_Reconstitute_PreservesIdAndStatus()
    {
        var id = new EquipmentId(Guid.NewGuid());
        var locId = new LocationId(Guid.NewGuid());
        var equipment = Equipment.Reconstitute(id, "Test Rig", "Drill", locId, EquipmentStatus.InUse);

        Assert.Equal(id, equipment.Id);
        Assert.Equal("Test Rig", equipment.Name);
        Assert.Equal(EquipmentStatus.InUse, equipment.Status);
    }

    [Fact]
    public void Location_Create_WithValidArguments_SetsProperties()
    {
        var location = Location.Create("Site Alpha", "North");

        Assert.Equal("Site Alpha", location.Name);
        Assert.Equal("North", location.Zone);
        Assert.NotEqual(default, location.Id);
    }

    [Fact]
    public void Operator_Create_WithValidArguments_SetsProperties()
    {
        var locId = new LocationId(Guid.NewGuid());
        var op = Operator.Create("Alice Johnson", "Senior", locId);

        Assert.Equal("Alice Johnson", op.FullName);
        Assert.Equal("Senior", op.CertificationLevel);
        Assert.Equal(locId, op.LocationId);
        Assert.True(op.IsActive);
    }

    [Fact]
    public void Booking_Create_WithValidArguments_SetsStartAndEnd()
    {
        var equipId = new EquipmentId(Guid.NewGuid());
        var opId = new OperatorId(Guid.NewGuid());
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(8);

        var booking = Booking.Create(equipId, opId, start, end);

        Assert.Equal(equipId, booking.EquipmentId);
        Assert.Equal(opId, booking.OperatorId);
        Assert.Equal(start, booking.StartTime);
        Assert.Equal(end, booking.EndTime);
    }

    [Fact]
    public void MaintenanceEvent_Create_WithValidArguments_SetsStatusToScheduled()
    {
        var equipId = new EquipmentId(Guid.NewGuid());
        var scheduled = DateTimeOffset.UtcNow.AddDays(7);

        var ev = MaintenanceEvent.Create(equipId, scheduled, "Replace hydraulic seals");

        Assert.Equal(equipId, ev.EquipmentId);
        Assert.Equal(scheduled, ev.ScheduledDate);
        Assert.Equal(MaintenanceStatus.Scheduled, ev.Status);
        Assert.Null(ev.CompletedDate);
    }

    [Fact]
    public void Location_Reconstitute_PreservesId()
    {
        var id = new LocationId(Guid.NewGuid());
        var location = Location.Reconstitute(id, "Site Gamma", "East");

        Assert.Equal(id, location.Id);
        Assert.Equal("Site Gamma", location.Name);
        Assert.Equal("East", location.Zone);
    }

    [Fact]
    public void Operator_Reconstitute_PreservesId()
    {
        var id = new OperatorId(Guid.NewGuid());
        var locId = new LocationId(Guid.NewGuid());
        var op = Operator.Reconstitute(id, "Bob Smith", "Junior", locId, false);

        Assert.Equal(id, op.Id);
        Assert.Equal("Bob Smith", op.FullName);
        Assert.False(op.IsActive);
    }

    [Fact]
    public void Booking_Reconstitute_PreservesId()
    {
        var id = new BookingId(Guid.NewGuid());
        var equipId = new EquipmentId(Guid.NewGuid());
        var opId = new OperatorId(Guid.NewGuid());
        var start = DateTimeOffset.UtcNow;
        var end = start.AddHours(8);

        var booking = Booking.Reconstitute(id, equipId, opId, start, end);

        Assert.Equal(id, booking.Id);
        Assert.Equal(equipId, booking.EquipmentId);
        Assert.Equal(opId, booking.OperatorId);
    }

    [Fact]
    public void MaintenanceEvent_Reconstitute_PreservesId()
    {
        var id = new MaintenanceEventId(Guid.NewGuid());
        var equipId = new EquipmentId(Guid.NewGuid());
        var scheduled = DateTimeOffset.UtcNow.AddDays(5);
        var completed = scheduled.AddDays(1);

        var ev = MaintenanceEvent.Reconstitute(id, equipId, scheduled, completed,
            "Engine overhaul", MaintenanceStatus.Completed);

        Assert.Equal(id, ev.Id);
        Assert.Equal(MaintenanceStatus.Completed, ev.Status);
        Assert.Equal(completed, ev.CompletedDate);
    }

    [Fact]
    public void Booking_Create_WithEndTimeBeforeStartTime_Throws()
    {
        var equipId = new EquipmentId(Guid.NewGuid());
        var opId = new OperatorId(Guid.NewGuid());
        var start = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            Booking.Create(equipId, opId, start, start.AddHours(-1)));
    }

    [Fact]
    public void Booking_Create_WithEndTimeEqualToStartTime_Throws()
    {
        var equipId = new EquipmentId(Guid.NewGuid());
        var opId = new OperatorId(Guid.NewGuid());
        var start = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            Booking.Create(equipId, opId, start, start));
    }

    [Fact]
    public void Booking_Reconstitute_WithEndTimeBeforeStartTime_Throws()
    {
        var id = new BookingId(Guid.NewGuid());
        var equipId = new EquipmentId(Guid.NewGuid());
        var opId = new OperatorId(Guid.NewGuid());
        var start = DateTimeOffset.UtcNow;

        Assert.Throws<ArgumentException>(() =>
            Booking.Reconstitute(id, equipId, opId, start, start.AddMinutes(-1)));
    }

    [Fact]
    public void Equipment_Reconstitute_WithLastServiceDate_SetsProperty()
    {
        var id = new EquipmentId(Guid.NewGuid());
        var locId = new LocationId(Guid.NewGuid());
        var serviceDate = new DateTimeOffset(2025, 4, 10, 8, 0, 0, TimeSpan.Zero);

        var equipment = Equipment.Reconstitute(id, "Test Rig", "Drill", locId,
            EquipmentStatus.Available, serviceDate);

        Assert.Equal(serviceDate, equipment.LastServiceDate);
    }



    private static readonly string DataDir =
        Path.Combine(AppContext.BaseDirectory, "data");

    private static BookingDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new BookingDbContext(opts);
    }

    [Fact]
    public void Seed_LoadsCorrectLocationCount()
    {
        using var db = CreateDb();
        DataSeeder.Seed(db, DataDir);
        Assert.True(db.Locations.Count() >= 4);
    }

    [Fact]
    public void Seed_LoadsCorrectEquipmentCount()
    {
        using var db = CreateDb();
        DataSeeder.Seed(db, DataDir);
        Assert.True(db.Equipment.Count() >= 15);
    }

    [Fact]
    public void Seed_LoadsCorrectOperatorCount()
    {
        using var db = CreateDb();
        DataSeeder.Seed(db, DataDir);
        Assert.True(db.Operators.Count() >= 10);
    }

    [Fact]
    public void Seed_LoadsCorrectBookingCount()
    {
        using var db = CreateDb();
        DataSeeder.Seed(db, DataDir);
        Assert.True(db.Bookings.Count() >= 12);
    }

    [Fact]
    public void Seed_LoadsCorrectMaintenanceEventCount()
    {
        using var db = CreateDb();
        DataSeeder.Seed(db, DataDir);
        Assert.True(db.MaintenanceEvents.Count() >= 8);
    }

    [Fact]
    public void Seed_PreservesLegacyLocationGuids()
    {
        using var db = CreateDb();
        DataSeeder.Seed(db, DataDir);

        var locationIds = db.Locations
            .Select(l => l.Id)
            .AsEnumerable()
            .Select(id => id.Value)
            .ToHashSet();

        Assert.Contains(Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"), locationIds);
        Assert.Contains(Guid.Parse("8d0f7780-8536-51ef-a55c-f18fd2a01bf8"), locationIds);
    }

    [Fact]
    public void Seed_PreservesLegacyEquipmentNames()
    {
        using var db = CreateDb();
        DataSeeder.Seed(db, DataDir);

        var names = db.Equipment.Select(e => e.Name).ToHashSet();

        Assert.Contains("Drill Rig Alpha", names);
        Assert.Contains("Loader Beta", names);
        Assert.Contains("Excavator Gamma", names);
    }

    [Fact]
    public void Seed_SomeEquipmentHasLastServiceDate()
    {
        using var db = CreateDb();
        DataSeeder.Seed(db, DataDir);

        var countWithServiceDate = db.Equipment
            .AsEnumerable()
            .Count(e => e.LastServiceDate.HasValue);

        Assert.True(countWithServiceDate >= 8,
            $"Expected at least 8 equipment rows with a LastServiceDate, got {countWithServiceDate}");
    }
}
