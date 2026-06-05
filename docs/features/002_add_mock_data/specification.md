# Task: PBI 002 – add_mock_data

## Feature: Move mock data from `Program.cs` into JSON files in a data folder, expand the data size, and add relevant new mock data entities

**Type**: refactoring

The current seed data is inline in `Program.cs` (3 Equipment rows, 2 hardcoded `LocationId` GUIDs). This makes it hard to extend, share, or read. The goal is to:

1. Extract all seed data into `.json` files under a `data/` folder
2. Expand the data to a realistic volume (10–20 records per entity)
3. Seed additional domain entities that are already part of the domain model (`Location`, `Operator`, `Booking`, `MaintenanceEvent`)

---

## Scope

| Layer | Files / components affected | Notes |
|---|---|---|
| `Presentation.Api` | `Program.cs` | Replace inline seed block with JSON-loading seed logic |
| `Presentation.Api` | `data/*.json` | New files: one per entity type |
| `Infrastructure.Data` | `Persistence/BookingDbContext.cs` | May need `DbSet` additions for new entities if not already present |
| `Core.Domain` | Aggregates for `Location`, `Operator`, `Booking`, `MaintenanceEvent` | Read-only for this PBI — no domain changes unless factory methods are missing |

---

## Current state

Seed data is hardcoded directly in the startup block of `Program.cs`:

```csharp
var locationA = new LocationId(Guid.Parse("7c9e6679-7425-40de-944b-e07fc1f90ae7"));
var locationB = new LocationId(Guid.Parse("8d0f7780-8536-51ef-a55c-f18fd2a01bf8"));

db.Equipment.AddRange(
    Equipment.Create("Drill Rig Alpha", "Drill", locationA),
    Equipment.Create("Loader Beta", "Loader", locationA),
    Equipment.Create("Excavator Gamma", "Excavator", locationB)
);
```

### Known issues / pain points

- Only 3 Equipment rows — too few to demonstrate pagination or filtering meaningfully
- No `Location`, `Operator`, `Booking`, or `MaintenanceEvent` data
- GUIDs are magic strings buried in startup code
- Cannot be used or reviewed without reading C# code

---

## Target state

Seed data lives in `src/Presentation.Api/data/` as JSON files, one per entity:

```
src/Presentation.Api/data/
├── locations.json
├── equipment.json
├── operators.json
├── bookings.json
└── maintenance-events.json
```

### JSON format example — `locations.json`

```json
[
  { "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7", "name": "Site Alpha", "zone": "North" },
  ...
]
```

### JSON format example — `equipment.json`

```json
[
  { "id": "...", "name": "Drill Rig Alpha", "type": "Drill", "locationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7", "status": "Available" },
  ...
]
```

`Program.cs` startup block reads each file, deserialises it, and calls the appropriate aggregate factory or constructor before saving to the in-memory DB.

### Data volume targets

| Entity | Minimum rows |
|---|---|
| Location | 4 |
| Equipment | 15 |
| Operator | 10 |
| Booking | 12 |
| MaintenanceEvent | 8 |

### Changed behaviour (if any)

This is a **pure internal change**. No API response shapes, routes, or behaviour change. The in-memory DB is populated identically from JSON rather than from inline code.

### Breaking changes

None. All existing GUIDs used as `locationA` / `locationB` must be preserved in the JSON files so any existing tests or manual curl calls continue to work.

| Before | After | Notes |
|---|---|---|
| Hardcoded `LocationId` GUIDs in `Program.cs` | Same GUIDs in `locations.json` | Must not change |
| 3 Equipment rows | 15 Equipment rows | Superset — no removals |

---

## Testing requirements

- Existing `dotnet test` suite must pass unchanged after the refactor
- Manually verify the API returns data after startup (curl `/get-equipment`)
- Add an integration test that seeds the in-memory DB from the JSON files and asserts the correct record counts per entity

---

## References

- Similar implementations: `docs/features/001_get_equipment/`
- Architecture: `docs/ARCHITECTURE.md`
- Source file being refactored: `src/Presentation.Api/Program.cs`
- Domain aggregates: `src/Core.Domain/`
