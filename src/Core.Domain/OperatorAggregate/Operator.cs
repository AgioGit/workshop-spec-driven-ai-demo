using Ardalis.GuardClauses;
using Core.Domain.EquipmentAggregate;
using StronglyTypedIds;

namespace Core.Domain.OperatorAggregate;

[StronglyTypedId(converters: StronglyTypedIdConverter.SystemTextJson)]
public partial struct OperatorId { }

public class Operator
{
    public OperatorId Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public string CertificationLevel { get; private set; } = string.Empty;
    public LocationId LocationId { get; private set; }
    public bool IsActive { get; private set; }

    private Operator() { }

    public static Operator Create(string fullName, string certificationLevel, LocationId locationId)
    {
        Guard.Against.NullOrWhiteSpace(fullName, nameof(fullName));
        Guard.Against.NullOrWhiteSpace(certificationLevel, nameof(certificationLevel));
        return new Operator
        {
            Id = new OperatorId(Guid.NewGuid()),
            FullName = fullName,
            CertificationLevel = certificationLevel,
            LocationId = locationId,
            IsActive = true
        };
    }

    public static Operator Reconstitute(OperatorId id, string fullName, string certificationLevel,
        LocationId locationId, bool isActive)
    {
        Guard.Against.NullOrWhiteSpace(fullName, nameof(fullName));
        Guard.Against.NullOrWhiteSpace(certificationLevel, nameof(certificationLevel));
        return new Operator
        {
            Id = id,
            FullName = fullName,
            CertificationLevel = certificationLevel,
            LocationId = locationId,
            IsActive = isActive
        };
    }
}
