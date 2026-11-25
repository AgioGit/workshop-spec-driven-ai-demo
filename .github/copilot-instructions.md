# Copilot Instructions — Equipment Booking Demo

Apply the shared, tool-agnostic repository guidance in [`AGENTS.md`](../AGENTS.md).

## Feature workflow

| Step | Skill | What it does |
|---|---|---|
| 1. Scaffold | `/feature-scaffold` | Creates `docs/features/{id}_{name}/` with specification + stub docs |
| 2. Plan | `/lessons-apply` + `/feature-plan` | Reads spec, generates implementable plan.md |
| 3. Implement | Agent auto-mode | Follows plan phase by phase |
| 4. Review | `/code-review` | Reviews branch vs. main |
| 5. Document | `/feature-implementation-doc` + `/feature-lessons-learned` | Produces implementation.md and lessons.md |

Use the repository-local skills for these stages and keep their outputs under `docs/features/{id}_{name}/`.

## Documentation references

| File | When to reference |
|---|---|
| [`AGENTS.md`](../AGENTS.md) | Shared repository structure, conventions, and validation commands |
| [`docs/ARCHITECTURE.md`](../docs/ARCHITECTURE.md) | Architecture, data flow, and aggregate design |
| [`docs/features/001_get_equipment/`](../docs/features/001_get_equipment/) | Completed reference feature |
