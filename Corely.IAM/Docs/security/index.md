# Security

Corely.IAM security is built on `Corely.Security` primitives with no external service dependencies.

## Key Principles

- **System key provisioning** — the system encryption key is supplied by the host via `ISecurityConfigurationProvider`, never stored in code
- **Encryption at rest** — all stored keys (account and user key pairs) are encrypted using the system key
- **No secrets in code** — no hardcoded keys, connection strings, or sensitive values
- **Pluggable algorithms** — crypto algorithms configurable via `IAMOptions` builder

## Authorization caching

`AuthorizationProvider` is registered `Scoped` and caches a user's permissions on first check. The
cache expires `SecurityOptions.PermissionCacheTtlSeconds` after the load, defaulting to 30 seconds.

The window is **absolute, not sliding** - measured from the load rather than from last use, so an
active user still gets a refresh. A sliding window would never expire for exactly the person who
needs one.

A host that creates a scope per request - MVC, Razor Pages, Web API - never reaches the expiry; its
scopes do not live that long, and a permission change takes effect on the next request as before.

A host with a long-lived scope depends on it. A Blazor Server circuit is a single scope for the
whole browser session, and interactive component events run on the circuit rather than through the
request pipeline, so nothing else would refresh it. Without the expiry, a permission granted or
revoked by someone else would not be seen until the user signed out.

For immediate invalidation, inject `IAuthorizationCacheClearer` and call `ClearCache()`. A host that
knows permissions just changed - it rendered the screen that changed them - should not have to wait
out the window. The library also clears the cache itself on account switch, sign-out and
deregistration.

Note that the cache is per-scope, so one scope cannot invalidate another's copy. The expiry, not
invalidation, is what bounds staleness for changes made elsewhere.

## Topics

- [Key Management](key-management.md) — system keys, account keys, user keys, encryption providers
- [User Context](user-context.md) — `UserContext`, `IUserContextProvider`, host-agnostic auth
