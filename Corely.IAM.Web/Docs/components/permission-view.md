# PermissionView

Authorization gate component that conditionally renders content based on the current user's CRUDX permissions. See [Authorization UI](../authorization-ui.md) for usage patterns.

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `Action` | `AuthAction` | — | CRUDX action to check |
| `Resource` | `string` | `""` | Resource type constant |
| `ResourceIds` | `Guid[]?` | `null` | Specific resource IDs (optional) |
| `ChildContent` | `RenderFragment?` | — | Default authorized content |
| `Authorized` | `RenderFragment?` | — | Overrides `ChildContent` when authorized |
| `NotAuthorized` | `RenderFragment?` | — | Content shown when not authorized |
| `Undetermined` | `RenderFragment?` | `null` | Content shown while the answer is not yet known. Optional — nothing renders when omitted |

## Behavior

- Calls `IAuthorizationProvider.IsAuthorizedAsync()` on parameter change
- Caches result — re-evaluation only on `Action`, `Resource`, or `ResourceIds` change
- `ResourceIds` equality uses span comparison for performance

### Three states, not two

The component distinguishes *undetermined* from *denied*. It skips the check entirely while there is
no user context to check against, leaving the result uncached so the next render re-runs it.

This matters because an authenticated page awaits its user context in `OnInitializedAsync`, and that
await makes Blazor paint an interim render of its children first. A check running in that render has
nothing to authorize against and would be denied — an unknown, not a decision. Caching it would be
permanent, since the parameters never change to invalidate it, so the gated content would stay
hidden for the life of that component instance.

While undetermined, `Undetermined` renders if supplied and nothing renders otherwise. `NotAuthorized`
is reserved for a real denial, so a "you cannot do this" message never flickers into a control the
user actually has.

A user with no context at all — anonymous — stays undetermined rather than resolving to denied. On
pages deriving from `AuthenticatedPageBase` this cannot be observed, since an unauthenticated visitor
is redirected to sign-in. On a public page, use `Undetermined` to render anonymous-visitor content.
