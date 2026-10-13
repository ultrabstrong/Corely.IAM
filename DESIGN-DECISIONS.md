# Design Decisions

Ideas that were dropped but are easy to remember as done. Open ideas live in
`Plans/Feature-Ideas.md`.

## Account-scoped password rules

Store password requirements in a table, one row per account, instead of in app settings.

**Why not.** A password's rules have to be known when the password is set, and at that point there
is often no account to ask:

- A user registers, and sets a password, before belonging to any account. An app that uses IAM for
  users only never has an account at all.
- A user can belong to several accounts. Account-scoped rules would give one password several sets
  of requirements: a password valid for one account could fail another, and there is no sound way
  to pick which account's rules win.

Password rules apply app-wide through `PasswordValidationOptions`.

**Revisit if** passwords ever become per-account credentials rather than one per user.

## Security headers set by the library

Have `Corely.IAM.Web` set response security headers for the host: a Content-Security-Policy,
`X-Frame-Options`, `Permissions-Policy`, HSTS, caching and the rest.

**Why not.** Security headers are an application concern. Each one encodes a choice only the app can
make: the sources its pages load, whether it embeds its own pages, which browser features it uses,
how it caches its files. Set inside `UseIAMWebAuthentication()`, the library's choice was forced on
every host, which could neither opt out without losing authentication nor override a header without
undocumented middleware after it. It broke hosts twice:

- The library's Content-Security-Policy blocked this package's own Google sign-in script.
- Its `Cache-Control: public, max-age=86400` on every static file left browsers showing stale CSS
  and scripts for a day, and contradicted the cache headers `MapStaticAssets` sets.

Each host sets its own headers, starting from the set in `Corely.IAM.Web/Docs/security.md`. The
duplication across hosts is accepted.

The Content-Security-Policy was moved back into the library once by accident, without the reason
being revisited. Check here before putting any header back.

**Revisit if** hosts want the library to apply headers they configure themselves. That is a
different proposal from headers the library decides.

## Distinct names for result types shared with other Corely libraries

Corely.Billing defines `RetrieveResultCode`, `ModifyResultCode`, `ModifyResult`, `PagedResult<T>`,
`RetrieveSingleResult<T>` and `RetrieveListResult<T>`, the same names as `Corely.IAM.Models`. Rename
one library's set (`BillingRetrieveResultCode`), give both a library prefix, or move them into
Corely.Common, so a file importing both needs no alias.

**Why not.**

- **A prefix on only the colliding types leaks.** The prefix would record a fact about a sibling
  library, not about the type, and would sit beside unprefixed domain types (`Grant`, `User`). Each
  new library would rerun the rule against whatever it happens to collide with.
- **The types that collide are the generic ones, and that is expected.** Domain types never collide,
  because their names are specific. Generic plumbing does, which is what namespaces are for.
- **Sharing them from Corely.Common couples the libraries through their most frequently changed
  types.** The two sets have already diverged: IAM's `ModifyResultCode` has codes Billing's lacks,
  and IAM's `RetrieveSingleResult<T>` carries effective permissions. Each library needs to change
  its own without a release of the other.
- **Naming by operation (`ListGrantsResult`) multiplies types** to fix a collision seen in one file.

A file that imports both namespaces uses an alias, e.g.
`using BillingRetrieveResultCode = Corely.Billing.Models.RetrieveResultCode;`.

**Revisit if** the collision spreads well beyond a few host files, or a third library makes aliasing
routine.
