# Platform account hardening

**Status: not started.** The fast follow to the platform account (`Corely.IAM/Docs/platform.md`).
No IAM package carrying the platform account is published until this is done.

## Why

A platform member's permissions reach every account, so one stolen sign-in reaches every customer, and
today nothing records what a platform member did outside the platform account.

## What is wanted

- **Audit log.** Every action a platform member takes in an account they are not a member of is
  recorded: who, which account, what, when. Where it is stored and how it is read are open.
- **Two factor sign in for every platform member.** The bootstrapped owner is enrolled; other platform
  members are not required to be. Refuse switching into another account, or signing in to the platform
  account, without it.
- **Customer visibility.** Decide whether a customer account can see that a platform member acted in
  it, and where (an activity list, a notice on the affected record).

## Also noticed while building it

- The profile page lists `UserContext.AvailableAccounts` with a Leave action. For a platform member who
  has entered an account they don't belong to, that list includes the entered account, and Leave on it
  is refused because they are not a member. Hide Leave there, or list memberships only.

## Done when

Every action outside the platform account by a platform member is in the audit log, no platform member
can act without two factor sign in, and the customer visibility question is answered and built.
