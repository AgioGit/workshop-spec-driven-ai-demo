# Specification: GET /GetAvailableEquipment (Worked Example)

> **Purpose of this file:** This is a fully completed `specification.md` for Card A. Use it as the template to show participants what "done" looks like. Place this as `docs/features/001_get_equipment/specification.md` in the demo repo (or as a companion reference).

---

# Task: PBI 100 – get_available_equipment

## Feature: List all equipment, optionally filtered by location and status

This endpoint belongs to `Presentation.Api`. It is a **GET-only** endpoint with no write operations or event publishing.

---

## Feature 1: GET /GetAvailableEquipment

Returns a paginated list of equipment. The caller can narrow the result by location and/or operational status. Used by the booking UI to show what equipment is available for reservation at a given site.

### Parameters

| Name | Type | Required | Default | Description |
|---|---|---|---|---|
| `LocationId` | `Guid` | No | — | If provided, return only equipment at this location. If the location ID does not exist, return an empty list. |
| `StatusFilter` | `string` | No | — | One of: `Available`, `InUse`, `Maintenance`. Case-insensitive. If omitted, all statuses are returned. An unrecognised value returns HTTP 400. |
| `Skip` | `int` | Yes | `0` | Pagination offset. Must be ≥ 0; negative values return HTTP 400. |
| `Take` | `int` | No | — | Maximum records to return. If omitted, return all matching records. Must be > 0 if provided. |

### Response mapping

| Target Property | Type | Source Aggregate | Source Property | Notes |
|---|---|---|---|---|
| `EquipmentId` | `Guid` | Equipment | `Id.Value` | Expose as plain Guid, not strongly-typed ID |
| `Name` | `string` | Equipment | `Name` | Never null |
| `Type` | `string` | Equipment | `Type` | Never null |
| `Status` | `string` | Equipment | `Status.ToString()` | One of `Available`, `InUse`, `Maintenance` |
| `LocationId` | `Guid` | Equipment | `LocationId.Value` | Expose as plain Guid |
| `LastServiceDate` | `DateTimeOffset?` | Equipment | `LastServiceDate` | Null if never serviced |

### Behavioural notes

- **StatusFilter validation** happens in the FastEndpoints validator, not in the interactor or repository. Return HTTP 400 with a descriptive message before hitting any domain logic.
- **Unknown LocationId** returns an empty list with HTTP 200 (not 404). The caller is responsible for knowing which locations exist.
- **Pagination** applies to the post-filter result set. `Skip(0).Take(10)` returns the first 10 matching equipment items.
- **Result order**: stable order by `Name` ascending, then `EquipmentId` as a tiebreaker.
- **DTO placement**: `GetEquipmentResponse` belongs in `Presentation.Contracts` — it has no Application layer dependency.

---

## References

- Similar implementations: `docs/features/001_get_equipment/` (this is the reference feature itself)
- Architecture: `docs/ARCHITECTURE.md`
- Domain aggregate: `src/Core.Domain/EquipmentAggregate/Equipment.cs`
- Repository interface: `src/Core.Domain/EquipmentAggregate/Repositories/IEquipmentReadRepository.cs`

---

## Why every section matters

> This section explains the spec to workshop participants. It would not be in a production spec.

| Section | Why it matters |
|---|---|
| **Parameters table** | Without valid values and defaults, the agent will guess. It often guesses wrong on edge cases (null handling, default values). |
| **Response mapping table** | Without this, the agent will invent property names. Property names in a public API are a contract — you cannot change them later without breaking clients. |
| **Behavioural notes** | This is where edge cases live. "Return empty list, not 404" is a decision. Document it or the agent will make its own decision. |
| **References** | Pointing to similar features is the most powerful thing in the spec. The plan will mirror proven patterns instead of inventing new ones. |
