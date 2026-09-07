# Expire the Authorization Permission Cache

## Problem

`AuthorizationProvider` is registered `Scoped` and caches a user's permissions on the first check,
with no expiry. `ClearCache()` fires only on account switch, sign-out and deregistration - never
when a permission is actually granted or revoked.

That is fine for a host that creates a scope per request. MVC, Razor Pages and Web API all do, so
the cache lives for one request and a permission change takes effect on the next one.

A Blazor Server circuit is one scope for the entire browser session. Interactive component events
run on the circuit rather than through the request pipeline, so nothing refreshes the cache in
between. **A permission granted or revoked while the user is signed in is not seen until they sign
out and back in.**

The cache is also per-scope, so it could never see a change made by someone else's session anyway.
Invalidation triggered by the granting side is not an option without cross-scope coordination that
this library deliberately does not have.

Found while investigating a separate `PermissionView` bug. It is unrelated to that bug and was not
introduced by its fix.

## Approach

Two changes, independent but complementary.

### 1. Give the cache an expiry (the default that means nobody has to think about it)

Expire the cached permissions a configurable interval after they were loaded, defaulting to 30
seconds. On expiry the next check reloads from the repository.

**Absolute, not sliding.** The window runs from the moment the permissions were loaded, not from
last use. A sliding window would never expire for an active user - precisely the person who needs
the refresh.

**Configurable via `IAMOptions`**, defaulting to 30 seconds. Most hosts never touch it; a host with
a compliance reason to want 5 seconds or 5 minutes can say so without a code change.

Hosts with a scope per request are unaffected: their scopes never live long enough for the window to
matter.

### 2. Expose `ClearCache` for hosts that need immediate invalidation

`IAuthorizationCacheClearer` is `internal` today, so a host cannot clear the cache at all. A
long-lived host that knows permissions just changed - it just rendered the screen that changed them
- should be able to act on that rather than wait out the window.

Making it public turns the refresh policy into something the host owns, which is where it belongs.
The library keeps a sensible default; the host can be stricter.

## Why not other approaches

- **Change the DI lifetime.** `Transient` gives a fresh cache per injection and a database read for
  every permission check, several per page render. `Singleton` shares one cache across all users.
  Neither is viable. The lifetime is not the problem; the missing expiry is.
- **Invalidate when a permission changes.** The cache is per-scope, so the scope that grants a
  permission cannot reach the scope that holds the stale copy. This would need cross-scope
  coordination - a distributed cache or a notification bus - which is a much larger commitment than
  the problem warrants.
- **Drop the cache entirely.** A permission check happens several times per page render. Every one
  becoming a database round-trip is not acceptable.

## Risks

Low. The cache is read-through: an expiry causes a reload, never a failure, and nothing holds a
reference expecting the entry to still be present.

The one real subtlety is consistency within a single render. If a page performs several checks and
the window expires partway through, some resolve against the old snapshot and some against the new,
so two controls could briefly disagree. Permissions changing inside that window is rare enough that
designing around it is not worth the complexity - but it should be a deliberate acceptance rather
than an oversight, which is why it is written down here.

## Scope

- Add the expiry interval to `IAMOptions`, defaulted to 30 seconds.
- Track load time in `AuthorizationProvider` using the injected `TimeProvider`, so tests drive the
  clock rather than sleeping.
- Make `IAuthorizationCacheClearer` public and confirm its registration is reachable from a host.
- Unit tests: the cache is reused inside the window, reloaded after it, the window is absolute
  rather than sliding, and an explicit `ClearCache` forces a reload immediately.
- Update `Corely.IAM/Docs/security/index.md` and CLAUDE.md, which currently document the assumption
  and the absence of a host-reachable hook.

## Notes

- This does **not** address the first-render flicker on a fresh circuit. That is a different problem
  with a separate plan: `resolve-user-context-at-circuit-start.md`. The two are independent and can
  be done in either order.
- Exposing `ClearCache` is the only part that touches the library's public surface, so it needs a
  minor version rather than a patch.

## Status

Implemented in `Corely.IAM` 2.1.0.

- `SecurityOptions.PermissionCacheTtlSeconds`, defaulting to 30, applied as an absolute window from
  the load using the injected `TimeProvider`.
- `IAuthorizationCacheClearer` made public. Minor rather than patch because of it.
- Four tests: cached within the window, reloaded after it, the window is absolute rather than
  sliding (checking every second for the full window does not hold the entry alive), and
  `ClearCache` forces a reload immediately.
- Docs updated in `Corely.IAM/Docs/security/index.md`, CLAUDE.md and `MIGRATION-2.0.md`.

Two things worth recording from building it:

- The first draft of the revoke helper flipped the CRUDX flags on the permission rows, and the
  cache-hit test failed. `MockRepo` hands back the same entity instances the cache holds, so
  mutating them edited the cache in place. The helper deletes the rows instead.
- Rather than take a dependency on `Microsoft.Extensions.TimeProvider.Testing`, the unit test uses
  a small controllable `TimeProvider`, matching what `Corely.IAM.IntegrationTests` already does.
