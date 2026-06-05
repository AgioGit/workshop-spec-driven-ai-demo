# Code Review: PBI 100 – get_equipment

## Scope

Code review of the `001_get_equipment` implementation against the specification and plan.

Reviewer mode:
- Review all new files against specification, plan, and architecture conventions
- Report substantive correctness issues only

## Summary

**Status:** Approved ✅  
Two findings raised during review, both resolved before approval.

## Findings

### Finding 1: Missing Guard clause in interactor constructor

- **Severity:** Medium
- **File:** `src/Application/UseCases/GetEquipment/GetEquipmentInteractor.cs`
- **Category:** Architecture / Logic

#### Problem
The interactor's constructor injected `IEquipmentReadRepository` but did not call `Guard.Against.Null(repository)`. This violates the convention stated in `.github/copilot-instructions.md`: "Every service/handler constructor uses `Guard.Against.Null(...)` for all injected dependencies."

#### Why it matters
Without the guard, a misconfigured DI container silently passes a null repository, causing a `NullReferenceException` in `HandleAsync` rather than a clear `ArgumentNullException` at startup. The error message is harder to diagnose in production.

#### Recommended action
Add `Guard.Against.Null(repository, nameof(repository));` as the first line of the constructor.

#### Resolution
✅ Resolved — guard clause added in `GetEquipmentInteractor.cs`.

---

### Finding 2: StatusFilter query string casing not normalised before Enum.TryParse

- **Severity:** Medium
- **File:** `src/Presentation.Api/Endpoints/Equipment/Get.cs`
- **Category:** Logic

#### Problem
The specification states "Case-insensitive" for `StatusFilter`. The initial implementation called `Enum.TryParse<EquipmentStatus>(raw, ignoreCase: false, out var status)`. Passing `available` (lowercase) returned HTTP 400 instead of matching `Available`.

#### Why it matters
Clients using lowercase values would receive unexpected 400 responses. The spec is explicit that matching should be case-insensitive.

#### Recommended action
Pass `ignoreCase: true` as the second argument to `Enum.TryParse`.

#### Resolution
✅ Resolved — `ignoreCase: true` added.

---

## Review conclusion

**Approved** — both findings resolved. No further changes required before merge.
