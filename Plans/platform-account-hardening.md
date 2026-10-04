# Platform account hardening

**Status: not started.** The fast follow to the platform account (`Corely.IAM/Docs/platform.md`).
The platform account is published only as previews (Corely.IAM 3.4.0-preview.1, IAM.Web
3.3.0-preview.1, the CLI 3.1.0-preview.1). No stable release carries it until this is done.

## Why

A platform member's permissions reach every account, so one stolen sign in reaches every customer, and
today nothing records what a platform member did outside the platform account.

## What is wanted

- **Auditing.** Planned in full in [auditing.md](auditing.md). It covers every service, not only
  platform members, and answers customer visibility: each account decides what is recorded in it,
  including platform members' actions, and reads it on the audit page.
- **Two factor sign in for every platform member.** The bootstrapped owner is enrolled; other platform
  members are not required to be. Refuse switching into another account, or signing in to the platform
  account, without it.

## Also noticed while building it

- The profile page lists `UserContext.AvailableAccounts` with a Leave action. For a platform member who
  has entered an account they don't belong to, that list includes the entered account, and Leave on it
  is refused because they are not a member. Hide Leave there, or list memberships only.
- `corely-iam-db platform bootstrap` prints a failure (an email already in use, for one) but exits 0,
  so a script running it reads success.

## Done when

Auditing is done, no platform member can act without two factor sign in, and the issues above are
fixed.
