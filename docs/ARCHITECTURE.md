# Architecture: Equipment Booking Demo

> **How to use this file:**  
> Place this at `docs/ARCHITECTURE.md` in the `workshop-api-demo` repo.  
> Skills reference this when planning and reviewing features.  
> It is intentionally simplified compared to production architecture docs.

---

## What this system does

The Equipment Booking Demo API manages the booking and tracking of industrial equipment across locations. It allows:
- Listing equipment by location and status
- Creating and managing bookings (reservations) of equipment
- Tracking operators and their assignments
- Recording maintenance events

---

## Solution structure

```
src/
├── Core.Domain/               # Domain layer — zero external dependencies
│   ├── EquipmentAggregate/    # Equipment entity + specs + repos
│   ├── BookingAggregate/      # Booking entity + specs + repos
│   ├── OperatorAggregate/     # Operator entity + specs + repos
│   ├── LocationAggregate/     # Location entity + specs + repos
│   └── MaintenanceAggregate/  # MaintenanceEvent entity + specs + repos
├── Application/               # Use cases (MediatR request + interactor pairs)
│   └── UseCases/
│       ├── GetEquipment/
│       ├── GetBookingsByDate/
│       └── ...
├── Infrastructure.Data/       # EF Core (in-memory for demo) + repository implementations
│   └── Persistence/
│       ├── BookingDbContext.cs
│       ├── Configurations/    # IEntityTypeConfiguration<T> implementations
│       └── Repositories/      # IEquipmentReadRepository etc.
├── Presentation.Api/          # FastEndpoints API
│   ├── Program.cs
│   └── Endpoints/
│       ├── Equipment/
│       ├── Bookings/
│       └── ...
└── Presentation.Contracts/    # Request/response DTOs
    ├── Equipment/
    ├── Bookings/
    └── ...
```

---

## Data flow

```
HTTP Request
  → FastEndpoint (Presentation.Api)
    → MediatR Send
      → Interactor (Application/UseCases)
        → ReadRepository (Core.Domain interface / Infrastructure.Data impl)
          → EF Core / Ardalis.Specification
        → Returns IReadOnlyList<TData>
      → Interactor maps to response list
    → FastEndpoint sends response DTO
```

---

## Aggregate overview

### Equipment
- **Properties:** Id (EquipmentId), Name, Type, Status (enum), LocationId, LastServiceDate, CreatedAt, UpdatedAt
- **Status values:** `Available`, `InUse`, `Maintenance`
- **Factory:** `Equipment.Create(name, type, locationId)` — sets status to Available

### Booking
- **Properties:** Id (BookingId), EquipmentId, OperatorId, StartTime, EndTime, Status, CreatedAt, UpdatedAt
- **Status values:** `Pending`, `Active`, `Completed`, `Cancelled`
- **Rule:** A booking cannot overlap another active booking for the same equipment

### Operator
- **Properties:** Id (OperatorId), FullName, CertificationLevel (enum), LocationId, IsActive, ActiveSince, CreatedAt, UpdatedAt
- **CertificationLevel values:** `Basic`, `Standard`, `Advanced`

### Location
- **Properties:** Id (LocationId), Name, Code, CreatedAt

### MaintenanceEvent
- **Properties:** Id (MaintenanceEventId), EquipmentId, ScheduledDate, CompletedDate, MaintenanceType (enum), AssignedTechnician, Notes, CreatedAt, UpdatedAt
- **MaintenanceType values:** `Scheduled`, `Emergency`, `Routine`

---

## Conventions reference

### Strongly-typed IDs

Internal keys use StronglyTypedId:
```csharp
[StronglyTypedId] public partial struct EquipmentId { }
```

Expose as plain `Guid` in response DTOs (`.Value`).  
**Never use `.Value` inside LINQ-to-EF** — EF resolves via the value converter.

### Ardalis.Specification

All queries use spec classes in `{Aggregate}Aggregate/Specifications/`:
```csharp
public sealed class EquipmentByLocationSpec : Specification<Equipment>
{
    public EquipmentByLocationSpec(LocationId locationId)
    {
        Query.Where(e => e.LocationId == locationId)
             .OrderBy(e => e.Name);
    }
}
```

### Pagination

Every list endpoint exposes `Skip` (int, default 0) and `Take` (int?, optional).  
Apply in the spec:
```csharp
Query.Skip(skip);
if (take.HasValue) Query.Take(take.Value);
```

### DTO placement rule

- DTOs with no Application dependency → `Presentation.Contracts/{Entity}/`
- DTOs whose `From(...)` factory takes an Application data record → `Presentation.Api/` (Contracts cannot reference Application)

### Endpoint pattern

```csharp
public sealed class Get : EndpointWithoutRequest<IReadOnlyList<GetEquipmentResponse>>
{
    public override void Configure()
    {
        Get("/get-equipment");
        Policies("AppAccess");
        Summary(s =>
        {
            s.Summary = "List equipment";
            s.Params["LocationId"] = "Filter to equipment at this location";
            s.Params["Skip"] = "Pagination offset";
            s.Params["Take"] = "Max records to return";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetEquipmentRequest(...), ct);
        await SendOkAsync(result.Select(GetEquipmentResponse.From).ToList(), ct);
    }
}
```

---

## Reference feature

`docs/features/001_get_equipment/` — GET /GetEquipment  
All four docs are complete and represent the target quality level for every feature.
