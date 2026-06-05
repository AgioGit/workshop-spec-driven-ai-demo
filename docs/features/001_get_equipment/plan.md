# Plan: PBI 100 – get_equipment

## 1. Overview

Implements `GET /get-equipment` — a paginated, filterable list of all equipment in the system. The endpoint lives in `Presentation.Api` and uses MediatR to dispatch to `GetEquipmentInteractor` in the `Application` layer. The interactor queries `IEquipmentReadRepository` via an Ardalis specification and returns `IReadOnlyList<GetEquipmentData>`. The endpoint maps each data record to `GetEquipmentResponse` (from `Presentation.Contracts`) and returns HTTP 200.

**API surface:**
```
GET /get-equipment?LocationId={guid}&StatusFilter={string}&Skip={int}&Take={int}
```

**Response contract (per item):**

| Property | Type | Notes |
|---|---|---|
| `equipmentId` | `Guid` | Exposed as plain Guid (`.Value`) |
| `name` | `string` | Never null |
| `type` | `string` | Never null |
| `status` | `string` | One of `Available`, `InUse`, `Maintenance` |
| `locationId` | `Guid` | Exposed as plain Guid (`.Value`) |
| `lastServiceDate` | `DateTimeOffset?` | Null if never serviced |

## 2. Legacy behaviour to preserve

This is a new endpoint with no legacy system to migrate from. The specification defines the following branching behaviour that must be preserved exactly:

- **No `LocationId` supplied** → return all equipment across all locations, ordered by `Name` ascending then `EquipmentId` as tiebreaker. This is broader than a naïve reading might suggest — it is not an error to omit the location.
- **`LocationId` supplied but unknown** → return an **empty list with HTTP 200** (not 404). The caller is responsible for knowing which locations exist; the API does not validate location existence.
- **`StatusFilter` supplied** → narrow results to that status only. Validation happens at the endpoint boundary (HTTP 400 before the interactor is reached); the interactor always receives a valid or null value.
- **`StatusFilter` unrecognised** → HTTP 400 with a descriptive error message. Case-insensitive matching must be applied before rejection.
- **`Skip` / `Take`** → pagination is applied to the post-filter result set. `Skip=0` with no `Take` returns all matching records; this is the default behaviour and must not be treated as an error.

## 3. Recommended design

- **Two Ardalis specifications** handle the location-branching logic in the domain layer: `AllEquipmentSpec` (no location filter) and `EquipmentByLocationSpec` (with optional `StatusFilter`). Keeping them separate avoids a nullable-parameter smell inside a single spec and makes each testable in isolation.
- The **interactor selects the spec** based on whether `LocationId` is provided. No branching logic leaks into the endpoint or the infrastructure layer.
- `IEquipmentReadRepository` is defined in `Core.Domain` and implemented by `EquipmentReadRepository` in `Infrastructure.Data` (EF Core in-memory). No new repository interface is needed beyond what already exists.
- **DTO placement**: `GetEquipmentResponse` has no Application layer dependency — it maps from the `GetEquipmentData` record — so it belongs in `Presentation.Contracts/Equipment/`. The `GetEquipmentRequest` and `GetEquipmentData` types stay in `Application/UseCases/GetEquipment/`.
- **Pagination** is applied inside each specification via `Query.Skip(skip)` and `Query.Take(take.Value)`, keeping the interactor free of pagination concerns.

## 4. Files to create

**Domain layer — `src\Core.Domain\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 1 | `src\Core.Domain\EquipmentAggregate\Equipment.cs` | Aggregate root with `Create(name, type, locationId)` factory; sets Status to Available |
| 2 | `src\Core.Domain\EquipmentAggregate\Specifications\AllEquipmentSpec.cs` | Returns all equipment ordered by Name then Id, with Skip/Take pagination |
| 3 | `src\Core.Domain\EquipmentAggregate\Specifications\EquipmentByLocationSpec.cs` | Filters by LocationId and optional StatusFilter, ordered by Name then Id, with pagination |
| 4 | `src\Core.Domain\EquipmentAggregate\Repositories\IEquipmentReadRepository.cs` | Read-only repository marker interface extending `IReadRepositoryBase<Equipment>` |
| 5 | `src\Core.Domain\EquipmentAggregate\Repositories\IEquipmentRepository.cs` | Read-write repository marker interface extending `IRepositoryBase<Equipment>` |

**Application layer — `src\Application\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 6 | `src\Application\UseCases\GetEquipment\GetEquipmentRequest.cs` | `sealed record GetEquipmentRequest` implementing `IRequest<IReadOnlyList<GetEquipmentData>>`; plus the `GetEquipmentData` data record |
| 7 | `src\Application\UseCases\GetEquipment\GetEquipmentInteractor.cs` | `IRequestHandler` that selects the appropriate spec and maps `Equipment` → `GetEquipmentData` |

**Infrastructure layer — `src\Infrastructure.Data\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 8 | `src\Infrastructure.Data\Persistence\BookingDbContext.cs` | EF Core `DbContext` with `DbSet<Equipment>`; applies configurations from assembly |
| 9 | `src\Infrastructure.Data\Persistence\Configurations\EquipmentConfiguration.cs` | `IEntityTypeConfiguration<Equipment>` — value converters for `EquipmentId` and `LocationId`, column constraints |
| 10 | `src\Infrastructure.Data\Persistence\Repositories\EquipmentReadRepository.cs` | Concrete `RepositoryBase<Equipment>` implementing `IEquipmentReadRepository` |

**Presentation – Contracts — `src\Presentation.Contracts\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 11 | `src\Presentation.Contracts\Equipment\GetEquipmentResponse.cs` | Response DTO with `static From(GetEquipmentData)` factory; exposes IDs as plain `Guid` |

**Presentation – API — `src\Presentation.Api\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 12 | `src\Presentation.Api\Endpoints\Equipment\Get.cs` | `EndpointWithoutRequest<IReadOnlyList<GetEquipmentResponse>>` — reads query params, validates StatusFilter inline, dispatches via MediatR |
| 13 | `src\Presentation.Api\Program.cs` | DI registrations for DbContext, repository, MediatR, FastEndpoints, and auth policy; seeds demo data |

**Test files — `src\Core.Domain.Tests\`**

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 14 | `src\Core.Domain.Tests\EquipmentAggregate\EquipmentTests.cs` | Unit tests for `Equipment.Create`, both specs, and the interactor (12 tests) |

## 5. Files to modify

| # | Exact file path | What changes and why |
|---|---|---|
| 1 | `src\workshop-api-demo.slnx` | Solution file — all six projects added via `dotnet sln add` |
| 2 | `src\Infrastructure.Data\Infrastructure.Data.csproj` | Add `Microsoft.EntityFrameworkCore.InMemory` and `Ardalis.Specification.EntityFrameworkCore` package references |
| 3 | `src\Core.Domain.Tests\Core.Domain.Tests.csproj` | Add project references to `Infrastructure.Data` and `Application`; add test NuGet packages |

## 6. Detailed implementation order (TDD)

### Phase 1: Domain specs

Write these test cases in `EquipmentTests.cs` **before** implementing the production types:

- `Create_WithValidArguments_ReturnsEquipmentWithAvailableStatus`
- `Create_WithNullName_ThrowsArgumentException`
- `Create_WithEmptyName_ThrowsArgumentException`
- `AllEquipmentSpec_ReturnsAllEquipment_OrderedByName`
- `AllEquipmentSpec_WithSkip_AppliesPaginationOffset`
- `AllEquipmentSpec_WithTake_LimitsResults`
- `EquipmentByLocationSpec_FiltersToCorrectLocation`
- `EquipmentByLocationSpec_WithStatusFilter_FiltersToCorrectStatus`
- `EquipmentByLocationSpec_WithUnknownLocation_ReturnsEmpty`

Then implement:
1. `src\Core.Domain\EquipmentAggregate\Equipment.cs` — aggregate root with `[StronglyTypedId]` IDs and `Create` factory
2. `src\Core.Domain\EquipmentAggregate\Specifications\AllEquipmentSpec.cs`
3. `src\Core.Domain\EquipmentAggregate\Specifications\EquipmentByLocationSpec.cs`
4. `src\Core.Domain\EquipmentAggregate\Repositories\IEquipmentReadRepository.cs`
5. `src\Core.Domain\EquipmentAggregate\Repositories\IEquipmentRepository.cs`

### Phase 2: Application use case

Write test cases first:

- `Handle_WithNoLocationId_ReturnsAllEquipment`
- `Handle_WithLocationId_ReturnsFilteredEquipment`
- `Handle_WithUnknownLocationId_ReturnsEmptyList`

Then implement:
1. `src\Application\UseCases\GetEquipment\GetEquipmentRequest.cs` — sealed records for request + data model
2. `src\Application\UseCases\GetEquipment\GetEquipmentInteractor.cs` — handler with `Guard.Against.Null` on constructor injection

### Phase 3: Contracts and DTO mapping

Write test cases first:

- `From_MapsAllFieldsCorrectly` — verifies all six properties including `.Value` unwrapping
- `From_WithNullLastServiceDate_MapsToNull`

Then implement:
1. `src\Presentation.Contracts\Equipment\GetEquipmentResponse.cs` — record with `static From(GetEquipmentData)` factory

### Phase 4: Endpoint and validation

No dedicated tag constants are needed (route is a hardcoded string in `Configure()`). Implement:

1. `src\Presentation.Api\Endpoints\Equipment\Get.cs` — `EndpointWithoutRequest`, reads `LocationId`, `StatusFilter`, `Skip`, `Take` from query string
2. Inline `StatusFilter` validation using `Enum.TryParse<EquipmentStatus>(raw, ignoreCase: true, out var status)` — return HTTP 400 with descriptive message on failure

### Phase 5: DI registration

Add to `src\Presentation.Api\Program.cs`:

```csharp
builder.Services.AddDbContext<BookingDbContext>(opt =>
    opt.UseInMemoryDatabase("WorkshopDemo"));
builder.Services.AddScoped<IEquipmentReadRepository, EquipmentReadRepository>();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(GetEquipmentInteractor).Assembly));
builder.Services.AddAuthorization(opt =>
    opt.AddPolicy("AppAccess", p => p.RequireAssertion(_ => true)));
builder.Services.AddFastEndpoints();
```

Then seed three `Equipment` records in the in-memory DB immediately after `app.Build()` for demo purposes.

### Phase 6: Verification

Run from `src\`:

```bash
dotnet build
dotnet test
```

Expected: **12 passed, 0 failed**. Build must produce 0 errors and 0 warnings.

## 7. Notes on contract compatibility

- `LocationId` and `StatusFilter` are optional query parameters — the endpoint must not require them or default them to empty string.
- `Skip` defaults to `0` when omitted; `Take` is optional with no server-side upper bound.
- Response property names are camelCase (FastEndpoints default JSON serialiser): `equipmentId`, `name`, `type`, `status`, `locationId`, `lastServiceDate`.
- `equipmentId` and `locationId` are exposed as plain `Guid` (via `.Value`) — not as strongly-typed struct wrappers. This is the external contract and must not change.
- `status` is `enum.ToString()` — one of `Available`, `InUse`, `Maintenance`. Do not serialise as integer.
- `lastServiceDate` is `DateTimeOffset?` — serialises as ISO 8601 or JSON `null`. Never omit the property.

## 8. Risks and review points

- **StronglyTypedId + EF Core value converters** — `EquipmentId` and `LocationId` are structs; without explicit `HasConversion(id => id.Value, v => new EquipmentId(v))` in `EquipmentConfiguration`, EF Core cannot persist or query these columns. This is the most common failure point.
- **LINQ-to-EF with strongly-typed IDs** — never use `.Value` inside `Where()` or `OrderBy()`. Use the struct directly (`e.LocationId == locationId`); EF resolves via the registered value converter. Using `.Value` causes a client-side evaluation or runtime exception.
- **Repository lifetime** — register `IEquipmentReadRepository` as `Scoped`, not `Transient`. A `Transient` repository wrapping a `Scoped` `DbContext` causes a captive dependency exception at startup.
- **FastEndpoints auth policy** — `Policies("AppAccess")` in `Configure()` requires the policy to be registered in DI before the app is built. Missing registration causes all requests to return HTTP 403 with no visible error.
- **`EndpointWithoutRequest` cannot use a typed `Validator<T>`** — `StatusFilter` validation must be done inline in `HandleAsync` rather than via a separate validator class (which requires a bound request type).
