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

- Owns its DI scope (see [Services and scopes](index.md#services-and-scopes)) and signs it in from the
  auth cookie before checking, so each instance checks against its own `DbContext`
- Calls `IAuthorizationProvider.IsAuthorizedAsync()` on parameter change
- Caches the result; re-evaluation only on `Action`, `Resource`, or `ResourceIds` change
- `ResourceIds` equality uses span comparison for performance

### Three states, not two

The component distinguishes *undetermined* from *denied*. It skips the check entirely while there is
no user context to check against, leaving the result uncached so the next render re-runs it. A check
with no user would be denied, which is an unknown, not a decision, and caching it would be permanent,
since the parameters never change to invalidate it.

While undetermined, `Undetermined` renders if supplied and nothing renders otherwise. `NotAuthorized`
is reserved for a real denial, so a "you cannot do this" message never flickers into a control the
user actually has.

A user with no context at all (anonymous) stays undetermined rather than resolving to denied. On
pages deriving from `AuthenticatedPageBase` this cannot be observed, since an unauthenticated visitor
is redirected to sign-in. On a public page, use `Undetermined` to render anonymous-visitor content.
