# Feature Planning Workflow — How This Plan Was Created

This document records the end-to-end process used to produce `plan.md` for PBI 002 – add_mock_data. It is intended as a repeatable workflow guide for anyone planning a new feature in this repository.

---

## 1. The prompt

The user opened a Copilot CLI session and typed:

```
first lessons-apply then feature-plan for @docs\features\002_add_mock_data\specification.md
```

That single line triggered a two-skill pipeline:

1. **`/lessons-apply`** — searches all completed features for recorded lessons that are relevant to this feature and surfaces them as planning context.
2. **`/feature-plan`** — reads the specification and generates a fully implementable `plan.md`.

---

## 2. Step-by-step: what happened

### Step 1 — lessons-apply

The skill searched `docs/features/**/lessons.md` across the entire repository.

**Result:** No lessons files existed yet (this was only the second feature). The skill reported this and advised extra caution around:
- Domain aggregate factory methods (missing factories can block seeding)
- EF Core `DbSet` completeness for new entities
- JSON deserialization → strongly-typed ID mapping

When lessons *do* exist, this step surfaces the most relevant past mistakes ranked by severity and domain/layer overlap. It does not write any files — it outputs planning context directly into the conversation.

### Step 2 — feature-plan: codebase exploration

Before writing a single line of plan, Copilot explored the codebase in parallel to understand the current state:

- Read `specification.md` to extract feature type, entities, and scope
- Read the reference plan at `docs/features/001_get_equipment/plan.md` to calibrate tone and depth
- Read the existing source files: `Equipment.cs`, `BookingDbContext.cs`, `Program.cs`, `EquipmentTests.cs`, `EquipmentConfiguration.cs`, project files

This grounding step ensures the plan matches the actual codebase rather than a hypothetical one.

### Step 3 — feature-plan: first draft

With the codebase understood, Copilot wrote `plan.md` covering:
- Overview and data volume targets
- Legacy behaviour to preserve (GUIDs, Equipment names)
- Recommended design (aggregates, DataSeeder, EF configurations, JSON files)
- Files to create (15 items across 4 layers)
- Files to modify (6 items)
- Detailed TDD implementation order (6 phases)
- Contract compatibility notes
- Risks and review points

### Step 4 — rubber duck review

Before accepting the plan, the user ran:

```
/rubber-duck @docs\features\002_add_mock_data\plan.md
```

The rubber duck agent performed an independent critical review and found **three blocking issues** and **four non-blocking issues**:

| # | Finding | Severity |
|---|---|---|
| 1 | `DataSeeder` placed in `Presentation.Api` but tests are in `Core.Domain.Tests` — tests cannot reference it | 🔴 Blocking |
| 2 | `Equipment.Reconstitute` needed by the seeder but never planned | 🔴 Blocking |
| 3 | `LocationId` relocation unnecessarily cascades across 5 files for a mock-data feature | 🟠 Non-blocking |
| 4 | `AppContext.BaseDirectory + 4x ..` path resolution is brittle across CI | 🟠 Non-blocking |
| 5 | Missing idempotency guard in `DataSeeder` | 🟠 Non-blocking |
| 6 | TDD ordering had compile gaps (tests reference types that don't exist yet) | 🟠 Non-blocking |

### Step 5 — plan revision

All blocking issues were fixed and all non-blocking issues were adopted:

| Change | What was done |
|---|---|
| `DataSeeder` location | Moved to `Infrastructure.Data/Seeding/DataSeeder.cs` |
| `Equipment.Reconstitute` | Added as first item in Phase 1 |
| `LocationId` relocation | Reverted — stays in `Equipment.cs`; `Location.cs` gets its own PK struct |
| Test path resolution | Replaced `..` traversal with `<Content>` link in test `.csproj` |
| Idempotency | Added `if (db.Equipment.Any()) return;` |
| TDD ordering | Reordered phases to avoid compile gaps |

The final `plan.md` reflects all of these corrections.

---

## 3. The full skill pipeline (visual)

```
User prompt
    │
    ├─► /lessons-apply
    │       └─► Searches docs/features/**/lessons.md
    │           Outputs relevant past lessons as context
    │
    ├─► /feature-plan
    │       ├─► Reads specification.md
    │       ├─► Reads reference plan (001_get_equipment/plan.md)
    │       ├─► Explores codebase (Equipment.cs, DbContext, Program.cs, tests)
    │       └─► Writes plan.md (first draft)
    │
    └─► /rubber-duck  ← user-triggered review
            ├─► Independent critique of plan.md
            └─► Blocking issues → plan.md revised
```

---

## 4. How to repeat this for a new feature

1. **Scaffold the feature folder** (if not already done):
   ```
   /feature-scaffold
   ```
   This creates `docs/features/{id}_{name}/` with `specification.md` and stub docs.

2. **Write `specification.md`** — fill in the feature type, scope table, current state, target state, and testing requirements.

3. **Run lessons-apply + feature-plan** in one prompt:
   ```
   first lessons-apply then feature-plan for @docs\features\{id}_{name}\specification.md
   ```

4. **Run a rubber duck review** before accepting the plan:
   ```
   /rubber-duck @docs\features\{id}_{name}\plan.md
   ```

5. **Revise the plan** based on blocking findings. Set aside non-blocking findings that add complexity without clear benefit.

6. **Implement** phase by phase following the plan.

---

## 5. Key lessons from this session

- **`lessons-apply` before `feature-plan`** — even when no lessons exist, the step forces you to think about whether past mistakes apply. Once lessons accumulate across features, this becomes the most valuable step.
- **Rubber duck is not optional for non-trivial plans** — the first draft had a blocking bug (`DataSeeder` in the wrong project) that would have caused a compile failure immediately on implementation. The rubber duck caught it before a single line of code was written.
- **Explore the actual codebase before planning** — the plan correctly identified that `LocationId` lives in `Equipment.cs` because Copilot read the file, not because it assumed a pattern.
- **Placement decisions matter early** — where a class lives (which project) determines what can reference it. Get this right in the plan; it is expensive to fix mid-implementation.
