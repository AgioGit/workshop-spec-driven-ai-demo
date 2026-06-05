using Ardalis.Specification;

namespace Core.Domain.EquipmentAggregate.Specifications;

public sealed class EquipmentByLocationSpec : Specification<Equipment>
{
    public EquipmentByLocationSpec(LocationId locationId, EquipmentStatus? statusFilter, int skip, int? take)
    {
        Query.Where(e => e.LocationId == locationId);
        if (statusFilter.HasValue)
            Query.Where(e => e.Status == statusFilter.Value);
        Query.OrderBy(e => e.Name).ThenBy(e => e.Id);
        Query.Skip(skip);
        if (take.HasValue)
            Query.Take(take.Value);
    }
}
