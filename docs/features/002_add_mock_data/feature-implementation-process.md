# Feature Implementation Process — Walkthrough

> This document captures the real interaction used to implement PBI 002 (`add_mock_data`) and to produce its implementation document. Use it as a worked example of the implementation phase of the feature workflow.

---

## 1. The implementation prompt

The user opened a Copilot CLI session and typed:

> *"Implement according to @docs\features\002_add_mock_data\plan.md"*

The plan file was tagged so the agent would read the full implementation spec before doing any work.

---

## 2. What happened during implementation

### Step 1 — Plan reading and codebase orientation

Before writing a single file, the agent read in parallel:

- `docs/features/002_add_mock_data/plan.md` — to understand all phases, files to create, and files to modify
- `src/Core.Domain/EquipmentAggregate/Equipment.cs` — existing aggregate to be extended
- `src/Infrastructure.Data/Persistence/BookingDbContext.cs` — DbContext to be expanded
- `src/Presentation.Api/Program.cs` — seed block to be replaced
- `src/Infrastructure.Data/Persistence/Configurations/EquipmentConfiguration.cs` — pattern for new EF configurations
- `src/Core.Domain.Tests/EquipmentAggregate/EquipmentTests.cs` — existing test structure to match
- Project files (`.csproj`) — to confirm existing package references

### Step 2 — Todo tracking

The agent created a six-phase todo list matching the plan's phases (P1–P6) and updated each item as it was completed.

### Step 3 — Implementation (Phases 1–5)

The agent worked through each phase sequentially:

| Phase | What was done |
|---|---|
| **P1 – Domain** | Added `Equipment.Reconstitute`; created `Location`, `Operator`, `Booking`, `MaintenanceEvent` aggregates with `Create` and `Reconstitute` factories |
| **P2 – EF Core** | Created four `IEntityTypeConfiguration<T>` classes; added four `DbSet<T>` properties to `BookingDbContext` |
| **P3 – JSON + project files** | Created the `data/` folder with five JSON files (4 locations, 15 equipment, 10 operators, 12 bookings, 8 maintenance events); updated both `.csproj` files |
| **P4 – DataSeeder** | Created `Infrastructure.Data/Seeding/DataSeeder.cs` with idempotency guard and file-scoped seed DTOs; created `DataSeederTests.cs` with 16 tests |
| **P5 – Program.cs** | Replaced the 10-line inline seed block with a two-line `DataSeeder.Seed(db, dataDir)` call |

### Step 4 — First build failure and fix

Running `dotnet build` after all files were written revealed **two categories of errors**:

**Error 1 — Duplicate `LocationId` struct (StronglyTypedId generator collision)**

```
CS8785: Generator 'StronglyTypedIdGenerator' failed to generate source. 
hintName 'LocationId.g.cs' must be unique within a generator.
```

The plan described `Location` as defining its own `[StronglyTypedId] LocationId` struct in `Core.Domain.LocationAggregate`, alongside the existing `Core.Domain.EquipmentAggregate.LocationId`. The StronglyTypedId v1 source generator keys output files on the struct _name_ only (not the fully-qualified name), so two structs both named `LocationId` in different namespaces collided on the generated file name `LocationId.g.cs`. The failure cascaded into `CS1729` (no constructor) errors on all six strongly-typed IDs in the solution.

**Fix:** Removed the `[StronglyTypedId] LocationId` declaration from `Location.cs` and used `EquipmentAggregate.LocationId` as `Location`'s PK instead — consistent with the plan's alternative wording: _"Location.cs will reference LocationId from there."_ The `using` alias strategy the plan described for `DataSeeder.cs` was not needed.

**Error 2 — Duplicate content items in `Presentation.Api.csproj`**

```
NETSDK1022: Duplicate 'Content' items were included.
```

The plan said to use `<Content Include="data\**\*.json">`. `Presentation.Api` uses `Microsoft.NET.Sdk.Web`, which auto-includes all content files from the project directory, so `Include` registered each JSON file a second time.

**Fix:** Changed `Include` to `Update`, which adjusts the `CopyToOutputDirectory` property on the already-included items without re-adding them.

### Step 5 — Clean build and test run

After the two fixes, the build succeeded:

```
Build succeeded. 0 Warning(s). 0 Error(s).
```

Test run output:

```
Passed! — Failed: 0, Passed: 28, Skipped: 0, Total: 28, Duration: 816 ms
```

The 28 tests break down as:
- **10** — pre-existing `EquipmentTests` (all still passing)
- **9** — new aggregate factory / reconstitute unit tests
- **7** — new `DataSeeder` integration tests (count assertions + legacy GUID/name preservation)

---

## 3. The implementation document prompt

Once implementation was verified, the user invoked the documentation skill:

> *"Use the skill tool to invoke the 'feature-implementation-doc' skill, then follow the skill's instructions."*

### What the skill did

The skill's instructions specified:

1. **Discover changes** — run `git diff` and `git log`. Because this is not a git repository, there was no diff output. The agent used its knowledge of what was just implemented combined with reading the files directly.
2. **Run the tests** — `dotnet test --no-restore` was run to capture real pass/fail counts.
3. **Read key files** — the agent read the plan, specification, and all newly created files to ensure the document reflected what was actually built, not what was planned.
4. **Write `implementation.md`** — the agent followed the structure of the reference doc at `docs/features/001_get_equipment/implementation.md` for tone and format.

The document was written to `docs/features/002_add_mock_data/implementation.md` (which existed as a stub with only a comment block). The stub comment was replaced with the full document.

---

## 4. The full skill pipeline (visual)

```
User prompt: "Implement according to @plan.md"
    │
    ├─► Read plan + existing codebase (parallel)
    │
    ├─► Phase 1: Domain aggregates + Equipment.Reconstitute
    │
    ├─► Phase 2: EF Core configurations + BookingDbContext
    │
    ├─► Phase 3: JSON data files + .csproj updates
    │
    ├─► Phase 4: DataSeeder.cs + DataSeederTests.cs
    │
    ├─► Phase 5: Program.cs wiring
    │
    ├─► dotnet build  ←  2 errors found, diagnosed, fixed
    │
    ├─► dotnet build  ←  0 errors, 0 warnings ✓
    │
    └─► dotnet test   ←  28 passed, 0 failed ✓

User prompt: "feature-implementation-doc skill"
    │
    ├─► dotnet test (capture live results)
    ├─► Read plan.md + specification.md + implementation files
    ├─► Read reference: 001_get_equipment/implementation.md
    └─► Write docs/features/002_add_mock_data/implementation.md
```

---

## 5. How to repeat this for a new feature

1. **Complete planning first.** `plan.md` must be finished and rubber-duck reviewed before starting implementation. See `feature-plan-process.md`.

2. **Trigger implementation with the plan file tagged:**
   ```
   Implement according to @docs\features\{id}_{name}\plan.md
   ```
   Tagging the plan ensures the agent reads the full context before writing any code.

3. **Let the agent run all phases.** The agent works autonomously through each plan phase. Expect it to:
   - Create directories and files in the correct layers
   - Run `dotnet build` to surface compile errors
   - Fix any errors and rebuild
   - Run `dotnet test` to verify all tests pass

4. **Expect at least one build iteration.** Plans describe design intent; runtime behaviour of tools (compilers, SDK defaults, source generators) sometimes requires a fix. Two common surprises in this codebase:
   - StronglyTypedId generator keying on struct name, not fully-qualified name
   - Web SDK auto-including content files (use `<Content Update>` not `<Content Include>`)

5. **Generate the implementation doc after tests pass:**
   ```
   Use the skill tool to invoke the "feature-implementation-doc" skill, then follow the skill's instructions.
   ```
   This fills `implementation.md` with real file lists, real test counts, and documented deviations from the plan.

---

## 6. Key takeaways

| Lesson | Why it matters |
|---|---|
| **Tag the plan file** | Attaching `@plan.md` gives the agent the full implementation spec without having to search for it |
| **Build errors are normal** | A plan is a design document, not a compiler. The agent diagnoses and fixes build errors autonomously — let it |
| **Two structs, same name = generator conflict** | StronglyTypedId v1 uses struct name as the hint file name. Two strongly-typed IDs with the same name in different namespaces will collide. Use distinct names across all aggregates |
| **Web SDK auto-includes content** | `Microsoft.NET.Sdk.Web` auto-includes all files that exist in the project directory. For existing content types (JSON, etc.), use `<Content Update>` to change copy behaviour, not `<Content Include>` |
| **Verify with real test output** | The implementation doc must contain real test counts from a real test run, not counts estimated from the plan. The skill runs `dotnet test` to capture them |
| **Document deviations, not just what was done** | The most valuable part of `implementation.md` is the _implementation notes_ section — it records why the code differs from the plan, which prevents future maintainers from "fixing" a deliberate deviation |
