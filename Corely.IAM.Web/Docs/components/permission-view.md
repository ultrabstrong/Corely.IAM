# PermissionView

Authorization gate component that conditionally renders content based on the current user's CRUDX permissions. See [Authorization UI](../authorization-ui.md) for usage patterns.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Action` | `AuthAction` | none | CRUDX action to check |
| `Resource` | `string` | `""` | Resource type constant |
| `ResourceIds` | `Guid[]?` | `null` | Specific resource IDs (optional) |
| `ChildContent` | `RenderFragment?` | none | Default authorized content |
| `Authorized` | `RenderFragment?` | none | Overrides `ChildContent` when authorized |
| `NotAuthorized` | `RenderFragment?` | none | Content shown when not authorized |
| `Undetermined` | `RenderFragment?` | `null` | Content shown while the answer is not yet known. Optional; nothing renders when omitted |

## Behavior

- Checks in an operation of its own (see [Services and scopes](index.md#services-and-scopes)), so each
  check is signed in and reads permissions as they are now
- Calls `IAuthorizationProvider.IsAuthorizedAsync()` on parameter change
- Caches the result; a parent's re-render opens no operation, and only a change of `Action`,
  `Resource` or `ResourceIds` checks again
- `ResourceIds` equality uses span comparison for performance

### Three states, not two

The component distinguishes *undetermined* from *denied*. It is undetermined while the check runs,
and when there is no user to check for, in which case it caches nothing. A check with no user would be
denied, which is an unknown, not a decision.

While undetermined, `Undetermined` renders if supplied and nothing renders otherwise. `NotAuthorized`
is reserved for a real denial, so a "you cannot do this" message never flickers into a control the
user actually has.

A visitor with no session (anonymous) stays undetermined rather than resolving to denied. On
pages deriving from `AuthenticatedPageBase` this cannot be observed, since an unauthenticated visitor
is redirected to sign-in. On a public page, use `Undetermined` to render anonymous-visitor content.
