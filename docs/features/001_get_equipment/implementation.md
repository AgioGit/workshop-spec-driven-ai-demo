# Implementation: PBI 100 – get_equipment

## Summary

Implemented `GET /get-equipment` end-to-end following the plan. All five layers were created: domain aggregate + specifications, application interactor, infrastructure EF Core repository, presentation contracts DTO, and FastEndpoints API endpoint. The plan was followed closely with one minor deviation: the `StatusFilter` validation was implemented inline in the endpoint's `HandleAsync` using `Enum.TryParse` rather than a separate `Validator<T>` class, as the endpoint uses `EndpointWithoutRequest` which cannot bind a typed request object through a validator.

## Files created (14 new files)

**Domain layer**

| File | Purpose |
|---|---|
| `src/Core.Domain/EquipmentAggregate/Equipment.cs` | Aggregate root with `Create` factory |
| `src/Core.Domain/EquipmentAggregate/Specifications/AllEquipmentSpec.cs` | All equipment, ordered by Name, with pagination |
| `src/Core.Domain/EquipmentAggregate/Specifications/EquipmentByLocationSpec.cs` | Filtered by LocationId and optional Status |
| `src/Core.Domain/EquipmentAggregate/Repositories/IEquipmentReadRepository.cs` | Read-only repository interface |
| `src/Core.Domain/EquipmentAggregate/Repositories/IEquipmentRepository.cs` | Read-write repository interface |

**Application layer**

| File | Purpose |
|---|---|
| `src/Application/UseCases/GetEquipment/GetEquipmentRequest.cs` | MediatR request + data record |
| `src/Application/UseCases/GetEquipment/GetEquipmentInteractor.cs` | Query handler |

**Infrastructure layer**

| File | Purpose |
|---|---|
| `src/Infrastructure.Data/Persistence/BookingDbContext.cs` | EF Core DbContext |
| `src/Infrastructure.Data/Persistence/Configurations/EquipmentConfiguration.cs` | Value converters + table config |
| `src/Infrastructure.Data/Persistence/Repositories/EquipmentReadRepository.cs` | Repository implementation |

**Presentation – Contracts**

| File | Purpose |
|---|---|
| `src/Presentation.Contracts/Equipment/GetEquipmentResponse.cs` | Response DTO with `From` factory |

**Presentation – API**

| File | Purpose |
|---|---|
| `src/Presentation.Api/Program.cs` | DI registration + seed data |
| `src/Presentation.Api/Endpoints/Equipment/Get.cs` | FastEndpoints handler |

**Tests**

| File | Purpose |
|---|---|
| `src/Core.Domain.Tests/EquipmentAggregate/EquipmentTests.cs` | 12 unit tests |

## Files modified (0 files)

No existing files were modified — this is a new solution.

## Implementation notes

**1. StatusFilter validation moved inline**

*Plan said:* Use a separate FastEndpoints `Validator<T>` class for StatusFilter validation.  
*Actual:* The endpoint uses `EndpointWithoutRequest<TRes>`, which cannot bind a typed request to a validator. Instead, `StatusFilter` is read from `Query<string>("StatusFilter")` and validated inline with `Enum.TryParse<EquipmentStatus>`. Returns `await SendAsync(new { error = "Invalid StatusFilter value" }, 400, ct)` on failure.  
*Impact:* Functionally equivalent. A follow-up feature using `Endpoint<TReq, TRes>` should use a proper validator.

## API contract

```
GET /get-equipment?LocationId={guid}&StatusFilter={string}&Skip={int}&Take={int}
```

| Parameter | Type | Required | Notes |
|---|---|---|---|
| `LocationId` | `Guid` | No | Filter to this location; unknown → empty list |
| `StatusFilter` | `string` | No | `Available` \| `InUse` \| `Maintenance`; invalid → HTTP 400 |
| `Skip` | `int` | No | Default 0 |
| `Take` | `int` | No | Omit to return all |

**Auth policy:** `AppAccess` (open — `RequireAssertion(_ => true)` for demo)

**Response type:** `IReadOnlyList<GetEquipmentResponse>`

**JSON response example:**
```json
[
  {
    "equipmentId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "name": "Drill Rig Alpha",
    "type": "Drill",
    "status": "Available",
    "locationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "lastServiceDate": "2025-11-15T08:00:00+00:00"
  },
  {
    "equipmentId": "5fa85f64-5717-4562-b3fc-2c963f66bfb7",
    "name": "Loader Beta",
    "type": "Loader",
    "status": "InUse",
    "locationId": "7c9e6679-7425-40de-944b-e07fc1f90ae7",
    "lastServiceDate": null
  }
]
```

## Test results

```
Core.Domain.Tests: 12 passed, 0 failed
Final solution build: Successful (0 warnings, 0 errors)
```

Commands run (from `src\`):
```bash
dotnet build
dotnet test
```
