# Security Review Follow-Up Investigation

## Problem

The recent security pass surfaced three implemented issues that merit deeper investigation before any remediation work is scoped:

1. **Invitation revocation authorization gap** — the revoke flow appears to rely on account context at the service layer, then revokes by raw invitation ID without a processor-level permission check or account ownership validation.
2. **Cross-account single-resource reads** — several `IRetrievalService` single-item methods appear to allow access based on user context plus resource-type authorization, while the underlying processors fetch by raw ID without validating that the resource belongs to the caller's current account.
3. **System-context list/query crash path** — some account-scoped list processors still dereference `CurrentAccount` even though system context is allowed through higher-level authorization checks and does not have a current account.

This plan started as **investigation only**. That investigation produced a concrete auth-layering migration plan, and the **`Now` + `Proc+` remediation tranche has since been implemented and validated**. The remaining items in this plan are still useful for tracking what is complete versus what is intentionally deferred.

## Current Status

| Area | Status | Notes |
|---|---|---|
| **Auth-layering audit** | **Done** | Service-vs-processor inventory completed and bucketed into `Now`, `Proc+`, `Sig`, and `Svc` |
| **`Now` bucket implementation** | **Done** | Safe service-layer boundary checks were moved into processor decorators/implementations |
| **`Proc+` bucket implementation** | **Done** | Moveable boundary checks were pushed down and internal request/signature plumbing was updated where needed |
| **Validation of `Now` + `Proc+` work** | **Done** | Repository rebuild/test flow is green after the migration |
| **System-context list/query crash path for migrated list ops** | **Done** | Account-scoped list processors now use `request.AccountId` instead of `CurrentAccount` on the migrated paths |
| **Wider system-context compatibility sweep** | **Done** | No additional crash paths were found on the migrated retrieval/list surfaces; the adjacent `UpdateGroupAsync` / `UpdateRoleAsync` account-scope mismatches identified during the sweep were fixed in the same tranche |
| **`Sig` bucket** | **Done** | `RevokeInvitationAsync`, `GetPermissionAsync`, `GetGroupAsync`, `GetRoleAsync`, and `GetUserAsync` now preserve account scope into the processor layer and enforce ownership there |
| **`Svc` bucket** | **Intentionally retained at service layer** | This is not a pending migration bucket; these flows should stay service-guarded unless a larger refactor removes their pre-processor `UserContext` coupling |
| **Invitation revocation follow-up** | **Done** | `RevokeInvitationAsync` now preserves `AccountId` across the service→processor boundary, authorizes in the processor decorator, and loads invitations by both `InvitationId` and `AccountId` |
| **Cross-account single-resource read follow-up** | **Done** | The four single-resource retrieval paths now pass current-account scope into processor decorators and query by both resource identity and account ownership |
| **Wider security review synthesis** | **Done** | The three follow-up issues are now synthesized below with their final impact, remediation outcome, and recommendation; no additional auth-boundary remediation remains in scope for this plan |

## Scope

In scope:

- Validate the exact exploit conditions for the three verified findings
- Audit the broader service-layer vs. processor-layer authorization split to determine whether issue 1 reflects a larger architectural pattern
- Identify all directly affected public service methods and processor paths
- Confirm whether each issue is tenant-isolation, permission-enforcement, or reliability-only
- Gather enough evidence to support a fix plan later
- Record which parts of the auth-layering follow-up have now been implemented versus deferred

Out of scope:

- Additional auth-boundary migration beyond the completed `Now` / `Proc+` / `Sig` work; `Svc` remains intentionally service-layer-owned in the current architecture
- Expanding into speculative or unimplemented future features
- General hardening or "nice to have" improvements

## Pre-Remediation Findings

### Auth-layering architecture audit

The follow-up audit broadened the question from individual bugs to a larger architecture decision: whether service-level authorization checks should move down into processor decorators now that most account and user targeting data had been pushed into request models.

The current answer is **not wholesale**.

- Moving authorization downward **does** look like the right direction for many request-scoped operations.
- It is **not** a complete fix for issue 2 by itself, because several single-resource read paths still do not carry account-scoping information.
- It would be actively unsafe to remove all service-layer checks first, because some services dereference `UserContext` before they ever call a processor.

Current migration buckets from the audit of auth-guarded service methods:

| Bucket | Count | Meaning |
|---|---:|---|
| **Now** | **10** | Safe to move into processor decorators without signature changes or service refactoring |
| **Proc+** | **26** | Moveable, but the processor decorator must absorb the existing boundary check (for example `HasAccountContext(request.AccountId)`) |
| **Sig** | **5** | Not safely moveable without changing data flow, method signatures, or adding ownership lookup |
| **Svc** | **12** | Keep at service layer unless the service implementation is refactored first, because it reads `UserContext` before processor entry |

Key architectural findings so far:

1. **Issue 1 is a real layering bug** — `RevokeInvitationAsync` loses `AccountId` between service and processor, so the processor cannot re-check the same boundary the service had.
2. **Issue 2 is a data-flow problem as much as a layering problem** — `GetPermissionAsync`, `GetGroupAsync`, `GetRoleAsync`, and `GetUserAsync` currently lack account-scoped inputs, so simply moving the current check downward would preserve the gap instead of closing it.
3. **Not every service-level check is redundant** — self operations such as password, MFA, Google auth, and some deregistration paths still need a pre-call guard because the service implementation reads `context.User` before reaching a processor.
4. **Some apparently symmetric cases are already stronger in the processor implementation** — for example account retrieval and account key retrieval already check `AvailableAccounts` inside the processor implementation, so those are not the same shape as the cross-account read bugs.

Important examples by bucket:

- **Sig**: `RevokeInvitationAsync`, `GetPermissionAsync`, `GetGroupAsync`, `GetRoleAsync`, `GetUserAsync`
- **Svc**: `SetPasswordAsync`, `DeregisterUserAsync`, `DeregisterUserFromAccountAsync`, `DeregisterBasicAuthAsync`, MFA service methods, Google auth service methods
- **Proc+**: most account-scoped create/update/delete/list operations where the request already contains `AccountId`

This means the likely remediation path, if pursued later, is:

1. Move clearly request-scoped boundary checks down for the **Proc+** bucket.
2. Fix the **Sig** bucket by preserving or reintroducing account-scoping data at the processor/decorator boundary.
3. Treat the **Svc** bucket as intentionally service-layer-owned work. Do not move those guards unless the service implementations are first refactored to stop pulling identifiers from `UserContext` before processor entry.

### Implemented from this plan

The first remediation tranche from this audit is now complete:

1. **`Now` + `Proc+` authorization moves** were implemented.
2. **Processor-side account scoping** was added for the migrated list/delete/assignment paths that needed `AccountId` to preserve the moved boundary.
3. **System-context compatibility** for the migrated account-scoped list operations was fixed by removing reliance on `CurrentAccount` in those processors.
4. **`Sig` item 1 (`RevokeInvitationAsync`)** was implemented as a signature/data-flow fix by preserving `AccountId` into the processor layer and scoping the revoke lookup to the requested account.
5. **Remaining `Sig` single-resource reads** were implemented by passing current-account scope from `RetrievalService` into the processor layer, moving the auth boundary into processor decorators, and scoping concrete permission/group/role/user lookups by both resource ID and account membership.

The following items from the audit were intentionally **not** implemented and are **not recommended** as follow-on auth-boundary moves in the current architecture:

1. **`Svc` paths** — self/user-context-coupled flows that still require pre-call guards at the service layer

### Wider system-context sweep findings

After the targeted fix for the migrated list/query paths, a wider pass was done over remaining `CurrentAccount` usage in service and processor code to check for adjacent system-context failures.

Findings:

1. **No additional crash paths were found on the migrated retrieval/list surfaces.**
   - `RetrievalService.GetEffectivePermissionsAsync(...)` explicitly returns an empty permission list for system context instead of dereferencing `CurrentAccount`.
   - `UserProcessor.GetUserByIdAsync(...)` still has a `CurrentAccount` fallback for hydrated reads when no explicit `accountId` is supplied, but the public retrieval path now passes account scope explicitly, so the migrated single-resource read fix does not depend on that fallback.

2. **Two adjacent non-crashing compatibility gaps were found outside the original list/query crash fix and were remediated in the same follow-up.**
   - `GroupProcessor.UpdateGroupAsync(...)`
   - `RoleProcessor.UpdateRoleAsync(...)`

   In both cases, the processor authorization decorator authorizes the call using `request.AccountId`, which allows system context through `HasAccountContext(request.AccountId)`, but the processor implementation was still performing the lookup using `_userContextProvider.GetUserContext()?.CurrentAccount?.Id` instead of `request.AccountId`.

   Result before the fix:
   - system context did **not** crash on these paths
   - but system-context calls could fall through to **NotFound** even when the requested account was explicitly supplied and authorized

   Remediation:
   - both processors now use `request.AccountId` for the lookup, so the execution path matches the authorization boundary
   - targeted tests were added to prove group/role updates still succeed when `CurrentAccount` is missing but an explicit request account is supplied

3. **Current assessment of issue 3 scope.**
   - The original issue 3 remediation is complete for the known migrated list/query paths.
   - The broader sweep did **not** uncover more null-dereference crash paths of the same kind.
   - It **did** uncover a narrower follow-on reliability/consistency issue on the group/role update paths, with the same underlying design smell: authorization used explicit account scope, but execution still consulted ambient `CurrentAccount`.
   - That narrower follow-on issue is now fixed.

## Final Synthesis

The three original follow-up issues from this plan are now closed.

### Issue 1 — invitation revocation authorization gap

- **Type:** permission-enforcement and tenant-isolation flaw
- **Final assessment:** confirmed and fixed
- **Root cause:** service-layer account scoping was not preserved into the processor layer, so revoke execution ran on raw invitation ID without the same boundary the service had enforced
- **Remediation:** `RevokeInvitationAsync` now preserves `AccountId`, authorizes in the processor decorator, and looks up by `InvitationId + AccountId`

### Issue 2 — cross-account single-resource reads

- **Type:** tenant-isolation/data-exposure flaw
- **Final assessment:** confirmed and fixed
- **Root cause:** the public retrieval surface entered through context-level checks, but downstream processors fetched permission/group/role/user records by raw ID without carrying account scope into the query
- **Remediation:** `GetPermissionAsync`, `GetGroupAsync`, `GetRoleAsync`, and `GetUserAsync` now pass explicit account scope into the processor layer, authorize there, and query by resource identity plus account ownership

### Issue 3 — system-context list/query crash path

- **Type:** secure-usability / reliability issue, with adjacent consistency fallout
- **Final assessment:** confirmed and fixed on the affected migrated paths, then widened and closed with follow-up fixes
- **Root cause:** some processor paths allowed system context at the authorization boundary but still read ambient `CurrentAccount` during execution
- **Remediation:** migrated list/query processors now use request/account inputs instead of `CurrentAccount`, and the wider sweep follow-up fixed the same mismatch on `UpdateGroupAsync` and `UpdateRoleAsync`

### Confirmed affected surfaces

The follow-up investigation ultimately confirmed and remediated the following surfaces:

1. `IInvitationService.RevokeInvitationAsync` / invitation processor revoke path
2. `IRetrievalService.GetPermissionAsync`
3. `IRetrievalService.GetGroupAsync`
4. `IRetrievalService.GetRoleAsync`
5. `IRetrievalService.GetUserAsync`
6. Migrated account-scoped list/query processors that previously relied on `CurrentAccount`
7. `GroupProcessor.UpdateGroupAsync`
8. `RoleProcessor.UpdateRoleAsync`

### Final recommendation

This plan does **not** leave another auth-boundary implementation tranche behind.

Recommended stance after the completed work:

1. Keep the completed `Now` / `Proc+` / `Sig` remediation as the final auth-boundary migration for this track.
2. Treat `Svc` as intentionally service-layer-owned until any future refactor removes pre-processor `UserContext` coupling from those service implementations.
3. Treat this plan as **closed for implementation**; any further work should come from separate security-review areas rather than from unfinished items in this follow-up plan.

## Investigation Approach

### 0. Auth-layering migration feasibility

Produce and maintain a method-by-method inventory of auth-guarded service operations and classify whether their current service-layer check can move to processor decorators safely.

Questions to answer:

- For each service authorization decorator method, does the downstream processor method already have enough information to perform the same boundary check?
- If not, is the gap caused by signature loss, missing request data, or the need for ownership lookup?
- Which methods only need decorator movement, and which require a deeper service/processor contract change?
- Which methods must keep a service-layer guard because the service implementation itself dereferences `UserContext` before calling any processor?

Expected output:

- A full method table with migration classification
- A corrected count by migration bucket
- A recommendation on whether the broader migration should be staged before, after, or alongside fixes for issues 1 and 2

### 1. Invitation revocation authorization gap

Trace the revoke flow end to end:

- `IInvitationService.RevokeInvitationAsync`
- service authorization decorator
- processor authorization decorator
- processor implementation and repository lookup

Questions to answer:

- Can **any account member** revoke invitations for the current account, or does another guard limit that in practice?
- Does the flow also permit **cross-account** revocation if an invitation ID from another account is known?
- Is the vulnerability limited to revoke, or do adjacent invitation operations share the same trust gap?

Expected output:

- A concrete description of the missing authorization boundary
- Exact exploit prerequisites
- A list of affected methods/files

### 2. Cross-account single-resource reads

Trace all single-item retrieval paths that currently enter through user-context checks rather than explicit account-context checks:

- `GetPermissionAsync`
- `GetGroupAsync`
- `GetRoleAsync`
- `GetUserAsync`

For each path, inspect:

- service authorization decorator behavior
- processor authorization decorator behavior
- processor repository query shape
- `AuthorizationProvider.IsAuthorizedAsync(...)` wildcard handling
- hydration behavior and whether it leaks additional cross-account data

Questions to answer:

- Which retrievals are truly cross-account exploitable today?
- Does exploitation require a **wildcard** permission in one account, a specific resource permission, or only membership?
- Does `GetUserAsync` materially differ because of own-user authorization?
- Are reads the only affected operations, or do update/delete flows follow the same ownership-gap pattern?

Expected output:

- A per-method exploitability matrix
- The minimum authorization state needed to exploit each path
- A clear distinction between direct data exposure and permission-metadata leakage

### 3. System-context list/query crash path

Trace account-scoped list/query APIs that allow system context through `HasAccountContext(accountId)` and verify whether lower layers still depend on `CurrentAccount`.

Initial focus:

- `UserProcessor.ListUsersAsync`
- `PermissionProcessor.ListPermissionsAsync`
- Any adjacent account-scoped list/query processors following the same pattern

Questions to answer:

- Which public service methods are reachable in system context but fail because processors still read `CurrentAccount`
- Whether the failure mode is a null dereference, other runtime exception, or inconsistent unauthorized result
- Whether the issue is limited to list operations or also affects hydration/query helpers

Expected output:

- A list of broken system-context call paths
- The precise reason each path fails
- Separation between security impact and secure-usability/reliability impact

## Deliverables

The investigation should produce:

1. A concise write-up for each issue with exact exploitability and impact
2. A file/method inventory of confirmed affected surfaces
3. A service-vs-processor authorization table classifying each auth-guarded service method as `Now`, `Proc+`, `Sig`, or `Svc`
4. A short recommendation on whether remediation should be handled as:
   - one consolidated fix
   - two tracks (auth boundary + system-context compatibility)
   - or separate issue-specific fixes

## Notes

- Keep the review high-signal and limited to implemented behavior.
- Prefer verified call chains over inferred architectural concerns.
- If an issue turns out to be narrower than initially thought, document the narrower boundary clearly rather than preserving the original broader claim.
