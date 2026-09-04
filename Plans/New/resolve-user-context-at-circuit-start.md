# Resolve the User Context When the Circuit Opens

## Problem

A Blazor Server circuit is a different DI scope from the HTTP request that served the page.
`AuthenticationTokenMiddleware` reads the auth cookie and sets the user context on every HTTP
request, but with `prerender: false` components render over the WebSocket, in the circuit's scope,
where that work is not visible. The circuit's first act is therefore to redo it:
`AuthenticatedPageBase.OnInitializedAsync` awaits `BlazorUserContextAccessor.GetUserContextAsync()`,
which re-reads the cookie and calls `AuthenticateWithTokenAsync`.

That await yields, so Blazor paints an interim render of the page's children before any context
exists. Components that depend on the context observe a half-initialized scope.

This already produced one shipped bug: `PermissionView` ran its authorization check during that
interim render, was denied for want of a context, cached the denial, and never re-checked, so gated
controls stayed hidden for the life of the component. Fixed in `Corely.IAM.Web` 2.0.1 by teaching
`PermissionView` to treat "no context" as undetermined rather than denied.

That fix is correct on its own terms - a denial derived from a missing context is not a decision,
and the component should say so. But it handles the symptom at one call site. Any future component
that reads the user context during its first render has the same exposure and will have to solve it
again.

## Proposal

Resolve the user context in a `CircuitHandler`'s `OnCircuitOpenedAsync`, before any component
renders. Every component would then see a populated context on its first render, on every page,
and no interim render could observe a half-initialized scope.

There is no `CircuitHandler` in the codebase today.

## What this does and does not buy

**Removes:** an entire class of ordering bug, not one instance of it. Components stop needing to
defend against a context that is not there yet.

**Does not remove:** the wait. `AuthenticateWithTokenAsync` validates the JWT and checks the tracked
token against the database - that is where revocation lives, so it is real I/O and cannot be made
free. The work moves from "one gated control appears late" to "the circuit connects a beat later",
paid once, somewhere users already expect a connection delay.

**Scale of the current cost, for calibration:** the resolve happens once per circuit, not once per
page. After it, the circuit-scoped provider holds the context and every later check takes the
synchronous fast path. So today's flicker is one control, on the first authenticated page, once per
browser session. This is an architectural cleanup, not a performance fix.

## Open questions

These are the reasons this is a plan rather than a change.

1. **Is the auth cookie reachable from `OnCircuitOpenedAsync`?** `BlazorUserContextAccessor` reads
   it through `IHttpContextAccessor`, which works today from within a render. Whether `HttpContext`
   is still available at circuit-open time, and whether relying on it there is sound, needs
   verifying rather than assuming. If it is not, the alternative is capturing what the circuit needs
   during the initial HTTP request and handing it across - a larger change.
2. **What happens when the token is invalid or expired at circuit start?** Today the redirect to
   sign-in is driven by `AuthenticatedPageBase` finding no authenticated context. Moving resolution
   earlier means deciding where that redirect now belongs, and what a circuit with a failed
   authentication should do.
3. **Does this interact with renewal?** `AuthenticationTokenMiddleware` attempts token renewal
   before clearing auth cookies. A circuit that authenticates independently needs to not fight with
   that, and a long-lived circuit outliving its access token needs a defined story.
4. **Does `PermissionView`'s undetermined state stay?** It should - the invariant holds regardless
   of when the context arrives, and it is what makes anonymous rendering expressible. This work
   would make it unobservable in practice on authenticated pages, not unnecessary.
5. **Is the circuit-open delay acceptable?** It is a blocking database round-trip before anything
   renders. Worth measuring against the current behavior before committing.

## Notes

- Deliberately not started. Raised while fixing the `PermissionView` bug; recorded so the reasoning
  is not lost, and so the next person does not rediscover the middleware/circuit scope split from
  scratch.
- The `PermissionView` fix is independent and already shipped. This work does not depend on it and
  does not supersede it.
