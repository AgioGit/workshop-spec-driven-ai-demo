using Application.UseCases.GetEquipment;
using Core.Domain.EquipmentAggregate;

namespace Presentation.Contracts.Equipment;

public sealed record GetEquipmentResponse(
    Guid EquipmentId,
    string Name,
    string Type,
    string Status,
    Guid LocationId,
    DateTimeOffset? LastServiceDate)
{
    public static GetEquipmentResponse From(GetEquipmentData data) =>
        new(
            data.Id.Value,
            data.Name,
            data.Type,
            data.Status.ToString(),
            data.LocationId.Value,
            data.LastServiceDate);
}
