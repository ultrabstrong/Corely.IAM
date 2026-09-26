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

## A Content-Security-Policy set by the library

Have `Corely.IAM.Web`'s `SecurityHeadersMiddleware` set a Content-Security-Policy for the host.

**Why not.** A policy has to list every source a page loads (analytics, a CDN, Google sign-in), and
only the app knows those. A library policy is either too loose to protect anything or tight enough
to break the app. It did break this package's own pages: its policy blocked the Google sign-in
script. The middleware sets the other security headers; the host sets the Content-Security-Policy.

This was reversed once by accident: moved back into the library without the reason being
revisited. Check here before putting it back.

**Revisit if** hosts want the library to apply a policy they configure themselves. That is a
different proposal from a policy the library decides.

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
