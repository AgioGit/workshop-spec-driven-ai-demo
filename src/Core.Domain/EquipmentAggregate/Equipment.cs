using Ardalis.GuardClauses;
using StronglyTypedIds;

namespace Core.Domain.EquipmentAggregate;

[StronglyTypedId(converters: StronglyTypedIdConverter.SystemTextJson)]
public partial struct EquipmentId { }

[StronglyTypedId(converters: StronglyTypedIdConverter.SystemTextJson)]
public partial struct LocationId { }

public enum EquipmentStatus { Available, InUse, Maintenance }

public class Equipment
{
    public EquipmentId Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Type { get; private set; } = string.Empty;
    public EquipmentStatus Status { get; private set; }
    public LocationId LocationId { get; private set; }
    public DateTimeOffset? LastServiceDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Equipment() { }

    public static Equipment Create(string name, string type, LocationId locationId)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Value cannot be null or whitespace.", nameof(type));

        return new Equipment
        {
            Id = new EquipmentId(Guid.NewGuid()),
            Name = name,
            Type = type,
            LocationId = locationId,
            Status = EquipmentStatus.Available,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public static Equipment Reconstitute(EquipmentId id, string name, string type,
        LocationId locationId, EquipmentStatus status, DateTimeOffset? lastServiceDate = null)
    {
        Guard.Against.NullOrWhiteSpace(name, nameof(name));
        Guard.Against.NullOrWhiteSpace(type, nameof(type));
        return new Equipment
        {
            Id = id,
            Name = name,
            Type = type,
            LocationId = locationId,
            Status = status,
            LastServiceDate = lastServiceDate,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
}
