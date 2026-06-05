---
name: feature-scaffold
description: Scaffold a new feature folder with all workflow docs and a pre-filled specification.md. Use this when starting a new feature (PBI) to create the docs/features/{id}_{name}/ folder with specifications/instructions, plan, implementation, and code-review documents.
---

## Parameters

- **id**: <!-- required — PBI / work item ID, e.g. 134500 -->
- **name**: <!-- required — snake_case feature name, e.g. pcm_api_getequipment -->
- **type**: <!-- required — endpoint | sync | event | domain-model | front-end | back-end | refactoring | {custom: describe in one word} -->
- **description**: <!-- required — one sentence describing what this feature does -->
- **similarFeature**: <!-- optional — folder name of a similar feature to use as structural reference, e.g. 131396_pcm_api_get_ringarea -->
- **wikiPageUrl**: <!-- optional — Azure DevOps wiki URL for the event/API contract, e.g. https://dev.azure.com/{org}/{project}/_wiki/wikis/{wiki}?pagePath={path} -->

---

## Task

Scaffold a new feature under `docs/features/{id}_{name}/` and generate all four workflow documents.

> **Working directory note:** The solution is in `src/`. The `docs/` folder is at the repository root. Do not prefix `docs/` paths with `src/`.

---

## Step 1 — Find a similar feature for structural reference

If `{similarFeature}` is provided, read:
- `docs/features/{similarFeature}/specification.md`

Otherwise, find the best match from `docs/features/` based on `{type}`:

| type | default reference folder |
|---|---|
| `endpoint` | find the most structurally similar API endpoint feature in `docs/features/` |
| `sync` | find the most structurally similar sync / data-import feature in `docs/features/` |
| `event` | find the most structurally similar event publisher or consumer feature in `docs/features/` |
| `domain-model` | find the most structurally similar domain aggregate feature in `docs/features/` |
| `front-end` | find the most structurally similar Blazor feature in `docs/features/` |
| `back-end` | find the most structurally similar API / service feature in `docs/features/` |
| `refactoring` | find the feature most closely related to the area being refactored |
| `{custom}` | find the best structural match in `docs/features/` based on the description |

Read the reference specification.md (or legacy instructions.md) to understand the section structure, depth of detail, and reference conventions used in this codebase. Do not copy content — only calibrate structure and style.

---

## Step 2 — Create the folder and all four workflow files

Create the following files:

### `docs/features/{id}_{name}/specification.md`

Generate a pre-filled instructions.md tailored to `{type}`. Use `{description}` to seed the feature description. Mark every section that requires human input with a `<!-- TODO: ... -->` comment.

### `docs/features/{id}_{name}/plan.md`

```markdown
# Plan: PBI {id} – {name}

<!--
  Planning phase — generate this from specification.md.

  In Copilot CLI:
    Use the /feature-plan skill with featurePath: docs/features/{id}_{name}
-->
```

### `docs/features/{id}_{name}/specification.md`

```markdown
# Implementation: PBI {id} – {name}

<!--
  Implementation phase — generate this after all tests pass.

  In Copilot CLI:
    Use the /feature-implementation-doc skill with featurePath: docs/features/{id}_{name}
-->
```

### `docs/features/{id}_{name}/code-review.md`

```markdown
# Code Review: PBI {id} – {name}

<!--
  Code review phase — generate this before merge.

  In Copilot CLI: /review or use the /code-review skill
-->

## Scope

Fresh-context code review of the current branch compared to `develop`.

Reviewer mode:

- compare current branch vs `develop`
- report only substantive correctness issues
- ignore style and minor nits

## Summary

<!-- Status: Pending / Resolved -->

## Findings

<!-- Format each finding as:

### Finding N: [Short title]

- **Severity:** Critical | Major | Medium | Minor
- **File:** `path/to/file.cs`

#### Problem
#### Why it matters
#### Recommended action
#### Resolution

-->

## Review conclusion

<!-- Approved / Changes requested -->
```

---

## Step 3 — specification.md content by type

Generate the body of instructions.md using the appropriate template below. Fill in what you can from `{description}` and the reference feature. Leave `<!-- TODO: ... -->` comments wherever the developer must supply specifics.

### Type: `endpoint`

```markdown
# Task: PBI {id} – {name}

## Feature: {description}

<!-- TODO: Which API project does this endpoint belong to? e.g. Presentation.Desktop.WebApi | Presentation.Mobile.WebApi -->

<!-- TODO: Is this GET-only, or does it include write operations (POST / PUT / DELETE) and/or event publishing? -->

---

## Feature 1: <!-- TODO: verb + endpoint name, e.g. GET /GetEquipment -->

### Parameters

| Name | Type | Required | Description |
|---|---|---|---|
| <!-- TODO --> | | | |

### Behaviour

<!-- TODO: Describe the expected behaviour -->

### Response mapping

| Target Property | Type | Source Aggregate | Source Property | Notes |
|---|---|---|---|---|
| <!-- TODO --> | | | | |

### Behavioural notes

<!-- TODO: DTO placement, nullable decisions, any special mapping rules -->

---

## References

- Similar implementations: `@docs/features/{similarFeature}`
- Architecture: `@docs/ARCHITECTURE_INDEX.md`
- External code sources: <!-- TODO: link to legacy controller / BO / DTO if applicable -->
```

### Type: `sync`

```markdown
# Task: PBI {id} – {name}

## Feature: {description}

<!-- TODO: Describe the data source being synchronised (e.g. external Oracle DB, REST API, message queue) -->

<!-- TODO: Name the closest existing sync to use as the primary reference -->

---

## Source

| Source table / resource | Role |
|---|---|
| <!-- TODO: name --> | <!-- TODO: role --> |

---

## Source → Local mapping

| Source field | Source type | Local property | Notes |
|---|---|---|---|
| <!-- TODO --> | | | |

---

## Sync behaviour

- **Incremental sync cursor**: <!-- TODO: e.g. SourceUpdatedAt -->
- **Batch size**: <!-- TODO: e.g. 5 000 items per batch -->
- **Upsert strategy**: identity comparer on `<!-- TODO: external ID field -->`
- **Soft delete**: <!-- TODO: Yes / No -->

---

## References

- Similar implementations: `@docs/features/{similarFeature}`
- Architecture: `@docs/ARCHITECTURE_INDEX.md`
```

### Type: `event`

**Before generating the event template, attempt to auto-discover the event contract:**

1. If `{wikiPageUrl}` is provided, fetch it:
   ```powershell
   curl -s "{wikiPageUrl}" 2>$null
   # Or for Azure DevOps REST API (replace with actual org/project/wiki):
   # curl -s "https://dev.azure.com/{org}/{project}/_apis/wiki/wikis/{wikiId}/pages?path={pagePath}&api-version=7.1&includeContent=true"
   ```
   Extract: exchange name, routing key, payload fields, and any field→source mappings from the markdown.

2. If no wiki URL is provided or the fetch fails, search the codebase for related event classes:
   ```powershell
   Get-ChildItem -Recurse -Path src -Filter "*IntegrationEvent*.cs" | Select-String -Pattern "{name}" -List | Select-Object Path
   ```
   Read any matching publisher or consumer classes to extract exchange, routing key, and DTO shape.

3. Fill in whatever was discovered. Leave `<!-- TODO -->` only for fields that couldn't be resolved.

```markdown
# Task: PBI {id} – {name}

## Feature: {description}

<!-- TODO: Is this a publisher (outbox pattern) or a consumer (inbox pattern)? -->

---

## Event specification

| Property | Value |
|---|---|
| Exchange | `<!-- discovered or TODO -->` |
| Routing key | `<!-- discovered or TODO -->` |

### Payload

| Field | Type | Source | Notes |
|---|---|---|---|
| <!-- discovered or TODO --> | | | |

---

## References

- Similar implementations: `@docs/features/{similarFeature}`
- Architecture: `@docs/ARCHITECTURE_INDEX.md`
- Wiki contract: <!-- {wikiPageUrl} if provided -->
```

### Type: `domain-model`

```markdown
# Task: PBI {id} – {name}

## Feature: {description}

---

## Aggregate: <!-- TODO: entity name -->

### Entity shape

| Property | Type | Notes |
|---|---|---|
| <!-- TODO --> | | |

### Strongly-typed IDs

| Type | Backing type | Notes |
|---|---|---|
| `<!-- TODO -->Id` | `int` | Internal DB key |
| `<!-- TODO -->Uid` | `Guid` | External system UID |

### Repository requirements

<!-- TODO: Read-only, read-write, or both? -->

---

## References

- Architecture: `@docs/ARCHITECTURE_INDEX.md`
```

### Type: `front-end`

```markdown
# Task: PBI {id} – {name}

## Feature: {description}

<!-- TODO: Which application is this for? Presentation.Desktop.Web (Blazor WASM) | Presentation.Mobile.Web (Blazor) -->

---

## UI scope

<!-- TODO: List the pages, routes, and/or components being added or changed -->

| Component / Page | Route (if applicable) | Notes |
|---|---|---|
| <!-- TODO --> | | |

---

## Data requirements

<!-- TODO: What data does the UI need? Which API endpoints or services supply it? -->

| Data | Source endpoint / service | Notes |
|---|---|---|
| <!-- TODO --> | | |

---

## UX behaviour

<!-- TODO: Describe user interactions, loading states, error handling, and any validation rules visible in the UI -->

---

## References

- Similar implementations: `@docs/features/{similarFeature}`
- Architecture: `@docs/ARCHITECTURE_INDEX.md`
```

### Type: `back-end`

```markdown
# Task: PBI {id} – {name}

## Feature: {description}

<!-- TODO: Which layer(s) are primarily affected?
- Application layer (use cases / interactors)
- Core.Domain (aggregates, services, value objects)
- Infrastructure.Data (repositories, EF config, migrations)
- Presentation.*WebApi (endpoints, DTOs)
-->

---

## Functional requirements

<!-- TODO: Describe what the back-end change must do -->

---

## Affected aggregates / services

| Aggregate / Service | Change type | Notes |
|---|---|---|
| <!-- TODO --> | Add / Modify / Remove | |

---

## API contract changes (if any)

| Endpoint | Method | Change | Notes |
|---|---|---|---|
| <!-- TODO --> | | | |

---

## Data / migration changes (if any)

<!-- TODO: Are schema changes needed? Describe new columns, tables, or indexes -->

---

## References

- Similar implementations: `@docs/features/{similarFeature}`
- Architecture: `@docs/ARCHITECTURE_INDEX.md`
```

### Type: `refactoring`

```markdown
# Task: PBI {id} – {name}

## Feature: {description}

<!-- TODO: Describe the motivation — e.g. performance, maintainability, architecture alignment, tech debt, naming cleanup -->

---

## Scope

<!-- TODO: Which layers and files are in scope? -->

| Layer | Files / components affected | Notes |
|---|---|---|
| <!-- TODO --> | | |

---

## Current state

<!-- TODO: Describe the current implementation and what is wrong or suboptimal about it -->

### Known issues / pain points

<!-- TODO -->

---

## Target state

<!-- TODO: Describe the desired end state after the refactoring -->

### Changed behaviour (if any)

<!-- TODO: Is this a pure internal refactoring, or are there observable behavioural changes? -->

### Breaking changes (if any)

<!-- TODO: List any renamed types, changed interfaces, removed members, or migration requirements -->

| Before | After | Notes |
|---|---|---|
| <!-- TODO --> | | |

---

## Testing requirements

<!-- TODO: Which existing tests need updating? Are new tests needed to cover the refactored code? -->

---

## References

- Similar implementations: `@docs/features/{similarFeature}`
- Architecture: `@docs/ARCHITECTURE_INDEX.md`
```

### Type: `{custom}`

When `{type}` is a freeform value (not one of the predefined types above), generate a general-purpose instructions.md:

```markdown
# Task: PBI {id} – {name}

## Feature: {description}

**Type**: {type}

<!-- TODO: Describe the motivation and context for this feature -->

---

## Scope

<!-- TODO: Which layers, services, or components are involved? -->

---

## Functional requirements

<!-- TODO: List the specific things this feature must do -->

1. <!-- TODO -->

---

## Non-functional requirements

<!-- TODO: Performance, security, reliability, compatibility concerns if any -->

---

## Affected files / components

| File / Component | Change type | Notes |
|---|---|---|
| <!-- TODO --> | Add / Modify / Remove | |

---

## Testing requirements

<!-- TODO: What tests are needed? -->

---

## References

- Similar implementations: `@docs/features/{similarFeature}`
- Architecture: `@docs/ARCHITECTURE_INDEX.md`
```

---

## Step 4 — Update docs/features/README.md

Append the following entry **before** the line `## 🔧 Guidelines for New Implementations`:

```markdown
### {id}. {name} (`{id}_{name}/`)
**Status**: 🚧 In Progress
**Description**: {description}

**Documentation**:
- [Instructions](./docs/features/{id}_{name}/specification.md)
- [Plan](./docs/features/{id}_{name}/plan.md)
- [Implementation](./docs/features/{id}_{name}/implementation.md)
- [Code Review](./docs/features/{id}_{name}/code-review.md)

---
```

---

## After creation

Print a summary:

```
✅ Scaffolded: docs/features/{id}_{name}/

  specification.md   ← fill in TODO sections, then use the /feature-plan skill
  plan.md           ← generated by /feature-plan skill
  implementation.md ← generated by /feature-implementation-doc skill
  code-review.md    ← generated by /review or /code-review skill

Next: open specification.md and complete all TODO sections.
```
