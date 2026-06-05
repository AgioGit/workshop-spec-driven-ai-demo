using Application.UseCases.GetEquipment;
using Ardalis.Specification.EntityFrameworkCore;
using Core.Domain.EquipmentAggregate;
using Core.Domain.EquipmentAggregate.Specifications;
using Infrastructure.Data.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Core.Domain.Tests.EquipmentAggregate;

public class EquipmentTests
{
    [Fact]
    public void Create_WithValidArguments_ReturnsEquipmentWithAvailableStatus()
    {
        var locationId = new LocationId(Guid.NewGuid());
        var equipment = Equipment.Create("Drill Rig Alpha", "Drill", locationId);

        Assert.Equal("Drill Rig Alpha", equipment.Name);
        Assert.Equal("Drill", equipment.Type);
        Assert.Equal(locationId, equipment.LocationId);
        Assert.Equal(EquipmentStatus.Available, equipment.Status);
        Assert.NotEqual(default, equipment.Id);
    }

    [Fact]
    public void Create_WithNullName_ThrowsArgumentException()
    {
        var locationId = new LocationId(Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => Equipment.Create(null!, "Drill", locationId));
    }

    [Fact]
    public void Create_WithEmptyName_ThrowsArgumentException()
    {
        var locationId = new LocationId(Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => Equipment.Create("", "Drill", locationId));
    }

    private static BookingDbContext CreateDb(IEnumerable<Equipment> seed)
    {
        var opts = new DbContextOptionsBuilder<BookingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new BookingDbContext(opts);
        db.Equipment.AddRange(seed);
        db.SaveChanges();
        return db;
    }

    private static readonly LocationId LocA = new(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly LocationId LocB = new(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static List<Equipment> SeedData() =>
    [
        Equipment.Create("Alpha", "Drill", LocA),
        Equipment.Create("Beta", "Loader", LocA),
        Equipment.Create("Gamma", "Excavator", LocB),
    ];

    [Fact]
    public async Task AllEquipmentSpec_ReturnsAllEquipment_OrderedByName()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var spec = new AllEquipmentSpec(0, null);
        var result = await repo.ListAsync(spec);

        Assert.Equal(3, result.Count);
        Assert.Equal("Alpha", result[0].Name);
        Assert.Equal("Beta", result[1].Name);
        Assert.Equal("Gamma", result[2].Name);
    }

    [Fact]
    public async Task EquipmentByLocationSpec_FiltersToCorrectLocation()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var spec = new EquipmentByLocationSpec(LocA, null, 0, null);
        var result = await repo.ListAsync(spec);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(LocA, e.LocationId));
    }

    [Fact]
    public async Task EquipmentByLocationSpec_WithStatusFilter_FiltersToCorrectStatus()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var spec = new EquipmentByLocationSpec(LocA, EquipmentStatus.Available, 0, null);
        var result = await repo.ListAsync(spec);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.Equal(EquipmentStatus.Available, e.Status));
    }

    [Fact]
    public async Task EquipmentByLocationSpec_WithUnknownLocation_ReturnsEmpty()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var unknownLoc = new LocationId(Guid.NewGuid());
        var spec = new EquipmentByLocationSpec(unknownLoc, null, 0, null);
        var result = await repo.ListAsync(spec);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AllEquipmentSpec_WithSkip_AppliesPaginationOffset()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var spec = new AllEquipmentSpec(1, null);
        var result = await repo.ListAsync(spec);

        Assert.Equal(2, result.Count);
        Assert.Equal("Beta", result[0].Name);
    }

    [Fact]
    public async Task AllEquipmentSpec_WithTake_LimitsResults()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var spec = new AllEquipmentSpec(0, 2);
        var result = await repo.ListAsync(spec);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_WithNoLocationId_ReturnsAllEquipment()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var interactor = new GetEquipmentInteractor(repo);
        var request = new GetEquipmentRequest(null, null, 0, null);
        var result = await interactor.Handle(request, CancellationToken.None);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task Handle_WithLocationId_ReturnsFilteredEquipment()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var interactor = new GetEquipmentInteractor(repo);
        var request = new GetEquipmentRequest(LocA, null, 0, null);
        var result = await interactor.Handle(request, CancellationToken.None);

        Assert.Equal(2, result.Count);
        Assert.All(result, d => Assert.Equal(LocA, d.LocationId));
    }


    [Fact]
    public async Task Handle_WithUnknownLocationId_ReturnsEmptyList()
    {
        using var db = CreateDb(SeedData());
        var repo = new EquipmentReadRepositoryForTest(db);
        var interactor = new GetEquipmentInteractor(repo);
        var request = new GetEquipmentRequest(new LocationId(Guid.NewGuid()), null, 0, null);
        var result = await interactor.Handle(request, CancellationToken.None);

        Assert.Empty(result);
    }
}

file class EquipmentReadRepositoryForTest : RepositoryBase<Equipment>, Core.Domain.EquipmentAggregate.Repositories.IEquipmentReadRepository
{
    public EquipmentReadRepositoryForTest(BookingDbContext db) : base(db) { }
}
