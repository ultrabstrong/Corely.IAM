# List authorization is coarse where single-record authorization is fine-grained

## Status

**Agreed in principle, not yet built.** Direction settled: push the permitted resource ids into the
list query rather than filtering after it, and document the scaling limit. Details in
"Agreed approach" below.

One question is still open and decides the priority - see "Is it live or latent?".

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

## Agreed approach

Filter inside `ListQueryHelper.ExecuteListAsync` by passing the permitted resource ids into the
query as a `WHERE Id IN (...)` clause. Every IAM list already routes through that helper, and it
owns both the page predicate and the count.

### Why this fixes paging rather than breaking it

The initial worry was that materialising ids would wreck ordering and paging. It is the reverse.
With the ids in the query, the database still does the ordering, the skip/take and the count, so
the page is correct. Paging is broken *today*, by filtering after the rows come back - ask for 50,
drop 7, and both the page size and the total are wrong.

### There is no extra database call

`AuthorizationProvider` already loads a user's permission rows into memory on the first check and
caches them. The id set is derived from data that is already in hand.

### Where it does not scale, and what to say about it

A large `IN (...)` clause is the real limit. SQL Server caps at roughly 2,100 parameters and
degrades before that. Two things push the ceiling further out than it first appears:

- **Wildcard grants add no clause at all.** `ResourceId == Guid.Empty` means every resource of the
  type, so the query is unfiltered.
- **Rows are per-role, not per-user.** Fifty documents shared with thirty people through one role
  is fifty permission rows, not fifteen hundred.

So the documented guidance is that many individual per-resource grants do not scale, and that a
wildcard grant is the alternative - **with the caveat that this is a permissions decision, not a
performance setting.** A wildcard grants access to everything of that type. If the individual
grants were standing in for "this principal should see all of these", the wildcard was always the
right model. If the restriction is real - this user sees only these fifty documents - the wildcard
is not available and the guidance does not apply. Word it so nobody widens access to fix a slow
query.

### Custom resource types

For a consumer's own tables the rows live in a different `DbContext`, and EF cannot join across two
contexts in one query even when they share a physical database. So closing the gap there means the
id set crosses the boundary and the consumer applies it to their own query. That is the only part
that needs anything public; IAM's own lists need nothing new, since permissions and groups already
share `IamDbContext`.

### Likelihood, by resource type

- **IAM's own types** (groups, roles, users): low. Nobody hand-grants fifty individual groups.
- **Consumer types**: plausible. Document sharing is exactly the "these specific records" case, and
  DocsToData is a document product.

## Notes

- The decorator pattern is not the problem and does not need replacing. It has one blind spot, and
  it is structural rather than a mistake in how it was applied.
- Found while assessing the libraries from an adopter's point of view, not from a bug report.
