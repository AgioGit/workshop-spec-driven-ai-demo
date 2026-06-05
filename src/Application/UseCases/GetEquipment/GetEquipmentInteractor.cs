using Ardalis.GuardClauses;
using Core.Domain.EquipmentAggregate;
using Core.Domain.EquipmentAggregate.Repositories;
using Core.Domain.EquipmentAggregate.Specifications;
using MediatR;

namespace Application.UseCases.GetEquipment;

public sealed class GetEquipmentInteractor : IRequestHandler<GetEquipmentRequest, IReadOnlyList<GetEquipmentData>>
{
    private readonly IEquipmentReadRepository _repository;

    public GetEquipmentInteractor(IEquipmentReadRepository repository)
    {
        Guard.Against.Null(repository, nameof(repository));
        _repository = repository;
    }

    public async Task<IReadOnlyList<GetEquipmentData>> Handle(GetEquipmentRequest request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Equipment> equipment;

        if (request.LocationId.HasValue)
        {
            var spec = new EquipmentByLocationSpec(request.LocationId.Value, request.StatusFilter, request.Skip, request.Take);
            equipment = await _repository.ListAsync(spec, cancellationToken);
        }
        else
        {
            var spec = new AllEquipmentSpec(request.Skip, request.Take);
            equipment = await _repository.ListAsync(spec, cancellationToken);
        }

        return equipment.Select(e => new GetEquipmentData(
            e.Id,
            e.Name,
            e.Type,
            e.Status,
            e.LocationId,
            e.LastServiceDate)).ToList();
    }
}
