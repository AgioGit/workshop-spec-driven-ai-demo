using Ardalis.GuardClauses;
using Core.Domain.EquipmentAggregate;

namespace Core.Domain.LocationAggregate;

public class Location
{
    public LocationId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Zone { get; private set; } = string.Empty;

    private Location() { }

    public static Location Create(string name, string zone)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(zone, nameof(zone));
        return new Location
        {
            Id = new LocationId(Guid.NewGuid()),
            Name = name,
            Zone = zone
        };
    }

    public static Location Reconstitute(LocationId id, string name, string zone)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(zone, nameof(zone));
        return new Location
        {
            Id = id,
            Name = name,
            Zone = zone
        };
    }
}
