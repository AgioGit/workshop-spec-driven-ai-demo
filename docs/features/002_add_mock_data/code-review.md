# Code Review: PBI 002 – add_mock_data

## Scope

Fresh-context code review of all files introduced or modified by PBI 002 (`add_mock_data`).  
This repository has no git history, so the review is performed against the implementation described in `implementation.md` and confirmed by direct file inspection.

Reviewer mode:

- report only substantive correctness issues
- ignore style and minor nits
- cross-reference findings with `plan.md` and `specification.md`

## Summary

**Status: All findings resolved. ✅ Approved.**

All 33 tests pass (0 failed). Four findings from the original review have been fixed:

| Finding | Severity | Status |
|---|---|---|
| 1 — Null-forgiving `!` in `DataSeeder.Deserialize` | Medium | ✅ Resolved |
| 2 — `Booking` missing time-range guard | Medium | ✅ Resolved |
| 3 — `Equipment.Reconstitute` omits `LastServiceDate` | Minor | ✅ Resolved |
| 4 — `Equipment.Reconstitute` guard style inconsistency | Minor | ✅ Resolved |

---

## Findings

### Finding 1: Null-forgiving `!` on `JsonSerializer.Deserialize` swallows deserialisation failures

- **Severity:** Medium
- **File:** `src/Infrastructure.Data/Seeding/DataSeeder.cs` (line 22)

#### Problem

```csharp
T Deserialize<T>(string fileName) =>
    JsonSerializer.Deserialize<T>(
        File.ReadAllText(Path.Combine(dataDirectory, fileName)), JsonOptions)!;
```

`JsonSerializer.Deserialize<T>` returns `null` when the JSON file contains the literal value `null`. The `!` operator suppresses the compiler warning but does not prevent a `NullReferenceException` from propagating when the caller immediately invokes `.Select()` on the result. The stack trace at that point points to the LINQ chain rather than the bad file, making the root cause hard to diagnose.

#### Why it matters

In a workshop setting the JSON files are edited by hand. If a participant truncates a file or saves `null`, the API fails at startup with a cryptic `NullReferenceException` instead of a message naming the offending file.

#### Recommended action

Replace the null-forgiving operator with an explicit guard:

```csharp
T Deserialize<T>(string fileName)
{
    var json = File.ReadAllText(Path.Combine(dataDirectory, fileName));
    return JsonSerializer.Deserialize<T>(json, JsonOptions)
        ?? throw new InvalidOperationException(
               $"DataSeeder: '{fileName}' deserialised to null. Verify the file contains a valid JSON array.");
}
```

#### Resolution

✅ **Resolved.** Replaced the `!` null-forgiving operator with `?? throw new InvalidOperationException(...)` naming the offending file. Also extended `EquipmentSeedRecord` with an optional `DateTimeOffset? LastServiceDate` field (see Finding 3 resolution).

---

### Finding 2: `Booking.Create` and `Booking.Reconstitute` accept invalid time ranges silently

- **Severity:** Medium
- **File:** `src/Core.Domain/BookingAggregate/Booking.cs`

#### Problem

Neither `Create` nor `Reconstitute` validate that `startTime < endTime`. A booking with `EndTime <= StartTime` is accepted without complaint and persisted to the database.

#### Why it matters

A booking with a zero or negative duration is semantically meaningless. Accepting it violates domain invariants and could confuse workshop attendees inspecting the seeded data. All other aggregates guard their string inputs; time-range integrity is the analogous invariant for `Booking`.

#### Recommended action

Add a guard in `Create` (and mirror it in `Reconstitute`):

```csharp
public static Booking Create(EquipmentId equipmentId, OperatorId operatorId,
    DateTimeOffset startTime, DateTimeOffset endTime)
{
    if (endTime <= startTime)
        throw new ArgumentException("EndTime must be after StartTime.", nameof(endTime));
    // ...
}
```

#### Resolution

✅ **Resolved.** Added `if (endTime <= startTime) throw new ArgumentException(...)` to both `Booking.Create` and `Booking.Reconstitute`. Three new tests added: equal start/end, reversed range for `Create`; reversed range for `Reconstitute`.

---

### Finding 3: `Equipment.Reconstitute` omits `LastServiceDate` — field is always `null` after seeding

- **Severity:** Minor
- **File:** `src/Core.Domain/EquipmentAggregate/Equipment.cs`, `src/Presentation.Api/data/equipment.json`

#### Problem

`Equipment.Reconstitute` signature is:

```csharp
public static Equipment Reconstitute(EquipmentId id, string name, string type,
    LocationId locationId, EquipmentStatus status)
```

It has no `lastServiceDate` parameter. The `EquipmentSeedRecord` likewise has no such field. As a result every seeded equipment row has `LastServiceDate = null` in the API response, even though the domain model supports the field and it would make the demo data more realistic.

#### Why it matters

Minor for a demo. However, `lastServiceDate` appears in the API response contract and a missing value for all 15 rows may prompt questions from workshop attendees.

#### Recommended action

Optionally add `DateTimeOffset? lastServiceDate = null` to both `Reconstitute` and `EquipmentSeedRecord`, populate a few rows in `equipment.json`, and set `LastServiceDate = lastServiceDate` in the factory body.

#### Resolution

✅ **Resolved.** Added `DateTimeOffset? lastServiceDate = null` optional parameter to `Equipment.Reconstitute`; updated `EquipmentSeedRecord` and the `DataSeeder` call accordingly. Populated `lastServiceDate` for 9 of 15 rows in `equipment.json` (the 3 legacy rows and equipment with `InUse`/`Maintenance` status). New test `Seed_SomeEquipmentHasLastServiceDate` asserts ≥ 8 rows have a non-null service date.

---

### Finding 4: `Equipment.Reconstitute` uses manual null-check style, inconsistent with sibling aggregates

- **Severity:** Minor
- **File:** `src/Core.Domain/EquipmentAggregate/Equipment.cs`

#### Problem

`Equipment.Reconstitute` guards string inputs with:

```csharp
if (string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));
```

All four new aggregates (`Location`, `Operator`, `MaintenanceEvent`, etc.) use `Guard.Against.NullOrWhiteSpace(name, nameof(name))`. The inconsistency is because `Reconstitute` was added to an existing class that predates the `Guard.Against` convention.

#### Why it matters

Cosmetic for the demo, but adds a pattern inconsistency that will be noticed in a workshop code walk-through.

#### Recommended action

Align `Equipment.Reconstitute` (and optionally `Equipment.Create`) with the project convention:

```csharp
Guard.Against.NullOrWhiteSpace(name, nameof(name));
Guard.Against.NullOrWhiteSpace(type, nameof(type));
```

#### Resolution

✅ **Resolved.** Replaced the two manual `if`-throw checks in `Equipment.Reconstitute` with `Guard.Against.NullOrWhiteSpace`. The `Equipment.Create` method retains its original manual style (not changed — reducing noise in unrelated code).

---

## Findings dismissed as by design

<details>
<summary>Click to expand</summary>

### Idempotency guard checks only `db.Equipment.Any()`

The `DataSeeder.Seed` method returns early if `Equipment` already has rows, rather than checking all five entity tables. This could theoretically leave other tables unseeded if only Equipment was previously loaded. The plan explicitly justifies this: *"This is correct for in-memory DB (fresh per process) but safe if the startup block is ever called twice in tests."* In the in-memory DB this is a non-issue — the database is always fresh per process. **[By design — plan §8]**

### `Presentation.Api.csproj` uses `<Content Update>` not `<Content Include>`

Implementation used `Update` instead of `Include` to avoid `NETSDK1022` duplicate-include warnings from `Microsoft.NET.Sdk.Web`. Documented in `implementation.md` §2. **[By design — implementation note]**

### `Location` PK reuses `EquipmentAggregate.LocationId`

The plan described two separate `LocationId` structs. The implementation correctly collapsed them to one after discovering that StronglyTypedId v1 generates output keyed on struct name, causing a hint-name collision. Documented in `implementation.md` §1. **[By design — implementation note]**

</details>

---

## Review conclusion

**✅ Approved** — all findings resolved.

### Resolved actions

- [x] **Finding 1** — `?? throw` with file name replaces `!` in `DataSeeder.Deserialize`
- [x] **Finding 2** — `endTime <= startTime` guard added to `Booking.Create` and `Booking.Reconstitute`; 3 tests added
- [x] **Finding 3** — `lastServiceDate` optional param added to `Equipment.Reconstitute`; 9 of 15 equipment rows seeded with non-null dates; new seeder test added
- [x] **Finding 4** — `Equipment.Reconstitute` guards aligned to `Guard.Against.NullOrWhiteSpace`

**Test results after fixes:** 33 passed, 0 failed (+5 new tests).
