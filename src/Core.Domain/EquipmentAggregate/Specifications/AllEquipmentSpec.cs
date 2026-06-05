using Ardalis.Specification;

namespace Core.Domain.EquipmentAggregate.Specifications;

public sealed class AllEquipmentSpec : Specification<Equipment>
{
    public AllEquipmentSpec(int skip, int? take)
    {
        Query.OrderBy(e => e.Name).ThenBy(e => e.Id);
        Query.Skip(skip);
        if (take.HasValue)
            Query.Take(take.Value);
    }
}
