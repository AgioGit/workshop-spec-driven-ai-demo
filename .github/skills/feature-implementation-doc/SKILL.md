---
name: feature-implementation-doc
description: Generate implementation.md documenting what was built in the current feature branch. Use this after a feature implementation is complete and all tests pass to produce a structured record of files created, deviations from plan, API contract, and test results.
---

## Parameters

- **featurePath**: <!-- required — repo-root-relative path to the feature folder, e.g. docs/features/134500_pcm_api_getequipment -->
- **baseBranch**: <!-- default: develop -->

---

## Task

Inspect what was built in the current branch and generate a comprehensive `{featurePath}/implementation.md`.

Match the structure and tone of existing implementation docs in `docs/features/` — read one or two before writing.

---

## Steps

### 1. Discover changes

Run:

```powershell
git --no-pager diff --name-only {baseBranch}...HEAD
git --no-pager log {baseBranch}..HEAD --oneline
```

### 2. Run the tests and capture results

Run from `src\`:

```powershell
dotnet test --no-restore
```

Capture the pass/fail counts per test project.

### 3. Read key files

For each new file discovered in step 1, read it to understand its actual purpose (do not invent from the plan).

Also read `{featurePath}/plan.md` to identify any deviations from the original design.

Read the feature specification for context on intended behaviour:
- `{featurePath}/specification.md` if present, otherwise `{featurePath}/instructions.md` (legacy naming).

### 4. Write implementation.md

---

## Document structure

### Summary

- What was implemented (one paragraph)
- Which plan was followed and whether there were any deviations

### Files created (N new files)

Grouped by layer:

| File | Purpose |
|---|---|

Layers: Domain, Application, Infrastructure (if any), Presentation – Contracts, Presentation – API, Tests

### Files modified (N files)

| File | Change |
|---|---|

### Implementation notes

For each meaningful deviation from `plan.md`, or non-obvious decision made during implementation, write a numbered note explaining:

- what was done
- why it differs from the plan or why it was necessary
- what the impact is

### API contract

```text
METHOD /Route?Param={value}
```

- List all parameters with type and required/optional
- Auth policy
- Response type
- JSON response example

### Test results

```text
{project name}: N passed, 0 failed
...
Final solution build: Successful (N warnings, 0 errors)
```

Commands run (from `src\`):

```bash
dotnet test ...
dotnet build ...
```

### Known follow-up items (optional)

If any scope was intentionally deferred, document it here.

---

## Requirements

- Use actual git diff output — do not fabricate file lists
- Test result counts must come from real test run output
- Note every deviation from the plan, even small ones
- Save the completed document to `{featurePath}/implementation.md`
