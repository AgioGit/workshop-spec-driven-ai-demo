# Implementation: PBI 002 – add_mock_data

## Summary

Refactored the inline seed block in `Program.cs` by extracting all mock data into five JSON files under `src/Presentation.Api/data/`, one file per entity. A static `DataSeeder` helper class in `Infrastructure.Data` reads each file, deserialises it, and calls the appropriate aggregate factory. Simultaneously, four new domain aggregates were introduced — `Location`, `Operator`, `Booking`, and `MaintenanceEvent` — along with their EF Core configurations, so the in-memory database is populated with a realistic, cross-entity dataset.

The plan was followed closely. One deviation was required: the plan described `Location` as having its own `Core.Domain.LocationAggregate.LocationId` strongly-typed struct (its PK), distinct from the existing `Core.Domain.EquipmentAggregate.LocationId`. In practice the StronglyTypedId source generator uses only the struct _name_ as the hint file name, making two structs both named `LocationId` produce a duplicate-hint collision and a generator failure. The fix was to reuse `EquipmentAggregate.LocationId` as `Location`'s PK, eliminating the second struct entirely. This matches the alternative wording in the plan: _"Location.cs will reference LocationId from there."_ All other aspects of the design are unchanged.

## Files created (15 new files)

**Domain layer**

| File | Purpose |
|---|---|
| `src/Core.Domain/LocationAggregate/Location.cs` | Aggregate root using `EquipmentAggregate.LocationId` as PK; `Create` and `Reconstitute` factories |
| `src/Core.Domain/OperatorAggregate/Operator.cs` | Aggregate root with `[StronglyTypedId] OperatorId`; `Create` and `Reconstitute` factories |
| `src/Core.Domain/BookingAggregate/Booking.cs` | Aggregate root with `[StronglyTypedId] BookingId`; `Create` and `Reconstitute` factories |
| `src/Core.Domain/MaintenanceEventAggregate/MaintenanceEvent.cs` | Aggregate root with `[StronglyTypedId] MaintenanceEventId`; `MaintenanceStatus` enum; `Create` and `Reconstitute` factories |

**Infrastructure layer**

| File | Purpose |
|---|---|
| `src/Infrastructure.Data/Persistence/Configurations/LocationConfiguration.cs` | EF value converter for `EquipmentAggregate.LocationId`; `Name` required/max 200, `Zone` required/max 100 |
| `src/Infrastructure.Data/Persistence/Configurations/OperatorConfiguration.cs` | EF value converters for `OperatorId` and `EquipmentAggregate.LocationId`; `FullName` required/max 200 |
| `src/Infrastructure.Data/Persistence/Configurations/BookingConfiguration.cs` | EF value converters for `BookingId`, `EquipmentId`, `OperatorId` |
| `src/Infrastructure.Data/Persistence/Configurations/MaintenanceEventConfiguration.cs` | EF value converters for `MaintenanceEventId`, `EquipmentId`; `Status` stored as string |
| `src/Infrastructure.Data/Seeding/DataSeeder.cs` | Static `Seed(BookingDbContext, string)` method; idempotency guard on `db.Equipment.Any()`; file-scoped seed DTOs via `file record` |

**Presentation – API**

| File | Purpose |
|---|---|
| `src/Presentation.Api/data/locations.json` | 4 Location rows; includes both legacy GUIDs as first two entries |
| `src/Presentation.Api/data/equipment.json` | 15 Equipment rows; includes the 3 legacy equipment names as first three entries |
| `src/Presentation.Api/data/operators.json` | 10 Operator rows across all four locations |
| `src/Presentation.Api/data/bookings.json` | 12 Booking rows referencing stable equipment and operator IDs |
| `src/Presentation.Api/data/maintenance-events.json` | 8 MaintenanceEvent rows in mixed statuses (`Scheduled`, `InProgress`, `Completed`) |

**Tests**

| File | Purpose |
|---|---|
| `src/Core.Domain.Tests/DataSeeding/DataSeederTests.cs` | 16 tests: 9 aggregate factory/reconstitute unit tests + 7 DataSeeder integration tests asserting counts and legacy-data preservation |

## Files modified (5 files)

| File | Change |
|---|---|
| `src/Core.Domain/EquipmentAggregate/Equipment.cs` | Added `Reconstitute(EquipmentId, string, string, LocationId, EquipmentStatus)` factory for stable-ID seeding |
| `src/Infrastructure.Data/Persistence/BookingDbContext.cs` | Added `DbSet<Location>`, `DbSet<Operator>`, `DbSet<Booking>`, `DbSet<MaintenanceEvent>`; added four new using directives |
| `src/Presentation.Api/Program.cs` | Replaced 10-line inline seed block with two-line `DataSeeder.Seed(db, dataDir)` call; removed `using Core.Domain.EquipmentAggregate`, added `using Infrastructure.Data.Seeding` |
| `src/Presentation.Api/Presentation.Api.csproj` | Added `<Content Update="data\**\*.json">` item to ensure JSON files are copied to build output |
| `src/Core.Domain.Tests/Core.Domain.Tests.csproj` | Added linked `<Content>` item pointing at `../Presentation.Api/data/**/*.json` so JSON files are copied into the test output directory at `data/*.json` |

## Implementation notes

**1. `Location` PK reuses `EquipmentAggregate.LocationId` instead of defining a new struct**

*Plan said:* `Location` defines its own `[StronglyTypedId] struct LocationId` in `Core.Domain.LocationAggregate`, coexisting with `Core.Domain.EquipmentAggregate.LocationId`.  
*Actual:* Only one `LocationId` struct exists — `Core.Domain.EquipmentAggregate.LocationId` — and `Location` uses it as its PK.  
*Why:* The StronglyTypedId v1 source generator generates output files keyed only on the struct name (e.g. `LocationId.g.cs`). Two structs sharing the name `LocationId` in different namespaces produce a duplicate hint-name collision, causing the generator to fail with `CS8785` and cascading `CS1729` errors across all six strongly-typed IDs in the solution.  
*Impact:* The plan's `using` alias strategy for disambiguating two `LocationId` types in `DataSeeder.cs` and `OperatorConfiguration.cs` was not needed. `DataSeeder.cs` uses `LocationId` directly for both the Location PK and the Equipment/Operator FK — they are the same type. The functional outcome is identical: locations, equipment, and operators all reference the same `Guid` values wrapped in the same struct.

**2. `Presentation.Api.csproj` uses `<Content Update>` not `<Content Include>`**

*Plan said:* `<Content Include="data\**\*.json">…`  
*Actual:* `<Content Update="data\**\*.json">…`  
*Why:* `Presentation.Api` uses `Microsoft.NET.Sdk.Web`, which auto-includes all `Content` items from the project directory by default. Using `Include` would register a duplicate and produce `NETSDK1022`. Using `Update` adjusts the `CopyToOutputDirectory` property on the already-included item without re-adding it.  
*Impact:* JSON files still appear in the build output directory; no functional difference.

**3. No new repository interfaces added**

Consistent with the plan: `DataSeeder` accesses `BookingDbContext` directly. No `ILocationRepository`, `IOperatorRepository`, etc. are introduced — they are out of scope for this PBI.

## API contract

No API surface change. All existing routes and response shapes are preserved.

```
GET /get-equipment?LocationId={guid}&StatusFilter={string}&Skip={int}&Take={int}
```

The only observable runtime difference is that the in-memory database now starts with **15 equipment rows** (previously 3) across **4 locations**, enabling more meaningful pagination and filtering demonstrations.

**JSON response example (first two rows after seeding):**

```json
[
  {
    "equipmentId": "00000000-0000-0000-0000-0000000e0001",
    "name": "Drill Rig Alpha",
    "type": "Drill",
    "status": "Available",
    "locationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "lastServiceDate": null
  },
  {
    "equipmentId": "00000000-0000-0000-0000-0000000e0002",
    "name": "Loader Beta",
    "type": "Loader",
    "status": "Available",
    "locationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "lastServiceDate": null
  }
]
```

## Test results

```
Core.Domain.Tests: 28 passed, 0 failed
Final solution build: Successful (0 warnings, 0 errors)
```

Commands run (from `src\`):

```bash
dotnet build
dotnet test
```

