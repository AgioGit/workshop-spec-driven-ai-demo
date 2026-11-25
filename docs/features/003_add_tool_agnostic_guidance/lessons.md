# Lessons Learned: 003 – add_tool_agnostic_guidance

Each lesson follows this structure for searchability and AI retrieval:
- **Header** — lesson ID + descriptive title
- **Metadata** — date discovered, severity, short summary, tags
- **Problem** — what went wrong and why
- **Fix** — concrete code or approach that resolved it
- **Rule** — generalised guidance for future features

Severity levels: 🔴 Critical · 🟠 Major · 🟡 Medium · 🔵 Minor

---

## L001 · Treat guidance moves as migrations of architectural invariants

| | |
|---|---|
| **Date** | 2026-11-25 |
| **Severity** | 🟡 Medium |
| **Summary** | Moving repository guidance to a new source of truth dropped the explicit `Core.Domain` no-external-dependencies invariant during review. |
| **Tags** | `backward-compat` `spec-deviation` `naming` |

### Problem

The relocation preserved most headings, commands, and examples, but a constraint embedded in one layer description was lost when the repository tree was rewritten. A structural comparison alone did not reveal that the shorter replacement text no longer prohibited external dependencies in `Core.Domain`.

### Fix

Compare the source and destination guidance as an invariant checklist, then retain explicit constraints in the migrated text:

```text
src\
├── Core.Domain\            # Zero external dependencies; aggregates, value objects, repositories
...
```

The review correction restored the invariant before merge.

### Rule

Before replacing or relocating repository guidance, extract its explicit and embedded invariants into a checklist and verify every item in the new source of truth.

---

## L002 · Resolve links from the containing document, not the repository root

| | |
|---|---|
| **Date** | 2026-11-25 |
| **Severity** | 🔵 Minor |
| **Summary** | Moving shared documentation from `.github` to the root changed the base used to resolve every relative Markdown reference. |
| **Tags** | `backward-compat` `spec-deviation` |

### Problem

References that work from the repository root cannot be reused unchanged inside `.github/copilot-instructions.md`. The two guidance files now have different relative paths to the same architecture and feature documents, so a superficial copy would create valid-looking but broken links.

### Fix

Resolve each target relative to the file that contains it and validate the result:

```text
# Root AGENTS.md
[Architecture](docs/ARCHITECTURE.md)

# .github/copilot-instructions.md
[Architecture](../docs/ARCHITECTURE.md)
```

The implemented documentation uses `docs/...` links in `AGENTS.md` and `../docs/...` links in the Copilot file.

### Rule

When guidance moves between directory levels, rewrite and verify every relative Markdown link from the final location of its containing file.

---

## L003 · Do not inherit a base branch from a scaffold without verifying it

| | |
|---|---|
| **Date** | 2026-11-25 |
| **Severity** | 🔵 Minor |
| **Summary** | The generated review scaffold referenced `develop`, while this repository's actual base branch is `main`, making its comparison commands invalid. |
| **Tags** | `spec-deviation` `naming` |

### Problem

The feature scaffold generated a generic `develop` comparison before checking the repository's branches. The Copilot workflow already names `main`, so the scaffold contradicted both the repository state and the active instructions.

### Fix

Detect the available base branch and generate the scaffold against the resolved value:

```powershell
$base = if (git rev-parse --verify develop 2>$null) { 'develop' } else { 'main' }
git --no-pager diff "$base...HEAD"
```

The PBI 003 review scaffold was updated to use `main` before merge.

### Rule

Before relying on branch names from templates or skills, resolve them against the repository and fail or update the documentation when the expected branch does not exist.

---

## L004 · Preserve the execution context of strongly typed ID rules

| | |
|---|---|
| **Date** | 2026-11-25 |
| **Severity** | 🟡 Medium |
| **Summary** | The `.Value` restriction needed to distinguish EF-translated queries from valid DTO mapping so the migrated guidance would not overconstrain callers. |
| **Tags** | `ef-core` `strongly-typed-id` `backward-compat` |

### Problem

The original shorthand warned against using `.Value` inside LINQ-to-EF but did not state where `.Value` remains appropriate. During extraction, a broader “never use `.Value`” rule could have prohibited legitimate conversion of persisted IDs into plain external DTO values.

### Fix

State both the restriction and the permitted context:

```text
Never use strongly typed ID .Value access inside LINQ translated by EF Core.
Use the strongly typed ID directly in Where, OrderBy, and Select.
Use .Value when mapping the ID to a plain external DTO value.
```

The implementation retains this distinction in `AGENTS.md`.

### Rule

When preserving framework-specific guidance, state the exact execution boundary that makes the restriction true and explicitly distinguish valid usage outside that boundary.

---
