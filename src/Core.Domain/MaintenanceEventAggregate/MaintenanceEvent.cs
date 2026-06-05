using Ardalis.GuardClauses;
using Core.Domain.EquipmentAggregate;
using StronglyTypedIds;

namespace Core.Domain.MaintenanceEventAggregate;

[StronglyTypedId(converters: StronglyTypedIdConverter.SystemTextJson)]
public partial struct MaintenanceEventId { }

public enum MaintenanceStatus { Scheduled, InProgress, Completed }

public class MaintenanceEvent
{
    public MaintenanceEventId Id { get; private set; }
    public EquipmentId EquipmentId { get; private set; }
    public DateTimeOffset ScheduledDate { get; private set; }
    public DateTimeOffset? CompletedDate { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public MaintenanceStatus Status { get; private set; }

    private MaintenanceEvent() { }

    public static MaintenanceEvent Create(EquipmentId equipmentId, DateTimeOffset scheduledDate,
        string description)
    {
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        return new MaintenanceEvent
        {
            Id = new MaintenanceEventId(Guid.NewGuid()),
            EquipmentId = equipmentId,
            ScheduledDate = scheduledDate,
            Description = description,
            Status = MaintenanceStatus.Scheduled
        };
    }

    public static MaintenanceEvent Reconstitute(MaintenanceEventId id, EquipmentId equipmentId,
        DateTimeOffset scheduledDate, DateTimeOffset? completedDate, string description,
        MaintenanceStatus status)
    {
        Guard.Against.NullOrWhiteSpace(description, nameof(description));
        return new MaintenanceEvent
        {
            Id = id,
            EquipmentId = equipmentId,
            ScheduledDate = scheduledDate,
            CompletedDate = completedDate,
            Description = description,
            Status = status
        };
    }
}
