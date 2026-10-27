# API keys

Status: released in `Corely.IAM` 6.0.0, `Corely.IAM.Web` 8.0.0 and `Corely.IAM.DataAccessMigrations.Cli` 6.0.0.

## What

An API key is a principal of its own, alongside users, that belongs to one account. It signs in by
exchanging its secret for the same access and refresh token pair a user gets (see
[session-hardening.md](session-hardening.md)). From then on roles, groups, permissions, the grant
bounds, list scoping and auditing work as they do for a user, and the authorization decorators never
learn that the caller is a key.

**The API is the host's.** IAM ships the key, its management and the exchange
(`SignInWithApiKeyAsync`), and no endpoint that accepts a key. A host that has an API calls the
exchange from it and validates the returned access token as it does a user's. A host without one
leaves API keys switched off, which is the default, and sees nothing of them.

A new key holds no permissions. It can do nothing until someone assigns it a role or adds it to a
group. Nothing assigns one automatically.

## Identity

Keys get their own entity and tables, built the way users already are. Each user piece has a key
counterpart:

| User | API key |
|------|---------|
| `UserEntity` | `ApiKeyEntity`: `Id`, `AccountId`, `Name`, `Description`, the login metric and lockout fields, `CreatedUtc`, `ModifiedUtc` |
| `UserAsymmetricKeyEntity` | `ApiKeyAsymmetricKeyEntity`: the key's token signing key, encrypted with the system key |
| `UserAuthTokenEntity` | `ApiKeyAuthTokenEntity`: the same columns, including the refresh token columns from session hardening, plus `Slot` |
| `UserRole`, `UserGroup` | `ApiKeyRole`, `ApiKeyGroup`, with `DeleteBehavior.NoAction` |
| `UserAccount` | none: `ApiKeyEntity.AccountId`, since a key has exactly one account |
| `User` model, mapper, validator | `ApiKey` model, mapper, validator |

`Name` is unique within the account. Deleting a role or group clears its `ApiKeys` collection the way
it clears `Users`; deleting an account deletes its keys.

## Secrets

Each key has two slots, primary and secondary, both always valid. Rotating one leaves the other alone,
so a consumer moves to the other slot first and nothing breaks.

```
ciam_<prefix>_<secret>
```

- `prefix` is 8 random base32 characters, unique across all keys, used to find the row.
- `secret` is 32 random bytes, base64url encoded. Only its SHA-256 hash is stored, compared in
  constant time. The full key is returned once, on creation or rotation.
- `ApiKeySecretEntity`: `ApiKeyId`, `Slot`, `Prefix`, `SecretHash`, `CreatedUtc`, `ExpiresUtc`,
  `LastUsedUtc`. At most one row per slot.
- `ExpiresUtc` is set per slot, bounded by the maximum lifetime setting.
- `LastUsedUtc` per slot answers "is anything still on the primary?" before rotating it. It is written
  on exchange only, never per request.

## Exchange and tokens

```csharp
var result = await authenticationService.SignInWithApiKeyAsync(
    new SignInWithApiKeyRequest(apiKey, deviceId));
```

- Returns access and refresh tokens with the key's account as the signed in account. Renewal is the
  ordinary refresh flow, against `ApiKeyAuthTokenEntity`.
- The JWT's `sub` is the key id and a `principal_type` claim says `api_key`. Validation reads the claim
  to pick the table and signing key.
- A wrong secret for a known prefix counts as a failed sign in, so lockout applies. Every failure
  (unknown prefix, wrong secret, expired slot, feature off, locked) returns `InvalidApiKeyError`
  without saying which.
- Rotating a slot revokes the sessions exchanged from that slot only.
- `DeleteExpiredAuthTokensAsync` from session hardening cleans both token tables.

## Context and authorization

- `UserContext` gains `ApiKey? ApiKey`. Exactly one of `User`, `ApiKey` and `IsSystemContext` is set.
  The type keeps its name; renaming it across the public surface is not worth the break.
- `HasUserContext()` and `HasAccountContext()` pass for a key, so service decorators let it through as
  they do a user.
- `IsNonSystemUserContext()` and `IsAuthorizedForOwnUser()` are false for a key. The operations they
  gate (password, MFA, Google, deregister self) are a user's own credentials, which a key does not
  have.
- `AuthorizationProvider` loads a key's permissions through `r.ApiKeys` and `g.ApiKeys`, a branch
  beside the user one, then applies the platform's resource type filter below. Everything downstream
  of the loaded set (`IsAuthorizedAsync`, list scoping, the `CanGrant*` checks) is unchanged, so a key
  can grant only what it effectively holds.
- `AuditEntryEntity` gains `ActorApiKeyId`. The audit page shows the key's name as the actor.

## Management

Following the existing service split, each call authorized against the new `api_key` resource type:

| Service | Method |
|---------|--------|
| `IRegistrationService` | `RegisterApiKeyAsync`: creates the key in the current account, returns the primary secret |
| `IRegistrationService` | `RegisterRolesWithApiKeyAsync`, `RegisterApiKeysWithGroupAsync` |
| `IModificationService` | `RotateApiKeySecretAsync(apiKeyId, slot, expiresUtc)`: replaces one slot, returns the new secret |
| `IModificationService` | `ModifyApiKeyAsync(apiKeyId, name, description)` |
| `IDeregistrationService` | `DeregisterApiKeyAsync`, `DeregisterRolesFromApiKeyAsync`, `DeregisterApiKeysFromGroupAsync` |
| `IRetrievalService` | `ListApiKeysAsync`, `GetApiKeyAsync`: names, slots, expiries, last used, roles, groups; never a secret |

Assigning a role to a key or adding a key to a group goes through new `CanGrant*` checks matching the
user ones: the caller must hold everything handed out.

`ApiKey.RESOURCE_TYPE = "api_key"` is registered like the other IAM types and the Owner role gets all
five actions on it. A key holding `api_key` permissions can manage keys, including itself.

## Platform settings

New fields on `SecurityPolicy` and its entity, with validator rules, `ChangesTo` entries and
`SecurityOptions` configuration keys, as in [session-hardening.md](session-hardening.md#settings).

| Setting | Default | Weakens when | Effect |
|---------|---------|--------------|--------|
| API keys enabled | off | turned on | The whole feature. Off refuses creating keys, exchanging them and renewing their sessions, and hides the API keys page, its nav link, keys on the role and group pages, and the other three settings below. Access tokens already issued run out within the access token lifetime |
| Platform account API keys | off | turned on | Allows keys in the platform account. Platform membership otherwise requires TOTP, which a key cannot have |
| API key maximum lifetime | 365 days, 1 to 3650 | raised | Upper bound on a slot's `ExpiresUtc` |
| API key resource types | none | a type added | The resource types a key's permissions may cover, IAM's and the host's alike |

**Resource types for keys.** `SecurityPolicy.ApiKeyResourceTypes` is a set of type names, stored in
its own table under the platform settings row (`PlatformApiKeyResourceTypeEntity`) and configured as
`SecurityOptions:ApiKeyResourceTypes` without a platform account. When a key's permissions load, any
permission on a type outside the set is dropped. Turning a type off takes effect within the permission
cache window with no change to roles; turning it back on restores what the roles already grant.

- Saving refuses a type that is not registered. A saved type the host later stops registering is
  ignored.
- A host type registered after the setting was saved starts off, like every type.
- `ChangesTo` reports one change per type added (weakens) or removed.
- The security settings page lists every registered type, IAM's and the host's in two groups, each
  with a switch.
- The `ApiKeys` component marks a key's permissions on a type that is switched off, so a role that
  seems to grant something but does not is visible.

Turning on a setting that weakens the policy already shows a warning on the page, so all four get it
with no new UI.

## Corely.IAM.Web

- An `ApiKeys` component for the current account: list (a `table-cards` table), create, rotate a slot,
  edit, delete, and role and group assignment. The new secret is shown once, with a copy button.
- A shell for it in `Corely.IAM.WebApp`, linked through `IIamWebLinks`.
- Role and group pages list keys beside users where they list members.
- The resource type switches on the security settings page.

## Tests

| Tier | Cases |
|------|-------|
| Unit | Key format and parsing; constant time compare; exchange success and each failure returning the same code; lockout; rotation revokes only its slot's sessions; a new key has no permissions; permissions on a switched off type are dropped; grant bounds on assigning roles and groups to a key; context checks (`HasUserContext` true, `IsNonSystemUserContext` false); settings ranges, unknown type refused, `ChangesTo` per type |
| Integration | Prefix uniqueness; permission loading through a key's roles and groups; deleting a role, group, key or account removes key join rows on both providers (no cascade on SQL Server); migration |
| Functional | A key exchanges, calls an authorized endpoint and renews; an audit entry names the key as actor |

## Release

Ships with [session-hardening.md](session-hardening.md) in `Corely.IAM` 6.0.0, `Corely.IAM.Web` 8.0.0
and `Corely.IAM.DataAccessMigrations.Cli` 6.0.0. Migration through `.\AddMigration.ps1 "ApiKeys"`.

## As built

| Planned | Built |
|---------|-------|
| A signing key per API key | Key tokens are signed with the account's key and carry `principal_type` = `api_key` |
| Allowed resource types as one switch per type | One `ResourceTypeSet` setting, stored as a comma separated column; a type not registered is refused on save |
| `ApiKeyAuthTokenEntity`, a token table per principal | One `AuthTokens` table (`UserAuthTokens` renamed) holds user and key sessions: `UserId` or `ApiKeyId`, plus `Slot`. `SessionTokenProvider` is the only code that builds, signs, validates, renews, revokes and cleans tokens; `AuthenticationProvider` and `ApiKeyAuthenticationProvider` keep only their own credential checks |
| Functional: a key exchanges, calls an endpoint and renews | `BearerApiTests`, 9 cases, against the WebApp's sample API |
| The host writes its own API transport | `Corely.IAM.Web` ships it, opt in: `AcceptBearerTokens`, `MapIAMTokenEndpoints` (key exchange, renew, sign out) and `AsIAMApi`. Users sign in through the pages only. The WebApp serves `/api/me`, `/api/roles` and an OpenAPI document at `/openapi/v1.json` |
| Functional: an audit entry names the key | Unit (`AuditProviderTests.Record_NamesTheApiKey_AsTheActorInItsAccount`) |
| | Keys cannot accept invitations, read the audit log, switch account or see effective permissions |
| | `corely-iam-devtool auth apikey <key>` exchanges a key for local testing |
| Group membership on the key | `RegisterApiKeysWithGroupAsync` and `DeregisterApiKeysFromGroupAsync` are group centric like their user counterparts and live on `GroupProcessor`; the key page calls them once per group |
| Management on new service methods | Keys follow the verb services like every other resource type, with no service of their own |
| | Every processor lookup by id is scoped to the caller's account (`AccountScope`), covered by `CrossAccountCoverageTests` |
| | Rotating a key's secret requires the caller to hold everything the key holds |
