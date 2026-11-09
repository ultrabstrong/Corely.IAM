# Platform account support group

> **Status:** built for Corely.IAM 7.3.0, both compiled models 7.3.0 and the migration CLI 7.2.0.
> Corely.Billing.IAM 6.1.0 and DocsToData declare their own support actions on top of it.

The platform owner holds full access on every type and should be signed in to rarely. Day to day
platform work (looking into an account, provisioning grants, turning SFTP on) needed a slimmer
standing role, and building one by hand meant adding a permission per type in every application.

## What it is

1. **Every resource type declares support actions** beside its owner actions:
   `ResourceTypeInfo.SupportActions`, Read by default when the owner can read, nothing otherwise.
   So the built-in types give Read on everything but `account_secret_keys` and the platform settings
   types. `IAMOptions.SetSupportActions(name, actions)` replaces the default on a registered type.
2. **The platform account has an Account Support Group** holding two system defined roles:
   - **Account Viewer Role:** Read on every type whose support actions include Read.
   - **Account Setup Role:** every other support action, per type.
3. **Bootstrap creates them, and every start completes them,** in `PlatformSupportDefaults`, run by
   `PlatformService.BootstrapPlatformAsync` and by `PlatformDefaultsStartup` (the hosted service that
   already completed the Owner role). Each role gets exactly its permissions and loses any other; a
   permission with the same actions already in the account is reused and made system defined; a role
   of the same name built by hand is adopted; the group is recreated, or given back a missing role.

## Declared by the libraries above IAM

| Package | Type | Support actions |
|---|---|---|
| Corely.Billing.IAM | `grant` | Read, Create |
| | `grant_request` | Read, Update (approving also needs `grant` Create) |
| | `billing_all_accounts` | Read (lists across accounts) |
| DocsToData | `sftp` | Read, Create (enable), Update (set password) |

## As built

1. **Not on `IPlatformService`.** The first version added `CompletePlatformSupportAsync` to the
   interface, which broke DocsToData's `PlatformBootstrapDecorator`, a host decorator implementing
   it. The work is internal instead, so the release stays additive.
2. **Tests that build a slim platform role** now reuse a permission that already exists with the same
   actions (`IamScenario.PermissionIdAsync`), since bootstrap now leaves `account: Read` and the
   other support rows in the platform account and a duplicate permission is refused.
