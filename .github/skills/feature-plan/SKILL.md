---
name: feature-plan
description: Generate plan.md for a feature from its instructions.md. Use this when planning a feature implementation before writing code — reads instructions.md and produces a detailed, fully implementable plan with TDD order, file paths, and spec designs.
---

## Parameters

- **featurePath**: <!-- required — repo-root-relative path to the feature folder, e.g. docs/features/134500_pcm_api_getequipment -->

---

## Task

Read the feature specification and create a detailed, fully implementable `{featurePath}/plan.md`.

**Specification file lookup** (in order — use the first that exists):
1. `{featurePath}/specification.md` — current naming
2. `{featurePath}/instructions.md` — legacy naming (still supported)

Match the structure and depth of existing plans in `docs/features/` — read two or three to calibrate tone and granularity before writing.

---

## Plan structure

### 1. Overview

- One paragraph: what the endpoint / worker / sync does
- The target API surface or sync signature
- The response contract (property names the external consumer sees)

### 2. Legacy behaviour to preserve

For each significant branch in the instructions (e.g. with/without `modifiedSince`, active vs. modified set):

- Describe what the legacy system does step-by-step
- Call out any semantics that are broader or narrower than a naïve reading

### 3. Recommended design

Explain the architectural approach:

- Where the branching logic lives (application layer vs. infrastructure)
- Which existing repositories are reused
- Which new specifications are needed and why they are kept small/focused
- Any DTO placement decisions (Contracts project vs. API project)

### 4. Files to create

Grouped by layer. Each row must have:

| # | Exact solution-relative file path | Purpose (one sentence) |

Layers to group by (omit layers with no new files):

- Domain layer – `src\core\Core.Domain\`
- Application layer – `src\application\Application\`
- Infrastructure layer (if needed)
- Presentation contracts
- Presentation API
- Test files (include one test file per production file)

### 5. Files to modify

| # | Exact file path | What changes and why |

### 6. Detailed implementation order (TDD)

Phase-by-phase, test-first:

- **Phase 1: Domain specs** — for each spec: list the test cases first, then the implementation step
- **Phase 2: Application use case** — request record, data model, interactor (with test cases listed first)
- **Phase 3: Contracts and DTO mapping** — request DTO, response DTO (with test cases first)
- **Phase 4: Endpoint and validation** — tag constants, validator, endpoint
- **Phase 5: DI registration** — exact `serviceCollection.Add*(...)` call to add
- **Phase 6: Verification** — exact `dotnet build` and `dotnet test` commands from `src\`

### 7. Notes on contract compatibility

- Request parameter naming (e.g. `PlatsId` to match Giron surface)
- Response property naming
- Nullable vs. non-nullable decisions
- Reused contract types

### 8. Risks and review points

Surface anything a human reviewer should double-check before approving the plan.

---

## Requirements

- Every file path must be exact and solution-relative (no `src\src\...` doubling)
- Test file paths must sit in the correct `.Tests` project alongside the code under test
- Build and test commands must follow `.github/copilot-instructions.md` conventions
- Do not invent new patterns — match the conventions shown in existing similar implementations
- Save the completed plan to `{featurePath}/plan.md`
