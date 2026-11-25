# Implementation: PBI 003 – add_tool_agnostic_guidance

## Summary

Refactored the repository guidance so shared, tool-agnostic conventions live in a root `AGENTS.md`, while `.github/copilot-instructions.md` retains the GitHub Copilot feature workflow and links to the shared guidance. The PBI 003 plan was followed for the intended documentation split, including the repository layout, domain model, implementation conventions, architecture references, and build commands. No application code or runtime contract changed.

## Files created (6 new files)

**Repository guidance**

| File | Purpose |
|---|---|
| `AGENTS.md` | Tool-agnostic source of truth for repository structure, technology stack, domain model, implementation conventions, architecture references, and validation commands |

**Feature documentation**

| File | Purpose |
|---|---|
| `docs/features/003_add_tool_agnostic_guidance/specification.md` | Defines the documentation refactor, scope, target state, and validation requirements |
| `docs/features/003_add_tool_agnostic_guidance/plan.md` | Records the approved implementation sequence, compatibility constraints, and focused acceptance checks |
| `docs/features/003_add_tool_agnostic_guidance/implementation.md` | Records the implemented changes, test results, deviations, and follow-up items |
| `docs/features/003_add_tool_agnostic_guidance/code-review.md` | Provides the feature review scaffold |
| `docs/features/003_add_tool_agnostic_guidance/lessons.md` | Records reusable lessons from the documentation migration and review corrections |

## Files modified (1 file)

| File | Change |
|---|---|
| `.github/copilot-instructions.md` | Replaced duplicated shared repository guidance with a link to `AGENTS.md`; retained the Copilot-specific skill workflow and corrected relative documentation links |

## Implementation notes

**1. Shared guidance and Copilot workflow were separated**

*Plan said:* Move all repository-wide implementation and validation guidance to root `AGENTS.md`; keep only Copilot-specific workflow and references in `.github/copilot-instructions.md`.  
*Actual:* `AGENTS.md` now owns the solution layout, .NET stack, domain model, aggregate layout, strongly typed ID and EF LINQ rules, constructor guards, MediatR naming, FastEndpoints conventions, architecture links, and `dotnet` commands. The Copilot file links to that document and retains the five-stage feature workflow.  
*Impact:* Other coding agents can discover shared repository conventions without parsing Copilot-specific skill instructions.

**2. `.Value` guidance was clarified**

*Plan said:* Preserve the EF LINQ restriction while noting that `.Value` remains appropriate for external DTO mapping.  
*Actual:* `AGENTS.md` explicitly distinguishes LINQ expressions translated by EF Core from mapping persisted strongly typed IDs to plain external DTO values.  
*Impact:* The original restriction is retained without incorrectly implying that `.Value` is forbidden in every context.

**3. Review findings were corrected**

*Plan said:* Preserve all existing repository facts and keep the feature review documentation consistent with the repository base branch.  
*Actual:* The completed review identified and corrected two omissions: `AGENTS.md` now explicitly states that `Core.Domain` has zero external dependencies, and `code-review.md` now uses `main` instead of the nonexistent `develop` branch.  
*Why this mattered:* These were content and consistency corrections identified during verification rather than intentional design deviations.  
*Impact:* The refactor retains the original architectural invariant and provides valid Git comparison guidance.

## API contract

No API surface changed. Routes, request parameters, authentication policy, response types, and JSON shapes remain unchanged.

## Test results

```text
Core.Domain.Tests: 33 passed, 0 failed, 0 skipped
Final solution build: Successful (0 warnings, 0 errors)
```

Commands run (from `src\`):

```bash
dotnet test --no-restore
dotnet build --no-restore
```

Focused documentation checks also confirmed that the changed Markdown links resolve and `git diff --check` reports no whitespace errors.

## Known follow-up items

None.
