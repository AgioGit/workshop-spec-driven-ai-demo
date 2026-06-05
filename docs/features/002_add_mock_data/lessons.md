# Lessons Learned: 002 – add_mock_data

Each lesson follows this structure for searchability and AI retrieval:
- **Header** — lesson ID + descriptive title
- **Metadata** — date discovered, severity, short summary, tags
- **Problem** — what went wrong and why
- **Fix** — concrete code or approach that resolved it
- **Rule** — generalised guidance for future features

Severity levels: 🔴 Critical · 🟠 Major · 🟡 Medium · 🔵 Minor

---

## L001 · StronglyTypedId v1 generates hint files keyed on struct name — two `LocationId` structs in different namespaces cause a fatal `CS8785` collision

| | |
|---|---|
| **Date** | 2026-06-05 |
| **Severity** | 🔴 Critical |
| **Summary** | Defining two `[StronglyTypedId]` structs with the same name (`LocationId`) in different namespaces causes the source generator to emit duplicate hint-file names, which the compiler rejects with `CS8785` and cascading `CS1729` errors across every strongly-typed ID in the solution. |
| **Tags** | `strongly-typed-id` `id-generation` `spec-deviation` `naming` `ef-core` |

### Problem

The plan proposed two separate `LocationId` structs: `Core.Domain.LocationAggregate.LocationId` (the `Location` aggregate's own PK) and `Core.Domain.EquipmentAggregate.LocationId` (the existing FK used by `Equipment` and `Operator`). StronglyTypedId v1's source generator creates output files named `{StructName}.g.cs`. When two structs share the name `LocationId` — regardless of namespace — the generator produces two files with the same hint name, the compiler cannot resolve the conflict, and the entire solution fails to build.

### Fix

```csharp
// ❌ Plan intent (fails to compile):
// Core.Domain.LocationAggregate.LocationId (new struct)
// Core.Domain.EquipmentAggregate.LocationId (existing struct)

// ✅ Actual fix: reuse the single existing struct as the Location PK
// Location.cs
using Core.Domain.EquipmentAggregate;

namespace Core.Domain.LocationAggregate;

public class Location
{
    public LocationId Id { get; private set; }  // EquipmentAggregate.LocationId
    // ...
}
```

All callers — `DataSeeder`, `LocationConfiguration`, `OperatorConfiguration` — use `LocationId` directly without ambiguity because there is only one struct.

### Rule

When two aggregates need to share a strongly-typed ID concept (e.g. a location FK), do not define two same-named structs in different namespaces. Either reuse the existing struct or give the new struct a distinct name (e.g. `LocationKey`, `SiteId`). Verify uniqueness of struct names across the solution before adding any new `[StronglyTypedId]`.

---

## L002 · `Microsoft.NET.Sdk.Web` auto-includes `Content` items — use `<Content Update>` not `<Content Include>` to set `CopyToOutputDirectory`

| | |
|---|---|
| **Date** | 2026-06-05 |
| **Severity** | 🟡 Medium |
| **Summary** | Using `<Content Include="data\**\*.json">` in a Web SDK project triggers `NETSDK1022` duplicate-item warnings because the SDK already auto-includes all project files as `Content`. Switching to `<Content Update>` adjusts the existing item's metadata without re-adding it. |
| **Tags** | `di` `spec-deviation` `missing-registration` |

### Problem

The plan specified `<Content Include="data\**\*.json"><CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory></Content>`. When applied to `Presentation.Api.csproj` (which uses `Microsoft.NET.Sdk.Web`), the build emitted `NETSDK1022: Duplicate 'Content' items were included`. The Web SDK automatically globs all files under the project directory into `Content`; an explicit `Include` registers the same files a second time.

### Fix

```xml
<!-- ❌ NETSDK1022 in Microsoft.NET.Sdk.Web projects -->
<Content Include="data\**\*.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>

<!-- ✅ Update the already-included item's metadata instead -->
<Content Update="data\**\*.json">
  <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
</Content>
```

`Microsoft.NET.Sdk` (non-Web) projects are unaffected and should continue to use `Include`.

### Rule

In `Microsoft.NET.Sdk.Web` projects, always use `<Content Update>` (not `<Content Include>`) when overriding `CopyToOutputDirectory` for files that already exist in the project directory. Reserve `Include` for files outside the project root or when explicitly opting in to auto-globbing disabled scenarios.

---

## L003 · Test projects need an explicit `<Content>` link to JSON seed files or integration tests throw `FileNotFoundException` at runtime

| | |
|---|---|
| **Date** | 2026-06-05 |
| **Severity** | 🟠 Major |
| **Summary** | Integration tests that call `DataSeeder.Seed(db, dataDir)` rely on JSON files being present at `AppContext.BaseDirectory/data/`. Without an explicit `<Content Link>` item in the test `.csproj`, the JSON files are never copied to the test output directory and all DataSeeder tests fail with `FileNotFoundException`. |
| **Tags** | `test-isolation` `ef-core` `missing-registration` |

### Problem

`DataSeeder.Seed` resolves JSON files from `Path.Combine(dataDirectory, fileName)` where `dataDirectory` is passed by the caller as `AppContext.BaseDirectory/data/`. In `Program.cs` this resolves correctly because `Presentation.Api.csproj` copies the files. In `Core.Domain.Tests` there is no copy step by default, so the `data/` folder does not exist in `bin/Debug/net9.0/` and every `DataSeeder` integration test throws at the `File.ReadAllText` call. The failure appears to be a seeding error rather than a missing-file configuration error.

### Fix

```xml
<!-- Core.Domain.Tests.csproj -->
<ItemGroup>
  <!-- Link the API's JSON data files into the test output directory -->
  <Content Include="..\Presentation.Api\data\**\*.json" Link="data\%(Filename)%(Extension)">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </Content>
</ItemGroup>
```

After this change, `data/*.json` appears alongside the test DLL in `bin/Debug/net9.0/data/` and all paths resolve correctly. No `..` traversal is needed in test code.

### Rule

Whenever a test project runs code that reads files from `AppContext.BaseDirectory`, add a `<Content Link>` item to the test `.csproj` pointing at those files with `CopyToOutputDirectory = PreserveNewest`. Verify the files appear in the build output directory before running tests. Never rely on `..` path traversal from `AppContext.BaseDirectory` in test code — it breaks when tests run from an arbitrary working directory.

---

## L004 · Aggregate factory methods must include a `Reconstitute` overload with an explicit ID before the aggregate can be used for deterministic seeding

| | |
|---|---|
| **Date** | 2026-06-05 |
| **Severity** | 🟡 Medium |
| **Summary** | `Equipment.Create` generates a fresh `Guid.NewGuid()` each run, making IDs non-deterministic. Because `bookings.json` and `maintenance-events.json` reference specific `EquipmentId` GUIDs, the seeder cannot compile until `Equipment.Reconstitute` (with an explicit `EquipmentId` parameter) is added. This was a known prerequisite, but missing it first caused a build failure. |
| **Tags** | `id-generation` `strongly-typed-id` `backward-compat` `ef-core` |

### Problem

The plan called for stable, hardcoded GUIDs across all seed files so that cross-entity references (Booking → Equipment, MaintenanceEvent → Equipment) are valid. The existing `Equipment.Create` factory generates a random GUID — usable for production but incompatible with deterministic seeding. Until `Equipment.Reconstitute` was added, `DataSeeder.cs` could not compile because no factory accepted an explicit `EquipmentId`.

### Fix

```csharp
// Equipment.cs — added alongside the existing Create factory
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

Every other new aggregate in this PBI also received a paired `Reconstitute` factory following the same pattern.

### Rule

Every aggregate root that will be seeded with deterministic IDs (from JSON, migrations, or fixture data) needs a `Reconstitute` factory that accepts an explicit typed ID. Plan for this in Phase 1 before writing any cross-entity JSON files — without it, the seeder cannot reference stable GUIDs and the build will fail. The `Create` factory is for new production instances; `Reconstitute` is for loading known-ID data.

---

## L005 · Place `DataSeeder` in `Infrastructure.Data`, not `Presentation.Api`, so it is reachable from integration tests without a circular project reference

| | |
|---|---|
| **Date** | 2026-06-05 |
| **Severity** | 🟡 Medium |
| **Summary** | Placing `DataSeeder` in `Presentation.Api` would have made it inaccessible to `Core.Domain.Tests`, because the test project does not (and should not) reference `Presentation.Api`. Moving the class to `Infrastructure.Data` — which `Core.Domain.Tests` already references — allows integration tests to call `DataSeeder.Seed` directly. |
| **Tags** | `di` `test-isolation` `ef-core` `spec-deviation` |

### Problem

An initial draft placed `DataSeeder` in `Presentation.Api/Seeding/`. Writing integration tests in `Core.Domain.Tests` that call `DataSeeder.Seed(db, dataDir)` would have required adding a project reference from `Core.Domain.Tests` to `Presentation.Api`, creating a dependency on the web host layer from a pure domain test project — an architecture violation.

### Fix

```
// ✅ Correct location
src/Infrastructure.Data/Seeding/DataSeeder.cs

// Project reference graph (no circularity):
Core.Domain.Tests → Infrastructure.Data → Core.Domain
Presentation.Api  → Infrastructure.Data
```

`Program.cs` calls `DataSeeder.Seed` via `using Infrastructure.Data.Seeding;` — identical behaviour, no new dependency.

### Rule

Place seeding helpers in the layer that owns the `DbContext` (`Infrastructure.Data`), not in the host project (`Presentation.Api`). This keeps seeder logic independently testable and avoids forcing test projects to take a host-layer dependency. `Program.cs` can always reference `Infrastructure.Data` without circularity.

---

## L006 · `JsonSerializer.Deserialize<T>` returns `null` for a `null`-valued JSON file — suppress with `!` causes a cryptic `NullReferenceException`, not a clear error

| | |
|---|---|
| **Date** | 2026-06-05 |
| **Severity** | 🟠 Major |
| **Summary** | Using the null-forgiving operator `!` on `JsonSerializer.Deserialize<T>` silences the compiler but does not prevent a `NullReferenceException` from propagating at the first `.Select()` call if a JSON file contains `null` or is otherwise undeserializable. The stack trace points to the LINQ chain, not the bad file. |
| **Tags** | `silent-failure` `null-constraint` `ef-core` |

### Problem

In `DataSeeder.cs`:

```csharp
T Deserialize<T>(string fileName) =>
    JsonSerializer.Deserialize<T>(
        File.ReadAllText(Path.Combine(dataDirectory, fileName)), JsonOptions)!;
```

If a workshop participant edits a JSON file and saves it as empty or writes the literal value `null`, `Deserialize<T>` returns `null`. The `!` suppresses the compiler warning. The first `locations.Select(r => ...)` call then throws `NullReferenceException: Object reference not set to an instance of an object` with a stack pointing into `DataSeeder.Seed`, making the root cause (bad file) non-obvious.

### Fix

```csharp
// Replace the null-forgiving operator with an explicit guard:
T Deserialize<T>(string fileName)
{
    var json = File.ReadAllText(Path.Combine(dataDirectory, fileName));
    return JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidOperationException(
               $"DataSeeder: '{fileName}' deserialised to null. " +
               $"Verify the file contains a valid JSON array.");
}
```

The error message now names the offending file, removing all ambiguity.

### Rule

Never use the null-forgiving operator `!` on `JsonSerializer.Deserialize<T>` in production or seeding code. Always replace it with `?? throw new InvalidOperationException(...)` that names the file and describes the expected format. Null-forgiving operators silence compiler warnings without providing any runtime safety.

---

## L007 · Domain aggregate `Create` factory must enforce all invariants — `Booking` accepted `EndTime <= StartTime` silently

| | |
|---|---|
| **Date** | 2026-06-05 |
| **Severity** | 🟡 Medium |
| **Summary** | `Booking.Create` was implemented without a guard for `EndTime <= StartTime`, meaning a zero-duration or reversed booking is accepted and persisted. The domain invariant was not listed in the spec but is logically required — caught only in code review. |
| **Tags** | `null-constraint` `silent-failure` `backward-compat` |

### Problem

`Booking.Create` and `Booking.Reconstitute` perform no validation on the time range:

```csharp
public static Booking Create(EquipmentId equipmentId, OperatorId operatorId,
    DateTimeOffset startTime, DateTimeOffset endTime)
{
    return new Booking { ..., StartTime = startTime, EndTime = endTime };
}
```

A booking where `EndTime == StartTime` or `EndTime < StartTime` passes all factory checks and is inserted into the database. The issue was invisible in unit tests because all test bookings used `start.AddHours(8)` and none tested the boundary.

### Fix

```csharp
public static Booking Create(EquipmentId equipmentId, OperatorId operatorId,
    DateTimeOffset startTime, DateTimeOffset endTime)
{
    if (endTime <= startTime)
        throw new ArgumentException(
            "EndTime must be strictly after StartTime.", nameof(endTime));

    return new Booking
    {
        Id = new BookingId(Guid.NewGuid()),
        EquipmentId = equipmentId,
        OperatorId = operatorId,
        StartTime = startTime,
        EndTime = endTime
    };
}
```

Mirror the guard in `Reconstitute` as well.

### Rule

For every aggregate that models a time-bounded entity (`Booking`, `Shift`, `Reservation`, `DowntimePeriod`), always validate `EndTime > StartTime` in both `Create` and `Reconstitute`. Add a corresponding test case for the boundary condition (`endTime == startTime` should throw). Do not rely on spec completeness — temporal invariants are domain-obvious and should be in every time-range aggregate by default.

---
