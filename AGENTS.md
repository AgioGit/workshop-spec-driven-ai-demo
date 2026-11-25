# Repository Guidance — Equipment Booking Demo

This is a demo .NET 9 solution organised with Clean Architecture and vertical slices.

## Repository overview

The solution lives under `src\` and uses these layers:

```text
src\
├── Core.Domain\            # Zero external dependencies; domain model and repository interfaces
├── Application\            # MediatR use cases (interactors)
├── Infrastructure.Data\    # EF Core in-memory persistence and repositories
├── Presentation.Api\       # FastEndpoints API
├── Presentation.Contracts\ # Request/response DTOs without an Application dependency
└── Core.Domain.Tests\      # Unit tests
```

**Technology stack:** .NET 9, C# 13, FastEndpoints, MediatR, EF Core (in-memory), Ardalis.Specification, Ardalis.GuardClauses, and StronglyTypedId.

## Solution directory

The solution file is in `src\`. Run all `dotnet` commands from that directory.

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~EquipmentTests"
```

## Domain model

The demo domain is an equipment booking system:

- **Equipment** — physical machines (`Name`, `Type`, `Status`, `LocationId`, `LastServiceDate`)
- **Location** — sites or zones where equipment is deployed
- **Operator** — people who operate equipment (`FullName`, `CertificationLevel`, `LocationId`, `IsActive`)
- **Booking** — a reservation of equipment for a time slot
- **MaintenanceEvent** — scheduled or completed maintenance on equipment

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for the full data flow, aggregate overview, and representative patterns.

## Key conventions

### Aggregate structure

Each aggregate lives in `Core.Domain/{Aggregate}Aggregate/`:

```text
{Aggregate}Aggregate/
├── {Entity}.cs
├── Specifications/
└── Repositories/
    ├── I{Entity}ReadRepository.cs
    └── I{Entity}Repository.cs
```

### Strongly typed IDs

Declare domain IDs with StronglyTypedId:

```csharp
[StronglyTypedId]
public partial struct EquipmentId { }
```

Never use `.Value` inside LINQ expressions translated by EF Core. Use the strongly typed ID directly in `Where`, `OrderBy`, and `Select`; EF Core resolves it through the registered value converter.

```csharp
Query.Where(e => e.LocationId == locationId);
```

Using `.Value` is appropriate when mapping a persisted ID to a plain external DTO value, but not inside a LINQ-to-EF query.

### Constructor guards

Every service and handler constructor guards all injected dependencies:

```csharp
_repository = Guard.Against.Null(repository);
```

### MediatR naming

Organise MediatR use cases as:

```text
UseCases/{Action}{Entity}/
├── {Action}{Entity}Request.cs
└── {Action}{Entity}Interactor.cs
```

The request is a sealed record implementing `IRequest<TResponse>`. Query interactors implement `IQueryingInteractor<TRequest, TResponse>`.

### FastEndpoints

Endpoints live in `Presentation.Api/Endpoints/{Entity}/{Action}.cs`.

- Use a short endpoint class name such as `List` or `Get`.
- Configure a hardcoded kebab-case route, for example `Get("/get-equipment")`.
- Apply `Policies("AppAccess")` to every endpoint.
- Include `Skip` (`int`, default `0`) and `Take` (`int?`) pagination parameters on GET endpoints.
- Put response DTOs in `Presentation.Contracts` when they have no dependency on the Application layer.

## Architecture documentation

| File | When to reference |
|---|---|
| [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) | General orientation, architecture, and conventions |
| [`docs/features/README.md`](docs/features/README.md) | Feature documentation index |
| [`docs/features/001_get_equipment/`](docs/features/001_get_equipment/) | Completed reference feature |
