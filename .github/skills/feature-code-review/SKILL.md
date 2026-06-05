---
name: code-review
description: Perform a thorough code review of changes in the current branch compared to develop (or another base branch). Reviews logic correctness, architecture compliance, security, performance, and test coverage. Use this when reviewing feature branch changes before merge.
---

## Parameters

- **featurePath**: <!-- optional — repo-root-relative path to the feature folder, e.g. docs/features/134500_pcm_api_getequipment. If omitted, auto-detect from the current branch name (last path segment maps to the feature folder). -->
- **baseBranch**: <!-- default: develop -->

---

## Objective

Perform a thorough code review of code changes in the current Git branch compared to a target branch (default: `develop`). Identify issues, suggest improvements, and ensure the changes follow established patterns and best practices.

---

## Review process

### Step 1: Gather changes

Run:
```powershell
git --no-pager diff --name-only develop...HEAD
git --no-pager log develop..HEAD --oneline
```

Constraints:
- If number of changed files > 10: provide a 1-page summary and flag top 5 files for deep review.
- If a diff chunk is larger than 2000 lines, only review the nearest 200 lines around each change by default.
- Always include a final "Code Review Summary" (Approved / Changes requested) and a short actionable checklist.

### Step 2: Categorize changes

Group changed files by: New Files, Modified Files, Deleted Files, Renamed Files.

### Step 3: Review each file

For each changed file, perform reviews relevant to its layer and type.

### Step 4: Cross-reference findings with the feature plan

After collecting findings, auto-detect or use `{featurePath}` to locate the feature folder.

1. Read `{featurePath}/plan.md` and `{featurePath}/specification.md` (fallback: `instructions.md`).
2. For each finding, determine whether it is:
   - ✅ **By design** — the plan or specification explicitly describes this approach; include a brief note with the plan reference so the reviewer understands the intent.
   - ⚠️ **Deviation** — differs from the plan but may be intentional; flag for human confirmation.
   - 🐛 **Genuine issue** — not addressed in the plan; requires a fix.
3. Annotate each finding with one of these labels in its header: `[By design]`, `[Deviation – confirm]`, or no label (genuine issue).
4. Suppress findings labelled `[By design]` from the final checklist — list them in a separate collapsed section "Findings dismissed as by design" for transparency.

---

## Review categories

### 1. Logic & Correctness

- Null checks before accessing properties/methods? `Guard.Against.Null()` in constructors?
- Empty collections, zero/negative values, boundary conditions handled?
- All async methods awaited? `CancellationToken` passed through?
- No `.Result` or `.Wait()` blocking calls?
- No multiple enumeration of `IEnumerable`?
- `IDisposable` resources disposed correctly?
- Exceptions caught appropriately and logged with context? No swallowed exceptions?

### 2. Architecture & Pattern Compliance

- Domain entities have no external dependencies?
- Application layer doesn't reference infrastructure directly?
- Presentation layer uses DTOs, not domain entities in responses?
- Queries use `IQueryingInteractor<TRequest, TResponse>`; commands use `CommandingInteractorBase`?
- FastEndpoints: correct base class (`EndpointWithoutRequest<TRes>` or `Endpoint<TReq, TRes>`), `Configure()` sets route and policies, `HandleAsync()` delegates to MediatR?
- Repository interfaces defined in Core.Domain, implementations in Infrastructure.Data?
- Read repositories (`IReadRepository`) for queries; write repositories (`IRepository`) for commands?
- Services registered with correct DI lifetime? No service locator pattern?

### 3. Security

- All user inputs validated? Guard clauses on method parameters? Request validators on endpoints?
- Parameterized queries used (no string concatenation in SQL)?
- Endpoints have `Policies()` configured? No hardcoded credentials?
- No PII logged? Secrets in configuration, not code?

### 4. Performance

- N+1 query problems avoided? Appropriate `Include()` for eager loading?
- Large result sets paginated?
- `StringBuilder` used for string concatenation in loops?
- I/O operations are async?

### 5. Test coverage

- New public methods have tests? Edge cases covered?
- Arrange-Act-Assert pattern followed? Meaningful test names?
- Happy path, error cases, and boundary conditions tested?

---

## Output format

For each issue found:

```
### [SEVERITY] Brief title

- **File:** `path/to/file.cs`
- **Category:** Logic / Architecture / Security / Performance / Style

**Problem:** Description of the issue.

**Suggested fix:** How to resolve it.
```

Severity levels: **Critical** (must fix before merge) | **Major** (should fix) | **Medium** (recommended) | **Minor** (optional)

End with a **Code Review Summary**: Approved / Changes requested, plus a short checklist of required actions.

---

## Useful git commands

```powershell
git --no-pager diff --name-only develop...HEAD
git --no-pager diff develop...HEAD -- path/to/file.cs
git --no-pager log develop..HEAD --oneline --stat
```
