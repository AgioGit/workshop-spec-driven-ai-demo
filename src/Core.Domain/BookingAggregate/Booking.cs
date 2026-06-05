using StronglyTypedIds;
using Core.Domain.EquipmentAggregate;
using Core.Domain.OperatorAggregate;

namespace Core.Domain.BookingAggregate;

[StronglyTypedId(converters: StronglyTypedIdConverter.SystemTextJson)]
public partial struct BookingId { }

public class Booking
{
    public BookingId Id { get; private set; }
    public EquipmentId EquipmentId { get; private set; }
    public OperatorId OperatorId { get; private set; }
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }

    private Booking() { }

    public static Booking Create(EquipmentId equipmentId, OperatorId operatorId,
        DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("EndTime must be strictly after StartTime.", nameof(endTime));
        return new Booking
        {
            Id = new BookingId(Guid.NewGuid()),
            EquipmentId = equipmentId,
            OperatorId = operatorId,
            StartTime = startTime,
            EndTime = endTime
        };
    }

    public static Booking Reconstitute(BookingId id, EquipmentId equipmentId, OperatorId operatorId,
        DateTimeOffset startTime, DateTimeOffset endTime)
    {
        if (endTime <= startTime)
            throw new ArgumentException("EndTime must be strictly after StartTime.", nameof(endTime));
        return new Booking
        {
            Id = id,
            EquipmentId = equipmentId,
            OperatorId = operatorId,
            StartTime = startTime,
            EndTime = endTime
        };
    }
}
