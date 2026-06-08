# Equipment Booking Demo — GitHub Copilot Skills Workshop

This repo is a **workshop for learning how to use GitHub Copilot Skills** in a real software development workflow. The .NET API is the vehicle — the main deliverables are the skills, the feature documentation workflow, and the worked examples in `docs/features/`.

---

## What this repo teaches

The workshop demonstrates a structured, skill-assisted feature development workflow:

```
/feature-scaffold  →  /lessons-apply + /feature-plan  →  implement  →  /code-review  →  /feature-implementation-doc + /feature-lessons-learned
```

Each step has a dedicated Copilot Skill that reads and writes structured Markdown documents under `docs/features/{id}_{name}/`. The result is a complete, auditable record of every feature — from spec to lessons learned.

---

## The workflow

| Step | Skill | What it produces |
|---|---|---|
| 1. Scaffold | `/feature-scaffold` | `specification.md` + stub docs in `docs/features/{id}_{name}/` |
| 2. Check past lessons | `/lessons-apply` | Surfaces relevant `lessons.md` entries before planning |
| 3. Plan | `/feature-plan` | `plan.md` — phased, TDD-ordered implementation plan |
| 4. Implement | Agent auto-mode | Code changes guided by `plan.md` |
| 5. Review | `/code-review` | `code-review.md` — signal-only review of branch changes |
| 6. Document | `/feature-implementation-doc` | `implementation.md` — what was actually built and any deviations |
| 7. Learn | `/feature-lessons-learned` | `lessons.md` — concrete, reusable lessons from what went wrong |

Skills live in `.github/skills/`. Copilot reads them automatically when invoked.

---

## Worked examples

The `docs/features/` folder contains fully completed walkthroughs:

| Feature | What it demonstrates |
|---|---|
| [`001_get_equipment`](docs/features/001_get_equipment/) | Reference feature — all four docs complete; target quality level |
| [`002_add_mock_data`](docs/features/002_add_mock_data/) | Full end-to-end workflow including scaffold process walkthrough, plan, implementation deviations, and seven lessons learned |

Study `002_add_mock_data/` to see how each document is written, how deviations from the plan are recorded, and how lessons are extracted for reuse.

---

## The demo codebase

The .NET API is an **Equipment Booking System** used as a realistic domain to practice the workflow on. It is intentionally simple.

**Stack:** .NET 9 · FastEndpoints · MediatR · EF Core (in-memory) · Ardalis.Specification · StronglyTypedId

```
src/
├── Core.Domain/            # Aggregates + repository interfaces
├── Application/            # MediatR use cases
├── Infrastructure.Data/    # EF Core + seeding
├── Presentation.Api/       # FastEndpoints API
├── Presentation.Contracts/ # Request/response DTOs
└── Core.Domain.Tests/      # Unit + integration tests
```

All `dotnet` commands run from `src/`:

```bash
dotnet build
dotnet test
dotnet run --project Presentation.Api   # Swagger at /swagger
```

See [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md) for conventions, aggregate design, and coding standards.
