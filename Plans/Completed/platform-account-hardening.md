# Platform account hardening

**Status: done.** The fast follow to the platform account (`Corely.IAM/Docs/platform.md`). Released
stable with Corely.IAM 3.4.0, the CLI 3.1.0 and IAM.Web 3.5.0.

## Why

A platform member's permissions reach every account, so one stolen sign in reaches every customer, and
today nothing records what a platform member did outside the platform account.

## What is wanted

- **Auditing.** Planned in full in [auditing.md](../auditing.md). It covers every service, not only
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

## Progress

- **Auditing:** done on the IAM side (see [auditing.md](../auditing.md)); Billing and DocsToData adopt
  it in their own repositories.
- **Two factor for platform members:** `AuthenticationProvider.GetTokenIssueContextAsync` refuses a
  token for the platform account, or for an account entered through platform reach, unless the user
  has TOTP enabled. That one place covers sign in, MFA verification, switching and renewal, so turning
  TOTP off ends access at the next renewal. New `TwoFactorRequiredError` on `SignInResultCode`,
  `RenewAuthTokenResultCode` and the internal token codes, appended so existing values keep their
  numbers. IAM.Web's account picker says why the switch was refused. Members of ordinary accounts are
  unaffected. Integration tests in `PlatformAccountTests`; `IamScenario.EnrollTwoFactorAsync` enrolls
  a user and `ActAsAsync` answers the MFA challenge.
- **Leave on an entered account:** `UserContext.EnteredAsPlatformMember` is set where tokens are issued
  and validated, and `UserContext.MemberAccounts` leaves the entered account out. The profile page
  lists `MemberAccounts`. `AvailableAccounts` keeps the entered account, because authorization checks
  rely on it.
- **Bootstrap exit code:** fixed for every command, not only bootstrap. `CommandBase.Error` marks the
  command failed and the action returns 1; `Program.Main` returns it. `db migrate` had the same
  problem.
