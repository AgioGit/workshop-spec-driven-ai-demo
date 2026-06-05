using Ardalis.Specification.EntityFrameworkCore;
using Core.Domain.EquipmentAggregate;
using Core.Domain.EquipmentAggregate.Repositories;

namespace Infrastructure.Data.Persistence.Repositories;

public class EquipmentReadRepository : RepositoryBase<Equipment>, IEquipmentReadRepository
{
    public EquipmentReadRepository(Persistence.BookingDbContext context) : base(context) { }
}
