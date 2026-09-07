# List authorization is coarse where single-record authorization is fine-grained

## Status

**Unresolved, and not yet agreed.** The owner's position is that the decorator pattern plus custom
resource types already covers this and that no new rails are needed. That position is right about
every operation except one shape, and the disagreement is narrow enough to settle by looking at the
code. Written down so it is not lost.

## The observation

`IAuthorizationProvider` exposes one method:

```csharp
Task<bool> IsAuthorizedAsync(AuthAction action, string resourceType, params Guid[] resourceIds);
```

It answers yes/no about resources the caller already holds. Decorators wrap a call and inspect its
arguments, which works whenever the operation names a resource:

- `GroupProcessorAuthorizationDecorator.GetGroupByIdAsync` passes the id
- DocsToData's `DocumentWorkflowTemplateAuthorizationDecorator` passes `workflowTemplateId`,
  `request.Id`, `request.TemplateId`, `stepTemplateId`

A list names no resource. The rows do not exist until the inner call returns, so the decorator has
nothing to inspect and falls back to a type-level check.

Both codebases land in the same place, independently:

| | Single record | List |
|---|---|---|
| `Corely.IAM` | `GetGroupByIdAsync` scopes by id | `ListGroupsAsync` checks the type, returns everything |
| DocsToData | four decorators pass ids | `ListWorkflowTemplatesAsync`, `GetAllGrantsAsync` pass none |

Of the 62 authorization calls in DocsToData, the list methods are the only ones that pass no
resource id.

## What actually breaks

Narrower than "lists are unauthorized", and worth stating precisely because the wider claim is wrong:

- **Filtering is achievable today.** Fetch rows, call `IsAuthorizedAsync` per row. Permissions are
  cached in memory, so this costs no extra queries.
- **Correct paging is not.** Ask for 50, filter afterwards, get 43. `TotalCount` counts rows the
  caller cannot see, so the page count is wrong and the last pages may be empty.

`ListQueryHelper.ExecuteListAsync` builds one predicate and uses it for both the page and
`CountAsync`, so a filter applied there stays consistent. A filter applied after the call cannot.

## Evidence

`Corely.IAM.UnitTests/Groups/Processors/GroupListAuthorizationScopeTests.cs`, committed with
`Skip` so CI stays green. Remove the `Skip` to see the current behaviour:

| Test | Today |
|---|---|
| `ListGroups_ReturnsOnlyPermittedGroups_WhenPermissionIsPerResource` | **fails** - returns the forbidden group too |
| `ListGroups_TotalCountReflectsOnlyPermittedGroups` | **fails** - reports 3 when the caller may see 1 |
| `ListGroups_ReturnsEverything_WhenPermissionIsWildcard` | passes |
| `ListGroups_ReturnsUnauthorized_WhenNoReadPermissionAtAll` | passes |

The two that pass are there to constrain a fix, not to demonstrate the defect.

## Is it live or latent?

**Unconfirmed, and it decides the priority.** No code was found in DocsToData that issues a
per-resource grant - only code prepared for them, passing ids on single-record operations. If every
grant issued in practice is a wildcard (`ResourceId == Guid.Empty`), nothing is broken today and
this is a trap armed for the first per-resource grant rather than a live defect.

**Answer this before doing any work.** Check what `ResourceId` values exist in the permissions table
of a real database.

## Options, if it is worth closing

1. **Filter inside `ExecuteListAsync`.** Every IAM list already routes through it, and it owns both
   the page predicate and the count, so paging stays correct. Needs the permitted-id set, which
   means a lookup on `AuthorizationProvider` - internal is enough for IAM's own lists.
2. **Also expose that lookup publicly.** Only matters for custom resource types, where the rows live
   in the consumer's own tables and IAM cannot reach the query. Without it, a consumer registering a
   custom type can filter per row but cannot page correctly.
3. **Do nothing, and document it.** State that per-resource permissions apply to single-record
   operations and that lists are type-level. Costs nothing and leaves the sharp edge in the API.

Option 1 is the smallest change that removes the inconsistency inside the library. Option 2 is only
worth it if custom resource types are meant to be first-class.

## Notes

- The decorator pattern is not the problem and does not need replacing. It has one blind spot, and
  it is structural rather than a mistake in how it was applied.
- Found while assessing the libraries from an adopter's point of view, not from a bug report.
