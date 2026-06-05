using Core.Domain.EquipmentAggregate;
using MediatR;

namespace Application.UseCases.GetEquipment;

public sealed record GetEquipmentRequest(
    LocationId? LocationId,
    EquipmentStatus? StatusFilter,
    int Skip,
    int? Take) : IRequest<IReadOnlyList<GetEquipmentData>>;

public sealed record GetEquipmentData(
    EquipmentId Id,
    string Name,
    string Type,
    EquipmentStatus Status,
    LocationId LocationId,
    DateTimeOffset? LastServiceDate);
