# Design Decisions

Ideas that were considered and deliberately not built. Each one is recorded so it does not have to
be re-derived, or re-proposed, from scratch. Open ideas live in `Plans/Feature-Ideas.md`.

An entry is not permanent. If the "Revisit if" condition becomes true, the question is open again.

## Account-scoped password rules

Store password requirements in a table, one row per account, instead of in app settings.

**Why not.** A password's rules have to be known when the password is set, and at that point there
is often no account to ask:

- A user registers, and sets a password, before belonging to any account. An app that uses IAM for
  users only never has an account at all.
- A user can belong to several accounts. Account-scoped rules would give one password several sets
  of requirements - a password valid for one account could fail another, and there is no sound way
  to pick which account's rules win.

Password rules apply app-wide through `PasswordValidationOptions`.

**Revisit if** passwords ever become per-account credentials rather than one per user.

## Light database schemas

Separate contexts or migration sets that deploy only the tables a simpler app uses - no groups,
roles, or permissions for an app with users only.

**Why not.**

- The unused tables are empty and cost effectively nothing.
- The code assumes one model. Loading a user includes their groups and roles; deleting a user clears
  their account links. A trimmed schema would fail at runtime on paths the app never meant to use.
- Apps change shape. A users-only app that later adds teams starts calling account APIs with no
  schema change. Split schemas would need an upgrade path from every light variant to the full one.
- Each variant would be another migration set for every supported provider, kept in step forever.

**Revisit if** the unused tables cause a real cost - a hosting limit, a compliance review that
objects to them - rather than a tidy-looking one.

## Migrating the IAM schema when the app starts

Apply pending IAM migrations automatically during host startup.

**Why not.** Schema changes are a deployment step, not something every running instance should
attempt. It would need the app's database identity to hold DDL rights, and several instances
starting together would race to migrate. The schema is created and upgraded with the
`corely-iam-db` tool, which a pipeline or a person runs deliberately.

**Revisit if** a hosting model appears where a separate deployment step genuinely is not available.

## Default roles for invitees, and roles assigned on the invitation

Let an invitation carry roles, or let an account name a role every accepted invitee receives.

**Why not.** Users are configured once they exist in the system; an invitation is not a user yet.
Pre-assignment is a stub standing in for a user, and it opens a slope toward more of them. An app
that wants every invitee to have a role assigns it itself after acceptance, with
`IRegistrationService.RegisterRolesWithUserAsync` - a few lines, and the choice stays with the app.

**Revisit if** several consumers write the same post-acceptance assignment and ask for it.

## Result-handler helper methods

Helpers over the result types - `OnSuccess`, `Match`, and similar - so callers do not branch on
result codes by hand.

**Why not.** The pattern is sound, but `if (result.ResultCode == Success) ... else ...` is already
short, and callers can write their own helpers in whatever style they prefer. Shipping them would
be more code to maintain and would couple every caller to a library-specific style that may not age
well.

**Revisit if** callers keep writing the same helpers independently.

## Filtering and sorting on every list in the web UI

Standardize filter, sort, and paging controls across every table in `Corely.IAM.Web`.

**Why not.** Not every list warrants it. The top-level entity lists do; a group's members or a
role's permissions are short, nested lists that do not justify a server-side filtering and paging
path of their own.

**Revisit if** nested lists grow large enough in real use to need it.

## A shared busy-button component

Export a Blazor `BusyButton` from `Corely.IAM.Web` for consuming apps to reuse.

**Why not.** A busy button is not an identity concern. An auth library that exports general UI
controls because it happens to ship a Razor class library ends up owning date pickers. The
authentication pages get their busy state from a small script that stays internal to them.

**Revisit if** there is ever a shared UI library to put it in. Not in this package.

## Recorded elsewhere

- Replacing the repository layer or `MockRepo` with the EF in-memory provider, raw `DbContext`, or
  `DbSet` mocking: `DESIGN-RATIONALE.md` in the Corely.DataAccess repository.
- Adding a browser (Playwright) test tier: the testing section of `CLAUDE.md`.
