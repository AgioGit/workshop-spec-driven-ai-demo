# Copilot Instructions — Equipment Booking Demo

> **How to use this file:**  
> Place this at `.github/copilot-instructions.md` in the `workshop-api-demo` repo.  
> It is adapted from the production LKAB.Mine.Production.Charging instructions but simplified for the demo domain.  
> The skills (feature-scaffold, feature-plan, etc.) read this file for context about conventions.

---

## Repository overview

This is a demo .NET 9 solution following **Clean Architecture** with **Vertical Slice** organisation.

The solution lives under `src/` and uses these layers:

```
src/
├── Core.Domain/          # Aggregates, Value Objects, Repository interfaces — no external dependencies
├── Application/          # MediatR use cases (interactors)
├── Infrastructure.Data/  # EF Core + in-memory / SQLite repositories
├── Presentation.Api/     # FastEndpoints API
├── Presentation.Contracts/ # Request/response DTOs (no Application dependency)
└── Core.Domain.Tests/    # Unit tests
```

**Technology stack:** .NET 9 · C# 13 · FastEndpoints · MediatR · EF Core (in-memory) · Ardalis.Specification · Ardalis.GuardClauses · StronglyTypedId

---

## Solution directory

The `.sln` file is in `src/`. Run all `dotnet` commands from `src/`.

```bash
dotnet build
dotnet test
```

---

## Domain model

The demo domain is an **Equipment Booking System**:

- **Equipment** — physical machines (Name, Type, Status, LocationId, LastServiceDate)
- **Location** — sites or zones where equipment is deployed
- **Operator** — people who operate equipment (FullName, CertificationLevel, LocationId, IsActive)
- **Booking** — a reservation of equipment for a time slot
- **MaintenanceEvent** — scheduled or completed maintenance on equipment

---

## Key conventions

### Aggregate structure

Each aggregate lives in `Core.Domain/{Aggregate}Aggregate/`:

```
{Aggregate}Aggregate/
├── {Entity}.cs                          # Aggregate root
├── Specifications/                      # Ardalis specs
├── Repositories/I{Entity}ReadRepository.cs
└── Repositories/I{Entity}Repository.cs
```

### Strongly-typed IDs

```csharp
[StronglyTypedId] public partial struct EquipmentId { }
```

**EF Core LINQ rule — never use `.Value` inside LINQ-to-EF:**  
Use `e.Id` directly in `Where`, `OrderBy`, `Select`. EF resolves it via the value converter.

### Guard clauses in constructors

Every service/handler constructor uses `Guard.Against.Null(...)` for all injected dependencies.

### MediatR naming

```
UseCases/{Action}{Entity}/
├── {Action}{Entity}Request.cs     # sealed record implementing IRequest<TResponse>
└── {Action}{Entity}Interactor.cs  # implements IQueryingInteractor<TRequest, TResponse>
```

Query interactors implement `IQueryingInteractor<TRequest, TResponse>`.

### FastEndpoints structure

Endpoints live in `Presentation.Api/Endpoints/{Entity}/{Action}.cs`.

- Endpoint class name: short verb (`List`, `Get`)
- Route: hardcoded kebab-case string in `Configure()` → `Get("/get-equipment")`
- All endpoints: `Policies("AppAccess")`
- GET endpoints always include `Skip` (`int`, default 0) and `Take` (`int?`) pagination parameters
- Response DTOs live in `Presentation.Contracts` if they have no Application layer dependency

### Feature workflow

| Step | Skill | What it does |
|---|---|---|
| 1. Scaffold | `/feature-scaffold` | Creates `docs/features/{id}_{name}/` with specification + stub docs |
| 2. Plan | `/lessons-apply` + `/feature-plan` | Reads spec, generates implementable plan.md |
| 3. Implement | Agent auto-mode | Follows plan phase by phase |
| 4. Review | `/code-review` | Reviews branch vs. main |
| 5. Document | `/feature-implementation-doc` + `/feature-lessons-learned` | Produces implementation.md and lessons.md |

### Architecture documentation

| File | When to reference |
|---|---|
| `docs/ARCHITECTURE.md` | General orientation, tech stack |
| `docs/features/001_get_equipment/` | Reference feature — all four docs complete |

---

## Build and test commands

All commands from `src/`:

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~EquipmentTests"
```
