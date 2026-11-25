# Task: PBI 003 – add_tool_agnostic_guidance

## Feature: Move general repository guidance from `.github/copilot-instructions.md` into a root `AGENTS.md`

**Type**: refactoring

The repository guidance currently mixes conventions that apply to any coding agent with workflow instructions that are specific to GitHub Copilot skills. The goal is to make the general guidance discoverable and reusable through a root `AGENTS.md`, while keeping `.github/copilot-instructions.md` focused on Copilot-specific workflow and delegating repeated repository details to `AGENTS.md`.

---

## Scope

| Layer | Files / components affected | Notes |
|---|---|---|
| Repository guidance | `AGENTS.md` | Add shared, tool-agnostic repository structure, architecture, conventions, and validation commands |
| Copilot guidance | `.github/copilot-instructions.md` | Retain Copilot-specific skill workflow and references; remove duplicated general guidance |
| Feature documentation | `docs/features/003_add_tool_agnostic_guidance/` | Record the specification and scaffold the plan, implementation, and review documents |

---

## Current state

`.github/copilot-instructions.md` is the only repository-wide instruction file. It contains both:

- General repository architecture, domain, C# conventions, and build commands
- Copilot-specific guidance for the repository's feature skills

### Known issues / pain points

- Other coding agents cannot discover the repository guidance through the conventional root `AGENTS.md`
- Tool-specific and tool-agnostic instructions are mixed in one file
- Duplicating the same content between files would create conflicting sources of truth

---

## Target state

- Root `AGENTS.md` contains repository guidance that any coding agent can apply
- `.github/copilot-instructions.md` contains Copilot-specific feature workflow guidance and points to `AGENTS.md` for shared conventions
- Links use repository-relative paths and continue to resolve
- Guidance remains technically accurate for the current .NET 9 solution
- No application code or unrelated documentation changes

### Changed behaviour (if any)

This is a documentation-only refactor. Build and runtime behaviour do not change.

### Breaking changes (if any)

None.

| Before | After | Notes |
|---|---|---|
| General guidance exists only in `.github/copilot-instructions.md` | General guidance lives in root `AGENTS.md` | Copilot guidance links to it |
| Shared and Copilot-specific guidance are mixed | Shared guidance and Copilot workflow are separated | Avoid duplicated instructions |

---

## Testing requirements

- Review both guidance files for duplication, omissions, broken references, and inaccurate commands
- Validate repository-relative links
- Confirm the change is limited to Markdown documentation
- Run Markdown consistency checks available in the repository, or perform a focused static validation if none exist

---

## References

- Current Copilot guidance: `.github/copilot-instructions.md`
- Feature scaffold reference: `docs/features/002_add_mock_data/`
- Architecture: `docs/ARCHITECTURE.md`
