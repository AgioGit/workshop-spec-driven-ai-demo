---
name: feature-lessons-learned
description: Scaffold a lessons learned document from documentation and implementation in the current feature branch. Use this after a feature is complete (tests passing, smoke-tested) to extract concrete, reusable lessons from what went wrong, what was surprising, and what should be done differently next time.
---

## Parameters

- **featurePath**: <!-- required — repo-root-relative path to the feature folder, e.g. docs/features/134500_pcm_api_getequipment -->
- **baseBranch**: <!-- default: develop -->

---

## Task

Extract concrete lessons from the current feature branch and write them to `{featurePath}/lessons.md`.

The required output format is fully specified in the **Document format** section below — do not look for format examples elsewhere.

---

## Steps

### 1. Gather branch changes

```powershell
git --no-pager diff --name-only {baseBranch}...HEAD
git --no-pager log {baseBranch}..HEAD --oneline
```

### 2. Read feature documentation

Read all of these that exist (skip missing files silently):

- `{featurePath}/specification.md` or `{featurePath}/instructions.md` (legacy) — intended behaviour; gaps between spec and reality are lesson candidates
- `{featurePath}/plan.md` — original design intent; deviations from this are lesson candidates
- `{featurePath}/implementation.md` — what was actually built; "implementation notes" sections often describe surprises
- `{featurePath}/code-review.md` — review findings that required fixes are strong lesson candidates
- `{featurePath}/smoke-test.md` — failures and bugs found during smoke testing

### 3. Read changed code files

For each changed file from step 1, read the diff:

```powershell
git --no-pager diff {baseBranch}...HEAD -- {file}
```

Focus on:
- Bug fixes (before/after patterns)
- Guard clauses or validation added after initial implementation
- Comments explaining non-obvious decisions
- Exception handling added reactively
- Test helpers that required reflection or workarounds

### 4. Extract lesson candidates

For each candidate, classify it:

| Source | Lesson type |
|---|---|
| Plan deviation | Architecture or design decision that changed during implementation |
| Code review finding that required a fix | Bug, pattern violation, or missed edge case |
| Smoke test failure | Runtime bug invisible in unit tests |
| Test helper workaround | Framework or infrastructure constraint |
| Surprising behaviour | EF Core, Dapper, Oracle, or domain model gotcha |
| Backward compatibility decision | Legacy system constraint that overrode a "clean" approach |

Discard candidates that are too project-specific to generalise, or that are obvious/trivial.

### 5. Assign metadata

For each lesson, determine:

**Severity:**
- 🔴 Critical — caused or would cause data loss, corrupt state, unhandled exception, or wrong data written to production
- 🟠 Major — caused a bug or wrong HTTP status that required a fix
- 🟡 Medium — caused a test failure, architectural issue, or required a design change
- 🔵 Minor — style, naming, or convenience issue with no runtime impact

**Tags:** 5–10 lowercase kebab-case or single-word terms. Include: the layer (`ef-core`, `gironadapter`, `oracle`, `fastendpoints`, `mediatr`), the problem type (`null-constraint`, `cascade-insert`, `silent-failure`), and any domain terms.

**Date:** Use today's date (the date this skill is invoked).

### 6. Write lessons.md

Write `{featurePath}/lessons.md`. If the file already exists, read it first — preserve any lessons already present (by their `L` ID), and append new ones continuing the numbering.

---

## Document format

### File header

```markdown
# Lessons Learned: {PBI ID} – {Feature Name}

Each lesson follows this structure for searchability and AI retrieval:
- **Header** — lesson ID + descriptive title
- **Metadata** — date discovered, severity, short summary, tags
- **Problem** — what went wrong and why
- **Fix** — concrete code or approach that resolved it
- **Rule** — generalised guidance for future features

Severity levels: 🔴 Critical · 🟠 Major · 🟡 Medium · 🔵 Minor

---
```

### Per-lesson format

```markdown
## L{NNN} · {Descriptive title — specific enough to search, general enough to apply elsewhere}

| | |
|---|---|
| **Date** | {YYYY-MM-DD} |
| **Severity** | {emoji} {Level} |
| **Summary** | {One sentence: what the problem was and what it cost.} |
| **Tags** | `tag1` `tag2` `tag3` `tag4` `tag5` |

### Problem

{2–4 sentences: what happened, what triggered it, why it wasn't caught earlier.}

### Fix

```csharp
// Concrete before/after or the fix pattern.
// Include enough context to be copy-pasteable.
```

{Optional: 1–2 sentences explaining the fix if the code alone isn't self-explanatory.}

### Rule

{The generalised, actionable rule. Written as an imperative or conditional:
"Always X when Y." / "Never use X for Z." / "Before doing X, check Y."
Should apply to future features beyond this one.}

---
```

### Numbering

- IDs are zero-padded three digits: `L001`, `L002`, …
- If appending to an existing file, continue from the highest existing ID.

---

## Canonical tag vocabulary

Use tags from these lists to keep lessons consistently searchable across features. Combine a **layer tag**, a **problem-type tag**, and any **domain tags** that apply.

### Layer tags
`ef-core` `gironadapter` `oracle` `fastendpoints` `mediatr` `rabbitmq` `dapper` `blazor` `di` `ardalis-spec` `strongly-typed-id`

### Problem-type tags
`null-constraint` `cascade-insert` `tracking-conflict` `silent-failure` `schema-prefix` `reserved-word` `utc-conversion` `id-generation` `soft-delete` `missing-registration` `wrong-repository` `pagination` `naming` `backward-compat` `spec-deviation` `test-isolation` `reflection-workaround`

### Domain tags
`borehole` `borehole-status` `borehole-operation` `borehole-charge-anomaly` `ring` `drift` `production-area` `site` `giron-write` `pcm-facade` `outbox` `event-publishing`

When assigning tags, pick **the most specific applicable terms** from each group. Do not invent new tags if an existing one fits.

---

## Quality criteria

Each lesson must pass all of these before being included:

1. **Concrete** — describes something that actually happened in this branch (not generic advice)
2. **Reusable** — the Rule applies to future features, not just this one
3. **Actionable** — a developer reading it knows exactly what to do differently
4. **Non-trivial** — not obvious to a developer familiar with the codebase
5. **Has a Fix** — there is a concrete code pattern or decision, not just "be careful"

Aim for 5–15 lessons per feature. Fewer is fine if the feature was straightforward. Do not pad with trivial observations.

---

## Requirements

- Read actual git diff and documentation — do not invent lessons
- Every lesson must trace to something concrete in the diff or docs
- Use the exact format above — metadata table, then Problem / Fix / Rule sections
- Lessons already in the file (if it exists) must not be modified or re-numbered
- Save the completed document to `{featurePath}/lessons.md`
