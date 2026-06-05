# Feature Finalization Process — Walkthrough

> This document captures the real interactions used to finalize PBI 002 (`add_mock_data`) after implementation was complete. It covers the code review, the lessons learned extraction, and the subsequent fix cycle. Use it as a worked example of the finalization phase of the feature workflow.

---

## Overview — The Finalization Pipeline

After implementation is complete and tests pass, every feature goes through a three-step finalization pipeline:

```
Code Review  →  Fix Findings  →  Lessons Learned
```

| Step | Skill / Action | Output file |
|---|---|---|
| 1 | `/code-review` skill | `code-review.md` |
| 2 | Manual: fix findings + re-run tests | updated source files + resolved `code-review.md` |
| 3 | `/feature-lessons-learned` skill | `lessons.md` |

---

## Step 1 — Code Review

### The prompt

The user opened a Copilot CLI session and typed:

> *"Use the skill tool to invoke the following skills in order: code-review, feature-lessons-learned. Then follow each skill's instructions to help with: - First and document to `@docs\features\002_add_mock_data\code-review.md` - Then"*

Both skills were invoked in sequence. The `code-review` skill ran first.

> **Note:** The user asked for both skills in a single message. Copilot executed them in the stated order — code review first, then lessons learned — and wrote both output files before finishing.

---

### What the code review skill does

The `/code-review` skill:

1. Runs `git diff --name-only develop...HEAD` and `git log develop..HEAD --oneline` to enumerate all changed files.
2. Groups files by category (new, modified, deleted).
3. Reviews each file against five dimensions: Logic & Correctness, Architecture compliance, Security, Performance, and Test coverage.
4. Cross-references findings against `plan.md` and `specification.md` — if a finding is explicitly described in the plan it is labelled **"By design"** and removed from the required-action checklist.
5. Writes the findings to `{featurePath}/code-review.md` in a structured format with severity, file, problem, recommended fix, and a resolution field.
6. Ends with a **Code Review Summary** (Approved / Changes requested) and an actionable checklist.

---

### What happened for PBI 002

**Repository context:** This repository has no git history (it is not yet a git repository), so `git diff` was unavailable. The agent adapted by reading all implementation files directly — cross-referencing `implementation.md` (which lists every file created or modified) against the live source code.

The agent read in parallel:

- All four new domain aggregates (`Location`, `Operator`, `Booking`, `MaintenanceEvent`)
- `Equipment.cs` (modified — `Reconstitute` factory added)
- `DataSeeder.cs` (new)
- `BookingDbContext.cs` (modified)
- All four EF Core configuration files (new)
- `Program.cs` (modified)
- `DataSeederTests.cs` (new)
- All five JSON seed files
- Both `.csproj` files
- `specification.md`, `plan.md`, and `implementation.md` for cross-referencing

**Four findings were raised:**

| # | Severity | File | Finding |
|---|---|---|---|
| 1 | Medium | `DataSeeder.cs` | Null-forgiving `!` on `JsonSerializer.Deserialize<T>` — produces a cryptic `NullReferenceException` if a JSON file contains `null`; replace with `?? throw new InvalidOperationException(...)` naming the file |
| 2 | Medium | `Booking.cs` | `Create` and `Reconstitute` accept `EndTime <= StartTime` silently — domain invariant missing |
| 3 | Minor | `Equipment.cs` + `equipment.json` | `Equipment.Reconstitute` has no `lastServiceDate` parameter — all 15 seeded rows always show `null` for that API response field |
| 4 | Minor | `Equipment.cs` | `Equipment.Reconstitute` uses manual `if`-throw guards instead of `Guard.Against.NullOrWhiteSpace` used by all new aggregates |

**Three findings were dismissed as By Design** (documented in a collapsed section in `code-review.md`):

- Idempotency guard checks only `db.Equipment.Any()` — correct for in-memory DB, documented in plan §8
- `<Content Update>` instead of `<Content Include>` — required by Web SDK auto-globbing, documented in `implementation.md`
- Single `LocationId` struct instead of two — forced by StronglyTypedId source generator constraint, documented in `implementation.md`

**Verdict:** ✅ Approved with minor recommendations (no critical/major blockers).

---

## Step 2 — Fix Findings

### The prompt

After reviewing `code-review.md`, the user typed:

> *"Fix findings found in `@docs\features\002_add_mock_data\code-review.md` as recommended and update documentation."*

The tagged file gave Copilot direct access to the findings without re-reading the whole conversation.

---

### What happened

The agent planned all four fixes, checked for dependencies between them (Finding 3 affected both `Equipment.cs` and `DataSeeder.cs`), and applied them in parallel where independent:

**Fix 1 — DataSeeder null-forgiving operator (`DataSeeder.cs`):**

```csharp
// Before
T Deserialize<T>(string fileName) =>
    JsonSerializer.Deserialize<T>(
        File.ReadAllText(Path.Combine(dataDirectory, fileName)), JsonOptions)!;

// After
T Deserialize<T>(string fileName) =>
    JsonSerializer.Deserialize<T>(
        File.ReadAllText(Path.Combine(dataDirectory, fileName)), JsonOptions)
    ?? throw new InvalidOperationException(
           $"DataSeeder: '{fileName}' deserialised to null. " +
           $"Verify the file contains a valid JSON array.");
```

**Fix 2 — Booking time-range guard (`Booking.cs`, both `Create` and `Reconstitute`):**

```csharp
if (endTime <= startTime)
    throw new ArgumentException("EndTime must be strictly after StartTime.", nameof(endTime));
```

**Fix 3 — `Equipment.Reconstitute` + `equipment.json` + `DataSeeder.cs`:**

- Added `DateTimeOffset? lastServiceDate = null` optional parameter to `Equipment.Reconstitute`
- Extended `EquipmentSeedRecord` in `DataSeeder.cs` with `DateTimeOffset? LastServiceDate = null`
- Passed `r.LastServiceDate` through to the `Reconstitute` call in the seeder
- Added `lastServiceDate` values to 9 of 15 rows in `equipment.json`

**Fix 4 — Guard alignment in `Equipment.Reconstitute` (`Equipment.cs`):**

```csharp
// Before (manual style)
if (string.IsNullOrWhiteSpace(name))
    throw new ArgumentException("Value cannot be null or whitespace.", nameof(name));

// After (project convention)
Guard.Against.NullOrWhiteSpace(name, nameof(name));
Guard.Against.NullOrWhiteSpace(type, nameof(type));
```

**Tests added (5 new):**

| Test name | Covers |
|---|---|
| `Booking_Create_WithEndTimeBeforeStartTime_Throws` | Finding 2 |
| `Booking_Create_WithEndTimeEqualToStartTime_Throws` | Finding 2 boundary |
| `Booking_Reconstitute_WithEndTimeBeforeStartTime_Throws` | Finding 2 on `Reconstitute` |
| `Equipment_Reconstitute_WithLastServiceDate_SetsProperty` | Finding 3 |
| `Seed_SomeEquipmentHasLastServiceDate` | Finding 3 integration |

**Test results after fixes:** 33 passed, 0 failed (was 28 before).

The agent then updated `code-review.md`:
- Each finding's `#### Resolution` field was filled in with what was done
- The summary block was updated to show all findings resolved
- The conclusion checklist was changed from `[ ]` to `[x]` for all items

---

## Step 3 — Lessons Learned

### What happened

The `/feature-lessons-learned` skill ran as part of the same session as the code review (Step 1 prompt). It ran **after** the code review was complete and written to `code-review.md`.

The skill:

1. Attempted `git diff` — not available (no git history); adapted by reading the feature documentation directly.
2. Read `specification.md`, `plan.md`, `implementation.md`, and `code-review.md`.
3. Extracted lesson candidates from:
   - The two explicit plan deviations documented in `implementation.md` (two `LocationId` struct collision; `Content Update` vs `Content Include`)
   - The plan's risk notes (§8 in `plan.md`)
   - The four code-review findings
4. Applied quality criteria: each lesson must be concrete, reusable, actionable, non-trivial, and have a fix.
5. Wrote `lessons.md` with seven lessons (L001–L007).

**The seven lessons extracted:**

| ID | Severity | Topic |
|---|---|---|
| L001 | 🔴 Critical | StronglyTypedId v1 hint-file collision — same struct name in different namespaces breaks the build |
| L002 | 🟡 Medium | Web SDK: `<Content Update>` not `<Content Include>` to avoid `NETSDK1022` |
| L003 | 🟠 Major | Test projects need explicit `<Content Link>` for seed JSON files or integration tests throw `FileNotFoundException` |
| L004 | 🟡 Medium | Aggregates need a `Reconstitute(id, …)` factory before deterministic seeding can compile |
| L005 | 🟡 Medium | `DataSeeder` belongs in `Infrastructure.Data`, not `Presentation.Api`, for test reachability |
| L006 | 🟠 Major | Never use `!` on `JsonSerializer.Deserialize` — use `?? throw` with a message naming the file |
| L007 | 🟡 Medium | Time-bounded aggregates must guard `EndTime > StartTime` in both `Create` and `Reconstitute` |

> **Note:** Lessons L001–L005 were sourced from the implementation deviations. Lessons L006–L007 were sourced from the code-review findings. This is the intended cross-pollination between the two documents.

---

## How to apply this workflow to a new feature

### Prerequisites

The feature must have:
- `implementation.md` filled in (confirms what was actually built)
- `dotnet test` passing

### Step-by-step

**1. Run the code review skill**

```
Use the skill tool to invoke the following skills in order: code-review, feature-lessons-learned.
Then follow each skill's instructions to help with:
- First document to @docs\features\{id}_{name}\code-review.md
- Then document to @docs\features\{id}_{name}\lessons.md
```

> Tag the feature folder path so Copilot targets the correct files. Both skills can be invoked in a single message — Copilot will run them sequentially.

**2. Review the output**

Open `code-review.md`. For each finding:
- Read the **Severity** — Critical and Major must be fixed before merge. Medium and Minor are recommended.
- Read the **Recommended action** — it includes a concrete code snippet.
- Confirm that "By design" dismissals in the collapsed section are genuinely expected (not bugs that slipped through).

**3. Fix the findings**

```
Fix findings found in @docs\features\{id}_{name}\code-review.md as recommended and update documentation.
```

Copilot will:
- Apply all code fixes
- Add tests covering the new behaviour
- Update the `#### Resolution` field for each finding in `code-review.md`
- Update the summary and conclusion sections

Run `dotnet test` after to confirm all tests pass.

**4. Check lessons.md**

Open `lessons.md` and verify:
- Each lesson traces to something real in the feature (not generic advice)
- Tags are drawn from the canonical vocabulary in the skill instructions
- The `Rule` section is written as a generalised imperative usable in future features

---

## Output files produced

| File | When written |
|---|---|
| `code-review.md` | After Step 1 (code review skill) |
| `lessons.md` | After Step 1 (lessons-learned skill, same session) |
| `code-review.md` (updated) | After Step 2 (findings fixed; resolutions filled in) |
| Source files | After Step 2 (actual fixes applied) |

---

## Tips

- **Invoke both skills in one message.** The code review context (what was found) is available to the lessons skill immediately if they run in the same session. This produces better lessons because the skill can reference specific findings by number.
- **Tag the feature folder path** in your prompt so Copilot writes to the right `code-review.md`. Without the `@` tag, the skill auto-detects from the branch name — but in a repo without git history this fallback is not available.
- **Fix findings before finalizing lessons.** Lessons L006 and L007 in this feature were extracted from code-review findings that were subsequently fixed. If lessons are extracted before fixes are applied, the `Rule` sections are still valid but the `Fix` code blocks may not reflect the final state of the codebase.
- **No git history? No problem.** The code review skill prefers `git diff` but falls back gracefully to reading `implementation.md` + live source files. All findings in this walkthrough were found without git.
