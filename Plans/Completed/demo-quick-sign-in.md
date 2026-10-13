# Demo quick sign in

**Status: done.** Released in Corely.IAM.Web 4.0.0; the empty `_VerifyMfaAssist` slot for the
platform owner's code button ships in Corely.IAM.Web 4.1.0.

## As built

Where the build differs from the plan below:

- **The bootstrap runs in `Start-LocalStack.ps1`,** which has the provider, connection, system key
  and settings path. It passes the secrets file to the seed with `-PlatformOwnerSecretsFile`; the
  seed does steps 2 to 5 and skips them without it. It bootstraps with `--settings` and the WebApp's
  `appsettings.json`.
- **`auth signin` prints the MFA challenge token** when MFA is required. It printed only the result
  code, so step 3 could not be scripted.
- **The optional code on `password register`, `password modify` and `deregister basic-auth` was
  required:** the attribute's one argument constructor defaults to required. Fixed with `false`.
- **The library renders the slot with `Html.PartialAsync`.** `Corely.IAM.Web` has no
  `_ViewImports` registering tag helpers, so `<partial>` came out as a literal tag.
- **The WebApp template no longer carries the moved `SecurityOptions` and
  `PasswordValidationOptions` keys.** A seeded WebApp has a platform account, so they would log the
  warning; without one the defaults are the same values.
- **Descriptions were checked against the seed** and four corrected: farah.khan, julia.woods,
  quinn.hughes and sara.kim had roles the plan left out.
- The functional tests cover the WebApp in Development and Production and both demo hosts in
  Development. The dropdown was checked by hand: picking platform-owner signs in to account
  selection.
- **platform-owner keeps its TOTP.** Turning it off locked the owner out of every account, the
  platform account included, since a platform member needs two factor to enter one. The seed now
  passes a code to `password modify` instead, `Start-LocalStack.ps1` saves the secret to
  `DemoFeatures:PlatformOwnerTotpSecret`, and the WebApp's `_VerifyMfaAssist.cshtml` (an empty slot
  in `Corely.IAM.Web`, like `_SignInAssist`) shows a Development only button entering the current
  code.

## What

A "Demo only: quick sign in" dropdown above the username and password on the sign in page of each demo
host. Each entry is a seeded username plus a short description of what that user has when the seed
finishes. Picking one fills the username and `Test1234` and submits.

It is static. It describes the seeded state and does not follow changes made while someone uses the
demo.

Hosts: `Corely.IAM.WebApp`, `Corely.IAM.Demos.UsersOnly`, `Corely.IAM.Demos.SharedAccount`. All three
use `Corely.IAM.Web`'s sign in page.

## How

**The slot.** `Corely.IAM.Web/Pages/Authentication/SignIn.cshtml` renders a partial,
`_SignInAssist`, above the form. `Corely.IAM.Web` ships that partial empty, so a real host renders
nothing and needs no change. This is the only change to a published package, and it is not a
feature: a Razor class library page partial is overridden by a file at the same path in the host.

Alternative with no package change at all: each demo host overrides the whole `SignIn.cshtml` at the
same path. That copies the page three times and the copies drift from the library's. Recommended:
the empty partial.

**The helper.** Each demo host adds `Pages/Authentication/_SignInAssist.cshtml`:

- A bordered box labelled "Demo only: quick sign in", visibly apart from the real form, with one
  line saying every demo user's password is `Test1234`.
- A `<select>` of the host's users, grouped with `<optgroup>` where the list is long. Option text is
  `username: description`.
- A few lines of inline script: on change, set `#username` and `#password` and submit the form.
- Rendered only when `IHostEnvironment.IsDevelopment()`, so a demo host run in Production shows the
  plain page.

The list lives in the partial as literal markup. No model, no service, no lookup.

## Platform owner in the WebApp seed

`SeedWebAppDemo.ps1` does not create a platform account today. Add a step at the end:

1. `corely-iam-db platform bootstrap --email platform-owner@demo.local --secrets <seed dir>` creates
   the platform account and its owner, with a random password and TOTP enabled.
2. Read the password and authenticator secret from the secrets file.
3. Sign in with the dev tools (`auth signin`, then `auth verify-mfa` with a code from
   `totp generate-code`).
4. `totp disable <code>`.
5. `password modify <generated> Test1234`. No code needed now that TOTP is off.

The library keeps requiring TOTP for a new platform owner. Only the demo seed removes it.

A database seeded before this change has no platform owner. `Start-LocalStack.ps1 -Reset` recreates
it.

## The lists

### Corely.IAM.WebApp

From `SeedWebAppDemo.ps1`. Every member of Acme NA is in its All Hands group, which gives Read Only
Analyst; descriptions mention it only where it is all the user has.

| Group | User | Description |
|---|---|---|
| Platform | platform-owner | Platform owner: full access everywhere, can switch into any account |
| Start here | admin | No accounts yet: create one |
| Account owners | alice.johnson | Owns Acme NA; Account Admin in the other four |
| | ben.carter | Owns Acme Europe; also an owner in Acme NA |
| | cara.nguyen | Owns Contoso Manufacturing |
| | daniel.lee | Owns Fabrikam Professional Services |
| | elena.morris | Owns Northwind Internal IT |
| Admins | farah.khan | Owner and Account Admin in Acme NA; Account Admin in Acme Europe |
| | gavin.brooks | Account Admin in Acme NA; Support Lead in Acme Europe |
| | hannah.price | Account Admin in Acme NA; Support Lead in Acme Europe |
| | isaac.reed | Owner in Acme Europe; Support Agent in Acme NA |
| | julia.woods | Account Admin in Contoso; Analyst and Security Auditor in Acme NA |
| | kevin.bell | Account Admin in Contoso |
| Members | leo.turner | Fulfillment Lead in Acme NA; Plant Supervisor in Contoso |
| | maya.shah | Operations Manager in Acme NA; Plant Supervisor in Contoso |
| | nolan.gray | Project Lead in Fabrikam |
| | olivia.ward | Fulfillment Lead in Acme NA; Project Lead in Fabrikam |
| | paula.ramos | Billing Manager in Acme NA; read only in Fabrikam |
| | quinn.hughes | Analyst in Acme NA; Helpdesk in Northwind |
| | rafael.gomez | Helpdesk in Northwind |
| | sara.kim | Analyst in Acme NA; Finance Reviewer in Acme Europe |
| | tyler.adams | Support Agent in Acme NA; Support Lead in Acme Europe |
| | uma.patel | Plant Supervisor in Contoso |
| | victor.ross | Operations Manager in Acme NA; QA Reviewer in Contoso |
| | willa.ford | Billing Manager in Acme NA |
| | xavier.young | Read only in Acme NA |
| | yasmine.ali | Read only in Acme NA |
| | zane.cooper | Fulfillment Lead in Acme NA; Project Lead in Fabrikam |
| Contractors | seasonal.01 to seasonal.04 | Contractor Limited in Acme NA: read accounts, users and groups |

Check each line against the seed while writing the partial; the seed is the source of truth.

### Corely.IAM.Demos.UsersOnly

| User | Description |
|---|---|
| alice | Has a few notes |
| marcus | Has a few notes |

### Corely.IAM.Demos.SharedAccount

| User | Description |
|---|---|
| olivia | Owns team Acme: sees members, can invite |
| bobby | Member of Acme with no roles |
| carla | Member of Acme with no roles |
| dana.solo | No team: create one or join with a token |

## Tests

Functional tier, in `Corely.IAM.Web.FunctionalTests` (the demo hosts are already smoke tested there in
`Demos`):

- Each demo host's sign in page contains the quick sign in box in Development.
- The WebApp's sign in page does not contain it outside Development.

What picking an entry does is browser behavior and is checked by hand, not with a new E2E tier.

## Docs

- Each demo README and the Running the WebApp Locally section of `CLAUDE.md`: one line on the
  dropdown, and that `platform-owner` / `Test1234` is seeded.
- `Corely.IAM.Web/Docs`: none. The empty partial is not something a host is meant to use.

## Releases

Corely.IAM.Web gets the empty partial. It rides along with the 4.0.0 release from
`password-register-and-modify.md`; no release of its own.
