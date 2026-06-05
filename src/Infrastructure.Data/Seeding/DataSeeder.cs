using System.Text.Json;
using Core.Domain.BookingAggregate;
using Core.Domain.EquipmentAggregate;
using Core.Domain.LocationAggregate;
using Core.Domain.MaintenanceEventAggregate;
using Core.Domain.OperatorAggregate;
using Infrastructure.Data.Persistence;

namespace Infrastructure.Data.Seeding;

public static class DataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public static void Seed(BookingDbContext db, string dataDirectory)
    {
        if (db.Equipment.Any()) return;

        T Deserialize<T>(string fileName) =>
            JsonSerializer.Deserialize<T>(
                File.ReadAllText(Path.Combine(dataDirectory, fileName)), JsonOptions)
            ?? throw new InvalidOperationException(
                   $"DataSeeder: '{fileName}' deserialised to null. " +
                   $"Verify the file contains a valid JSON array.");

        var locations = Deserialize<LocationSeedRecord[]>("locations.json");
        db.Locations.AddRange(locations.Select(r =>
            Location.Reconstitute(new LocationId(r.Id), r.Name, r.Zone)));

        var equipment = Deserialize<EquipmentSeedRecord[]>("equipment.json");
        db.Equipment.AddRange(equipment.Select(r =>
            Equipment.Reconstitute(new EquipmentId(r.Id), r.Name, r.Type,
                new LocationId(r.LocationId),
                Enum.Parse<EquipmentStatus>(r.Status, ignoreCase: true),
                r.LastServiceDate)));

        var operators = Deserialize<OperatorSeedRecord[]>("operators.json");
        db.Operators.AddRange(operators.Select(r =>
            Operator.Reconstitute(new OperatorId(r.Id), r.FullName, r.CertificationLevel,
                new LocationId(r.LocationId), r.IsActive)));

        var bookings = Deserialize<BookingSeedRecord[]>("bookings.json");
        db.Bookings.AddRange(bookings.Select(r =>
            Booking.Reconstitute(new BookingId(r.Id), new EquipmentId(r.EquipmentId),
                new OperatorId(r.OperatorId), r.StartTime, r.EndTime)));

        var events = Deserialize<MaintenanceEventSeedRecord[]>("maintenance-events.json");
        db.MaintenanceEvents.AddRange(events.Select(r =>
            MaintenanceEvent.Reconstitute(new MaintenanceEventId(r.Id), new EquipmentId(r.EquipmentId),
                r.ScheduledDate, r.CompletedDate, r.Description,
                Enum.Parse<MaintenanceStatus>(r.Status, ignoreCase: true))));

        db.SaveChanges();
    }
}

file record LocationSeedRecord(Guid Id, string Name, string Zone);
file record EquipmentSeedRecord(Guid Id, string Name, string Type, Guid LocationId, string Status, DateTimeOffset? LastServiceDate = null);
file record OperatorSeedRecord(Guid Id, string FullName, string CertificationLevel, Guid LocationId, bool IsActive);
file record BookingSeedRecord(Guid Id, Guid EquipmentId, Guid OperatorId, DateTimeOffset StartTime, DateTimeOffset EndTime);
file record MaintenanceEventSeedRecord(Guid Id, Guid EquipmentId, DateTimeOffset ScheduledDate, DateTimeOffset? CompletedDate, string Description, string Status);
