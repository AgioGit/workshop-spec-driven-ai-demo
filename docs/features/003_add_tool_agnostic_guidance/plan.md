# Plan: PBI 003 – add_tool_agnostic_guidance

## 1. Overview

Refactor repository instruction documentation so any coding agent can discover shared guidance in root `AGENTS.md`, while `.github/copilot-instructions.md` remains focused on GitHub Copilot skills and workflow. This is a documentation-only change with no API, data, or runtime contract.

**Documentation surface:** Root `AGENTS.md` is the source of truth for tool-agnostic repository facts; `.github/copilot-instructions.md` is the source of truth for Copilot-specific workflow.

## 2. Legacy behaviour to preserve

- Preserve all repository facts currently in `.github/copilot-instructions.md`: solution layout, .NET 9/C# 13 stack, domain model, aggregate structure, strongly typed ID guidance, EF LINQ `.Value` restriction, constructor guards, MediatR naming, FastEndpoints conventions, documentation references, and build/test commands.
- Preserve the complete Copilot feature workflow: scaffold, lessons/plan, implementation, review, and documentation.
- Keep `src\` as the required working directory for all `dotnet` commands.
- Do not alter code, public API behaviour, test expectations, or historical feature documents.
- Avoid substantive duplication: a shared rule may be referenced from `.github/copilot-instructions.md`, but should exist only in `AGENTS.md`.

## 3. Recommended design

Create a concise root `AGENTS.md` with tool-neutral headings for:

1. Repository purpose and solution structure
2. Technology stack and domain model
3. Architecture references
4. Aggregate, StronglyTypedId, EF LINQ, guard-clause, MediatR, DTO, FastEndpoints, and pagination conventions
5. Build and test commands

Reduce `.github/copilot-instructions.md` to:

1. A direct relative link to `AGENTS.md`
2. The Copilot skill workflow table
3. Copilot-oriented references to architecture and completed feature documentation

Use paths relative to each Markdown file. Do not reference Copilot slash commands or repository-local skills from `AGENTS.md`.

## 4. Files to create

| # | Exact solution-relative file path | Purpose |
|---|---|---|
| 1 | `AGENTS.md` | Own all repository-wide, tool-agnostic implementation and validation guidance |

## 5. Files to modify

| # | Exact file path | What changes and why |
|---|---|---|
| 1 | `.github/copilot-instructions.md` | Remove guidance moved to `AGENTS.md`, add a relative link to it, and retain only Copilot-specific workflow/documentation references |
| 2 | `docs/features/003_add_tool_agnostic_guidance/plan.md` | Replace the scaffold stub with this approved implementation plan |

No application, test, skill, feature-index, or historical-feature files are required by this specification.

## 6. Detailed implementation order

### Phase 1: Establish acceptance checks

Create a simple section-by-section checklist from the original `.github/copilot-instructions.md`:

- Repository overview and structure
- Solution location and commands
- Domain entities
- Every key convention
- Feature workflow
- Architecture references

This checklist is validation-only; no automated test source is needed for a documentation-only refactor.

### Phase 2: Create shared guidance

1. Add `AGENTS.md` at the repository root.
2. Preserve every shared rule from the original file, with wording made explicitly tool-neutral.
3. Keep the examples needed to make the rules actionable, especially strongly typed IDs, EF LINQ, aggregate layout, use-case layout, and endpoint configuration.
4. Link to `docs/ARCHITECTURE.md`, `docs/features/README.md`, and `docs/features/001_get_equipment/`.
5. Preserve the exact `dotnet` command set and state that commands run from `src\`.

### Phase 3: Narrow Copilot guidance

1. Replace the shared sections in `.github/copilot-instructions.md` with a relative link to `../AGENTS.md`.
2. Retain the Copilot-specific feature workflow and repository-local skill responsibilities.
3. Retain only references needed by Copilot users, resolving them relative to `.github/`.
4. Review the result to ensure Copilot-specific slash-command material is absent from `AGENTS.md`.

### Phase 4: Replace the scaffold plan

Update `docs/features/003_add_tool_agnostic_guidance/plan.md` with the approved plan while leaving `implementation.md` and `code-review.md` as workflow stubs until later stages.

### Phase 5: Verify documentation consistency

Run focused checks:

```bash
git diff --check
```

Validate changed Markdown links resolve relative to their containing files. Search the final diff for:

- Shared rules duplicated in both instruction files
- Copilot-specific workflow terms in `AGENTS.md`
- Missing original headings or commands
- Changes outside `AGENTS.md`, `.github/copilot-instructions.md`, and the feature `plan.md`

No .NET build or test run is required unless a later implementation unexpectedly changes application files. The repository-standard commands remain recorded in `AGENTS.md`:

```bash
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~EquipmentTests"
```

## 7. Notes on contract compatibility

There is no API or serialization contract. Documentation compatibility means:

- Existing command names and `src\` working directory remain unchanged.
- Existing technical guidance remains available with equivalent or clearer wording.
- Existing Copilot skill workflow remains discoverable.
- Relative Markdown links resolve from their new locations.
- `AGENTS.md` is the single source of shared facts; `.github/copilot-instructions.md` does not copy them.

## 8. Risks and review points

- **Information loss:** Compare the resulting guidance with the original Copilot file section by section.
- **Duplication drift:** Ensure the Copilot file links to shared guidance rather than restating it.
- **Incorrect link bases:** Validate root-relative content separately from links originating in `.github/`.
- **Tool leakage:** Confirm `AGENTS.md` contains no `/feature-*`, Copilot, or skill-invocation instructions.
- **Scope creep:** Reject changes to application code, tests, skills, historical feature records, or the feature index unless the specification is revised.
- **Terminology accuracy:** Keep `.Value` guidance precise: allowed for external mapping, forbidden inside EF-translated LINQ.
