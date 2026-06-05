# Plan: PBI 002 – add_mock_data

## 1. Overview

Refactors the startup seed block in `Program.cs` by moving all mock data into JSON files under `src/Presentation.Api/data/`, one file per entity. The startup block is replaced by a `DataSeeder` helper class that reads each file, deserialises it, and calls the appropriate aggregate factory. Simultaneously, four new domain aggregates are introduced — `Location`, `Operator`, `Booking`, and `MaintenanceEvent` — so the in-memory DB is populated with a realistic, cross-entity dataset. This is a **pure internal change**: no API routes, response shapes, or test interfaces change.

**No API surface change.** All existing `GET /get-equipment` behaviour is preserved.

**Data volume targets:**

| Entity | Minimum rows |
|---|---|
| Location | 4 |
| Equipment | 15 |
| Operator | 10 |
| Booking | 12 |
| MaintenanceEvent | 8 |

## 2. Legacy behaviour to preserve

- The two existing `LocationId` GUIDs (`7c9e6679-7425-40de-944b-e07fc1f90ae7` and `8d0f7780-8536-51ef-a55c-f18fd2a01bf8`) must appear unchanged in `locations.json`. Any existing curl calls or tests that use these specific GUIDs must continue to work.
- The three original Equipment names (`Drill Rig Alpha`, `Loader Beta`, `Excavator Gamma`) must appear in `equipment.json` as a subset of the 15 rows. No removals — only additions.
- `Equipment.Create` generates a fresh `Guid.NewGuid()` each run, making IDs non-deterministic. For seeding to work with stable cross-entity references (e.g. Booking must point to a known EquipmentId), each aggregate needs a `Reconstitute` factory that accepts an explicit `Id`.

## 3. Recommended design

- **New domain aggregates** (`Location`, `Operator`, `Booking`, `MaintenanceEvent`) are added to `Core.Domain`, each following the `EquipmentAggregate` pattern: aggregate root, `[StronglyTypedId]`, private constructor, `Create` factory with guard clauses, and a `Reconstitute` factory for seeding with explicit IDs.
- **`LocationId` stays in `Equipment.cs`** for this PBI. Moving it to a `LocationAggregate` namespace would cascade across 5 files and is separate refactor scope. `Location.cs` will reference `LocationId` from `Core.Domain.EquipmentAggregate`.
- **`Equipment.Reconstitute`** must be added to `Equipment.cs` in Phase 1 — the seeder needs to set a stable `EquipmentId` from the JSON; the existing `Create` factory generates a random GUID each run.
- **`DataSeeder`** is a static helper class in **`Infrastructure.Data`** (not `Presentation.Api`). This keeps file I/O in the data layer and makes the class accessible from `Core.Domain.Tests`, which already references `Infrastructure.Data`. `Program.cs` only invokes it.
- **Idempotency guard**: `DataSeeder.Seed` begins with `if (db.Equipment.Any()) return;` to prevent duplicate rows on repeated calls.
- **Simple seed DTOs** (C# `record`s, file-scoped to `DataSeeder.cs`) handle JSON deserialisation. They hold only raw JSON fields as `string`/`Guid` to avoid coupling System.Text.Json to domain types.
- **EF Core `DbSet` additions** for all four new aggregates go into `BookingDbContext`. An `IEntityTypeConfiguration<T>` is added for each entity to register strongly-typed ID value converters.
- **No new repository interfaces** are needed for this PBI — the seeder accesses `DbContext` directly.
- **`data/*.json` file copy** is configured in both `Presentation.Api.csproj` and `Core.Domain.Tests.csproj`. The test project uses a `<Content>` link item pointing at `Presentation.Api/data/**/*.json` with `CopyToOutputDirectory = PreserveNewest`, so tests resolve JSON from `AppContext.BaseDirectory/data/` without any `..` path traversal.

## 4. Files to create

**Domain layer — `src\Core.Domain\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 1 | `src\Core.Domain\LocationAggregate\Location.cs` | Aggregate root with `[StronglyTypedId] LocationId` *(separate from `EquipmentAggregate.LocationId` — see §7)*, `Create(name, zone)` and `Reconstitute` factories |
| 2 | `src\Core.Domain\OperatorAggregate\Operator.cs` | Aggregate root with `[StronglyTypedId] OperatorId`, `Create(fullName, certLevel, locationId)` and `Reconstitute` factories |
| 3 | `src\Core.Domain\BookingAggregate\Booking.cs` | Aggregate root with `[StronglyTypedId] BookingId`, `Create(equipmentId, operatorId, start, end)` and `Reconstitute` factories |
| 4 | `src\Core.Domain\MaintenanceEventAggregate\MaintenanceEvent.cs` | Aggregate root with `[StronglyTypedId] MaintenanceEventId`, `MaintenanceStatus` enum, `Create(equipmentId, scheduledDate, description)` and `Reconstitute` factories |

**Infrastructure layer — `src\Infrastructure.Data\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 5 | `src\Infrastructure.Data\Persistence\Configurations\LocationConfiguration.cs` | Value converters for `LocationId` (own ID only); Name required, max 200 |
| 6 | `src\Infrastructure.Data\Persistence\Configurations\OperatorConfiguration.cs` | Value converters for `OperatorId` and `EquipmentAggregate.LocationId`; FullName required |
| 7 | `src\Infrastructure.Data\Persistence\Configurations\BookingConfiguration.cs` | Value converters for `BookingId`, `EquipmentId`, `OperatorId` |
| 8 | `src\Infrastructure.Data\Persistence\Configurations\MaintenanceEventConfiguration.cs` | Value converters for `MaintenanceEventId`, `EquipmentId`; Status stored as string |
| 9 | `src\Infrastructure.Data\Seeding\DataSeeder.cs` | Static helper with `Seed(BookingDbContext, string)` method; file-scoped seed DTOs; idempotency guard |

**Presentation – API — `src\Presentation.Api\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 10 | `src\Presentation.Api\data\locations.json` | 4 Location rows (includes the two legacy GUIDs as the first two entries) |
| 11 | `src\Presentation.Api\data\equipment.json` | 15 Equipment rows (includes the 3 legacy rows) |
| 12 | `src\Presentation.Api\data\operators.json` | 10 Operator rows |
| 13 | `src\Presentation.Api\data\bookings.json` | 12 Booking rows |
| 14 | `src\Presentation.Api\data\maintenance-events.json` | 8 MaintenanceEvent rows |

**Test files — `src\Core.Domain.Tests\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 15 | `src\Core.Domain.Tests\DataSeeding\DataSeederTests.cs` | Integration test that calls `DataSeeder` with the real `data/` folder and asserts correct record counts for all five entity types |

## 5. Files to modify

| # | Exact file path | What changes and why |
|---|---|---|
| 1 | `src\Core.Domain\EquipmentAggregate\Equipment.cs` | Add `Reconstitute(EquipmentId, string, string, LocationId, EquipmentStatus)` factory — required by `DataSeeder` to seed with stable IDs |
| 2 | `src\Infrastructure.Data\Persistence\BookingDbContext.cs` | Add `DbSet<Location>`, `DbSet<Operator>`, `DbSet<Booking>`, `DbSet<MaintenanceEvent>` |
| 3 | `src\Presentation.Api\Program.cs` | Replace inline seed block with `DataSeeder.Seed(db, dataDir)` using `AppContext.BaseDirectory` |
| 4 | `src\Presentation.Api\Presentation.Api.csproj` | Add `<Content Include="data\**\*.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>` |
| 5 | `src\Core.Domain.Tests\Core.Domain.Tests.csproj` | Add linked `<Content>` item pointing at `../Presentation.Api/data/**/*.json` with `CopyToOutputDirectory = PreserveNewest` so tests resolve JSON from `AppContext.BaseDirectory/data/` |

> **Note on `LocationId`:** `LocationId` stays in `Equipment.cs` / `Core.Domain.EquipmentAggregate` for this PBI. `Location.cs` references it from there — no namespace changes to existing files. Relocating `LocationId` is out of scope.

> **Note on packages:** No new NuGet packages are needed. `System.Text.Json` is available in .NET 9; all EF Core and Ardalis packages are already referenced.

## 6. Detailed implementation order (TDD)

### Phase 1: `Equipment.Reconstitute` + new domain aggregates

Add `Equipment.Reconstitute` to make `Equipment.cs` compilable by the seeder:

```csharp
public static Equipment Reconstitute(EquipmentId id, string name, string type,
    LocationId locationId, EquipmentStatus status)
{
    if (string.IsNullOrWhiteSpace(name))
        throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
    if (string.IsNullOrWhiteSpace(type))
        throw new ArgumentException("Value cannot be null or whitespace.", nameof(type));
    return new Equipment
    {
        Id = id, Name = name, Type = type,
        LocationId = locationId, Status = status,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
```

Write these test cases in `Core.Domain.Tests\DataSeeding\DataSeederTests.cs` **first** (red), then implement the four new aggregates (green):

- `Equipment_Reconstitute_PreservesIdAndStatus`
- `Location_Create_WithValidArguments_SetsProperties`
- `Operator_Create_WithValidArguments_SetsProperties`
- `Booking_Create_WithValidArguments_SetsStartAndEnd`
- `MaintenanceEvent_Create_WithValidArguments_SetsStatusToScheduled`
- `Location_Reconstitute_PreservesId`
- `Operator_Reconstitute_PreservesId`
- `Booking_Reconstitute_PreservesId`
- `MaintenanceEvent_Reconstitute_PreservesId`

Implement:

1. `Equipment.Reconstitute` in `src\Core.Domain\EquipmentAggregate\Equipment.cs`

2. `src\Core.Domain\LocationAggregate\Location.cs` — **`LocationId` stays in `Equipment.cs`**; `Location` has its own `[StronglyTypedId] struct LocationId` for its primary key only; the `Zone` field links by the equipment-namespace `LocationId` value from the JSON.

   > **Note on two `LocationId` structs:** The `Location` aggregate's own primary key is `Core.Domain.LocationAggregate.LocationId`. The foreign-key field on `Operator` (pointing to a location) reuses `Core.Domain.EquipmentAggregate.LocationId` because `Equipment.LocationId` is already that type. This avoids any rename cascade. Both structs wrap a `Guid`; the EF configurations disambiguate them by column.

3. `src\Core.Domain\OperatorAggregate\Operator.cs` — `OperatorId`, `FullName`, `CertificationLevel`, `Core.Domain.EquipmentAggregate.LocationId`, `IsActive`
4. `src\Core.Domain\BookingAggregate\Booking.cs` — `BookingId`, `EquipmentId`, `OperatorId`, `StartTime`, `EndTime`
5. `src\Core.Domain\MaintenanceEventAggregate\MaintenanceEvent.cs` — `MaintenanceEventId`, `EquipmentId`, `ScheduledDate`, `CompletedDate?`, `Description`, `MaintenanceStatus` enum (`Scheduled`, `InProgress`, `Completed`)

### Phase 2: EF Core infrastructure

Implement EF configurations (no additional test cases — the in-memory DB pattern is already exercised by `EquipmentTests`):

1. `LocationConfiguration.cs` — `HasKey(l => l.Id)`, `HasConversion(id => id.Value, v => new Core.Domain.LocationAggregate.LocationId(v))`, `Name` required/max 200, `Zone` required/max 100
2. `OperatorConfiguration.cs` — converters for `OperatorId` and `Core.Domain.EquipmentAggregate.LocationId`
3. `BookingConfiguration.cs` — converters for `BookingId`, `EquipmentId`, `OperatorId`
4. `MaintenanceEventConfiguration.cs` — converters for `MaintenanceEventId`, `EquipmentId`; `Status` stored as string

Update `BookingDbContext.cs`:

```csharp
public DbSet<Location>          Locations          { get; set; } = null!;
public DbSet<Operator>          Operators          { get; set; } = null!;
public DbSet<Booking>           Bookings           { get; set; } = null!;
public DbSet<MaintenanceEvent>  MaintenanceEvents  { get; set; } = null!;
```

### Phase 3: JSON data files

Create `src/Presentation.Api/data/` and populate each file. Key constraints:

**`locations.json`** — first two entries must use the legacy GUIDs:
```json
[
  { "id": "7c9e6679-7425-40de-944b-e07fc1f90ae7", "name": "Site Alpha", "zone": "North" },
  { "id": "8d0f7780-8536-51ef-a55c-f18fd2a01bf8", "name": "Site Beta",  "zone": "South" },
  { "id": "...", "name": "Site Gamma", "zone": "East" },
  { "id": "...", "name": "Site Delta", "zone": "West" }
]
```

**`equipment.json`** — first three rows must match legacy entries (same names, same locationId GUIDs):
```json
[
  { "id": "...", "name": "Drill Rig Alpha", "type": "Drill",     "locationId": "7c9e6679-...", "status": "Available" },
  { "id": "...", "name": "Loader Beta",     "type": "Loader",    "locationId": "7c9e6679-...", "status": "Available" },
  { "id": "...", "name": "Excavator Gamma", "type": "Excavator", "locationId": "8d0f7780-...", "status": "Available" },
  ... (12 more rows)
]
```

All IDs across all files must use stable, hardcoded GUIDs so `bookings.json` and `maintenance-events.json` can reference valid EquipmentId and OperatorId values.

Update `Presentation.Api.csproj`:
```xml
<ItemGroup>
  <Content Include="data\**\*.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

Also update `Core.Domain.Tests.csproj` to link the same files into the test output (no `..` traversal in tests):
```xml
<ItemGroup>
  <Content Include="..\Presentation.Api\data\**\*.json" Link="data\%(Filename)%(Extension)">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

### Phase 4: DataSeeder implementation

Write test cases first (in `DataSeederTests.cs`):

- `Seed_LoadsCorrectLocationCount` — asserts `db.Locations.Count() >= 4`
- `Seed_LoadsCorrectEquipmentCount` — asserts `db.Equipment.Count() >= 15`
- `Seed_LoadsCorrectOperatorCount` — asserts `db.Operators.Count() >= 10`
- `Seed_LoadsCorrectBookingCount` — asserts `db.Bookings.Count() >= 12`
- `Seed_LoadsCorrectMaintenanceEventCount` — asserts `db.MaintenanceEvents.Count() >= 8`
- `Seed_PreservesLegacyLocationGuids` — finds both legacy LocationId GUIDs in `db.Locations`
- `Seed_PreservesLegacyEquipmentNames` — finds `Drill Rig Alpha`, `Loader Beta`, `Excavator Gamma` in `db.Equipment`

Then implement `src\Infrastructure.Data\Seeding\DataSeeder.cs`:

```csharp
public static class DataSeeder
{
    private static readonly JsonSerializerOptions JsonOptions =
        new() { PropertyNameCaseInsensitive = true };

    public static void Seed(BookingDbContext db, string dataDirectory)
    {
        if (db.Equipment.Any()) return;  // idempotency guard

        var locations = Deserialize<LocationSeedRecord[]>("locations.json");
        db.Locations.AddRange(locations.Select(r =>
            Location.Reconstitute(new Core.Domain.LocationAggregate.LocationId(r.Id), r.Name, r.Zone)));

        var equipment = Deserialize<EquipmentSeedRecord[]>("equipment.json");
        db.Equipment.AddRange(equipment.Select(r =>
            Equipment.Reconstitute(new EquipmentId(r.Id), r.Name, r.Type,
                new LocationId(r.LocationId), Enum.Parse<EquipmentStatus>(r.Status, ignoreCase: true))));

        var operators = Deserialize<OperatorSeedRecord[]>("operators.json");
        db.Operators.AddRange(operators.Select(r =>
            Operator.Reconstitute(new OperatorId(r.Id), r.FullName, r.CertificationLevel,
                new LocationId(r.LocationId), r.IsActive)));

        var bookings = Deserialize<BookingSeedRecord[]>("bookings.json");
        db.Bookings.AddRange(bookings.Select(r =>
            Booking.Reconstitute(new BookingId(r.Id), new EquipmentId(r.EquipmentId),
                new OperatorId(r.OperatorId), r.StartTime, r.EndTime)));

        var events = Deserialize<MaintenanceEventSeedRecord[]>("maintenance-events.json");
        db.MaintenanceEvents.AddRange(events.Select(r =>
            MaintenanceEvent.Reconstitute(new MaintenanceEventId(r.Id), new EquipmentId(r.EquipmentId),
                r.ScheduledDate, r.CompletedDate, r.Description,
                Enum.Parse<MaintenanceStatus>(r.Status, ignoreCase: true))));

        db.SaveChanges();

        T Deserialize<T>(string fileName) =>
            JsonSerializer.Deserialize<T>(
                File.ReadAllText(Path.Combine(dataDirectory, fileName)), JsonOptions)!;
    }
}

file record LocationSeedRecord(Guid Id, string Name, string Zone);
file record EquipmentSeedRecord(Guid Id, string Name, string Type, Guid LocationId, string Status);
file record OperatorSeedRecord(Guid Id, string FullName, string CertificationLevel, Guid LocationId, bool IsActive);
file record BookingSeedRecord(Guid Id, Guid EquipmentId, Guid OperatorId, DateTimeOffset StartTime, DateTimeOffset EndTime);
file record MaintenanceEventSeedRecord(Guid Id, Guid EquipmentId, DateTimeOffset ScheduledDate, DateTimeOffset? CompletedDate, string Description, string Status);
```

### Phase 5: Program.cs wiring

Replace the inline seed block:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
    DataSeeder.Seed(db, dataDir);
}
```

### Phase 6: Verification

Run from `src\`:

```bash
dotnet build
dotnet test
```

Expected: all existing tests pass + 9 new aggregate + 7 new DataSeeder tests = **≥ 26 passed, 0 failed**. Build produces 0 errors and 0 warnings.

## 7. Notes on contract compatibility

- **`LocationId` stays put.** `Core.Domain.EquipmentAggregate.LocationId` is unchanged. `Location.cs` defines its own `Core.Domain.LocationAggregate.LocationId` for its primary key. `Operator` and `Equipment` continue to use the equipment-namespace `LocationId` as a foreign-key value. EF configurations disambiguate by aggregate.
- JSON property names in seed files use `camelCase`. Set `PropertyNameCaseInsensitive = true` in the `JsonSerializerOptions` to avoid case-sensitivity issues with the C# record properties.
- `EquipmentStatus` values in `equipment.json` must match enum member names exactly (`Available`, `InUse`, `Maintenance`). Use `Enum.Parse(..., ignoreCase: true)` for safety.
- `MaintenanceStatus` values in `maintenance-events.json` likewise use enum names (`Scheduled`, `InProgress`, `Completed`).
- No response DTO or API contract changes — this section is N/A for a seeding refactor.

## 8. Risks and review points

- **`Equipment.Reconstitute` is missing from current codebase** — must be added first; without it the seeder cannot compile.
- **`DataSeeder` in `Infrastructure.Data`** — `Core.Domain.Tests` already references `Infrastructure.Data`, so `DataSeederTests` can call `DataSeeder` directly. Do not place it in `Presentation.Api`.
- **Test data path** — Tests resolve `data/` from `AppContext.BaseDirectory/data/` because the test `.csproj` links the JSON files with `CopyToOutputDirectory`. If the link is missing, tests throw `FileNotFoundException`. Verify JSON files appear in `bin/Debug/net9.0/data/` after build.
- **Two `LocationId` structs** — `Core.Domain.LocationAggregate.LocationId` (Location PK) and `Core.Domain.EquipmentAggregate.LocationId` (Equipment/Operator FK) coexist. `using` aliases or fully-qualified names are needed in `OperatorConfiguration.cs` and `DataSeeder.cs` to avoid ambiguity.
- **Stable GUIDs across files** — `bookings.json` and `maintenance-events.json` must reference GUIDs that exist in `equipment.json` and `operators.json`. Generate and record all GUIDs before writing any JSON file.
- **Idempotency guard** — `if (db.Equipment.Any()) return;` prevents duplicate seeding on repeated startup. This is correct for in-memory DB (fresh per process) but safe if the startup block is ever called twice in tests.
- **`Operator` class name** — `Operator` is a valid C# class name (not a reserved keyword). If any tooling complains, rename to `EquipmentOperator`.
- **EF Core in-memory load order** — No navigation properties are planned; a single `db.SaveChanges()` at the end of `Seed` is sufficient.
