# Plan: Public surface and constant placement

**Status: done.** Built for Corely.IAM 5.0.0, Corely.IAM.Web 6.0.0 and corely-iam-db 5.0.0, not
yet released: nothing is tagged until every plan is complete.

## As built

Where the build differs from the plan below:

- **A resource type name lives on its resource** as `RESOURCE_TYPE`: `Account`, `User`, `Group`,
  `Role`, `Permission`, and for the audit types `AuditEntry`, `AccountAuditSettings` and
  `PlatformSettings`. That settles the open question.
- **A constant's home and its visibility are separate choices.** A limit on a public model's field
  stays on the model even when no host needs it, as an `internal const`: the audit column lengths on
  `AuditEntry`, the key limits on `SymmetricKey` and `AsymmetricKey`. Only values with no field on a
  public model went to an entity or class.
- **Renamed where the type now says it:** `Group.NAME_MAX_LENGTH`, `Role.NAME_MAX_LENGTH`,
  `GoogleAuthEntity.SUBJECT_ID_MAX_LENGTH`, `TotpRecoveryCodeEntity.CODE_LENGTH`,
  `ExportAuditEntriesResult.MAX_ENTRIES`, `AuditEntryEntity.TABLE_NAME`,
  `PlatformSettings.DEFAULT_AUDIT_MAX_RETENTION_DAYS`. The settings row id is `SINGLE_ROW_ID` on
  both `PlatformSettingsEntity` and `PlatformSecuritySettingsEntity`.
- **TOTP code length** is `ConfirmTotpRequest.CODE_LENGTH`, and `DisableTotpRequest.CODE_LENGTH`
  reads it.
- **The migration projects' `MsSqlDesignTimeConstants` and `MySqlDesignTimeConstants`** moved onto
  each project's `IAMDesignTimeDbContextFactory`, their only user. No constants class, file, folder
  or namespace is left in the repository.
- **Three dead values deleted,** not one: `MfaChallengeConstants.DEFAULT_TIMEOUT_SECONDS`,
  `TotpAuthConstants.RECOVERY_CODE_DISPLAY_LENGTH`, `PasswordRecoveryConstants.TOKEN_MAX_LENGTH`.
- **`AuthAction` moved to `Corely.IAM.Security.Enums`,** beside `KeyType` and `KeyUsedFor`. An enum
  in a constants namespace was a mistake, and this major version already breaks hosts. No
  `Constants` folder is left.
- **`AuthenticationConstants`** split onto `AuthCookieManager` (cookie names) and
  `UserContextClaimsBuilder` (claim names), both internal.
- **DevTools needed nothing:** its `ValidationResult` is its own record, not IAM's.
- **`Corely.IAM.Web` gained `InternalsVisibleTo`** for its functional tests and for Moq's
  `DynamicProxyGenAssembly2`, which mocks `ILogger<T>` of the now internal middleware and accessor.
- **Theories over an internal enum** take the parameter as `Enum` and cast, since a public test
  method cannot name an internal type.
- **The test also refuses public validators,** and its walker (`PublicTypes`) is one file in
  `Corely.IAM.UnitTests`, linked into `Corely.IAM.Web.UnitTests`. The Razor generated `_Imports`
  class is a root.
- **Migration guides:** `MIGRATION-5.0.md` and `MIGRATION-IAM.Web-6.0.md`.

## Problem Statement

Two things have drifted.

**Types are public that no consumer can use.** The rule is that services, providers and the models
they take and return are public, and everything behind them (processors, entities, mappers,
repositories and the models only a processor sees) is internal. A trace from the public API found
public types that nothing public references, so they are API by accident: a consumer can take a
dependency on them, and changing them later is a breaking change.

**A model's limits live somewhere else.** A username's maximum length is
`UserConstants.USERNAME_MAX_LENGTH`, not something on `User`. A reader of the model has to know to go
looking in a separate `Constants` folder, and some values are defined more than once
(`EMAIL_MAX_LENGTH` four times).

Both are breaking changes, so they ship together as one major version.

## Part 1: Make accidental public types internal

### Corely.IAM

1. **Join entities** in `Entities/JoinEntities.cs`: `UserAccount`, `UserGroup`, `UserRole`,
   `GroupRole`, `RolePermission`. Every other entity is already internal; the migrations reach these
   through `InternalsVisibleTo`.
2. **Processor only models,** with their result code enums:
   `AddUserToAccountRequest`, `AddUserToAccountResult`, `RemoveUserFromAccountRequest`,
   `RemoveUserFromAccountResult`, `DeleteAccountResult`, `GetAccountResult`,
   `ListAccountsForUserResult`, `BasicAuth`, `VerifyBasicAuthResult`, `DeleteGroupResult`,
   `RemoveRolesFromGroupResult`, `RemoveUsersFromGroupRequest`, `RemoveUsersFromGroupResult`,
   `DeletePermissionResult`, `DeleteRoleResult`, `GetRoleResult`, `RemovePermissionsFromRoleResult`,
   `CreateDefaultSystemRolesResult`, `DeleteUserResult`, `GetUserResult`, `GetAsymmetricKeyResult`,
   `GetUserAuthTokenRequest`, `UserAuthTokenResult`, `RevokeUserAuthTokenRequest`,
   `IsSoleOwnerOfAccountResult`, `RemoveRolesFromUserResult`, `RetrieveAccountsRequest`,
   `RetrieveAccountsResult`.
3. **Result code enums whose record is already internal** and which the services convert before
   returning: `CreateUserResultCode`, `CreateAccountResultCode`, `CreateBasicAuthResultCode`,
   `UpdateBasicAuthResultCode`, `DeleteBasicAuthResultCode`. Codes a service result carries stay
   public (`CreateGroupResultCode` on `RegisterGroupResult`, `AssignRolesToUserResultCode` on
   `RegisterRolesWithUserResult`, and the like).
4. **Plumbing:** `ValidationResult`, `ValidationError`, `GoogleIdTokenPayload`, `UpsertType`,
   `PermissionLabelProvider` (a static helper, not a provider in the DI sense).

The trace read source text, so each item is confirmed against the compiled assembly before it
changes. A type that turns out to be reachable stays public and the plan records why.

DevTools uses `ValidationResult` (`ConfigurationValidator.cs`). It gets `InternalsVisibleTo`, as the
migration CLI has, or stops using the type; whichever is smaller.

### Corely.IAM.Web

5. **Concrete services:** `AuthCookieManager`, `UserContextClaimsBuilder`, `BlazorUserContextAccessor`,
   `PostAuthenticationFlowService`, `AccountDisplayState`, `IamAuthenticationStateProvider`. Hosts
   resolve the interfaces; `AddIAMWeb` and `AddIAMWebBlazor` register the classes.
6. **Middleware:** `AuthenticationTokenMiddleware`, `CorrelationIdMiddleware`, wired by
   `IamWebAppExtensions`. The docs that name `AuthenticationTokenMiddleware` keep naming it; it still
   exists, it is just not something a host constructs.

Razor components stay public, since a host renders them.

### Stays public

Services, provider interfaces (`IAuthorizationProvider`, `IUserContextProvider`,
`ISecurityPolicyProvider`, `IResourceTypeRegistry`, `ISecurityConfigurationProvider`,
`IAuditProvider`, `ITotpProvider`, the `IIam*` encryption providers), the models they take and return,
options, and the registration extensions.

## Part 2: Constants move onto what they describe

The `Constants` folder in each domain goes away. Each value moves to the type a reader would look at:

| Kind of value | Goes to | Example |
|---------------|---------|---------|
| A limit on a public model's field | That model, `public const` | `User.USERNAME_MAX_LENGTH`, `Account.NAME_MAX_LENGTH`, `Invitation.DESCRIPTION_MAX_LENGTH` |
| A setting's default and range | The settings model | `SecurityPolicy.MAX_LOGIN_ATTEMPTS_MIN`, replacing `SecurityPolicyConstants` |
| A limit on a field no public model has | The entity or the processor that uses it, internal | challenge token sizes, the encrypted TOTP secret length, recovery code and secret hash lengths, the Google subject id length |
| A well known value of a model | That model | `Role.OWNER_ROLE_NAME` |
| Not about a model | Where it is used, internal unless a host needs it | the JWT claim names in `UserConstants` go with token handling |

Entity configurations and validators read the limits from the model, so the database column and the
validation rule cannot disagree.

**Defaults move with the constants.** A setting's default belongs on its model the same way
`SecurityPolicy.Default` already does: audit retention defaults go on the audit settings model, the
invitation expiry default on `CreateInvitationRequest`. Validation rules stay in the validators
(FluentValidation is internal), reading their numbers from the model. Readings and conversions are
already covered by the seams rule. Nothing else moves.

### Public: a host has a use for it

| Value | Home |
|-------|------|
| Username, email, account, group and role name lengths | `User`, `Account`, `Group`, `Role` |
| Invitation email, description length; expiry minimum, maximum and default | `Invitation`, `CreateInvitationRequest` |
| TOTP code length | the TOTP request models |
| Every `SecurityPolicyConstants` value | `SecurityPolicy` |
| Audit retention defaults, export maximum | the audit settings model, the export request |
| Audit result code strings (`Success`, `Failed`, `Fault`) | `AuditEntry`, whose `ResultCode` they fill |
| `OWNER_ROLE_NAME` | `Role` |
| Resource type names | one public class, see below |

### Internal: no public use

| Value | Home |
|-------|------|
| MFA challenge token bytes, token length, device id length, attempt limit | the MFA challenge entity and processor |
| Encrypted TOTP secret length, recovery code length and hash length | the TOTP entity and processor |
| Google subject id length | the Google auth entity |
| Invitation token length and column length | the invitation entity and processor |
| Password recovery secret length, token length, hash length, token lifetime | the password recovery entity and processor |
| Stored password hash length (`BasicAuthConstants.PASSWORD_MAX_LENGTH`) | the basic auth entity |
| Audit column lengths, table name, unknown source marker | the audit entity |
| JWT audience and claim names | the authentication provider |
| `PlatformConstants`, `SymmetricKeyConstants`, `AsymmetricKeyConstants`, `MigrationConstants` (already internal) | their entities, or the one class using them |

**Dead values are deleted, not moved.** `MfaChallengeConstants.DEFAULT_TIMEOUT_SECONDS` is unused
since the timeout became a security setting. Each value is checked for use before it moves.

**Email length** is one value. `UserConstants`, `GoogleAuthConstants`, `InvitationConstants` and
`PasswordRecoveryConstants` each define `EMAIL_MAX_LENGTH = 254`; it becomes `User.EMAIL_MAX_LENGTH`
and the others read it.

**Resource type names** (`PermissionConstants.ACCOUNT_RESOURCE_TYPE` and the rest, plus the three in
`AuditConstants`) are not limits on a model. Hosts use them, so they stay public, gathered in one
place instead of split across two classes. The name of that place is decided during the work.

`AuditConstants` also holds internal values (column lengths, the table name, result code strings);
those follow the table above.

`Corely.IAM.Web`'s `AuthenticationConstants` is reviewed the same way.

## CLAUDE.md

Every rule this work establishes is written down in the same push:

- **Visibility.** Services, providers and the models they take and return are public; processors,
  entities, mappers, repositories and processor only models are internal. A result code enum is
  public only when a service result carries it. Nothing is public so a test or tool can reach it.
- **The test enforces it,** and how to satisfy it (make it internal, or allowlist it with a reason).
- **Domain layout** loses `Constants/`.
- **Constants and defaults** live on the type they describe; a value with no public use is internal
  on the entity or class using it. Replaces the constants class example in "Magic strings".

## Part 3: A test that keeps it this way

A unit test per assembly (`Corely.IAM.UnitTests`, `Corely.IAM.Web.UnitTests`) loads the exported types
and fails when:

1. a type in an `Entities`, `Processors`, `Mappers` or `Repos` namespace, or named `*Entity` or
   `*Processor`, is public; or
2. a public class, record or enum cannot be reached from the public API. The roots are the public
   interfaces, options and registration extensions; reachable means appearing in a public member's
   signature (parameters, return types, properties, generic arguments, base types) of a root or of
   something already reachable. Razor components are roots in `Corely.IAM.Web`.

A short allowlist covers public types a host uses by name without any signature reaching them, such
as the resource type names. The failure message names each offending type and says to make it
internal or add it to the allowlist with a reason.

Cost: one reflection pass over a few hundred types per assembly, milliseconds, no database or host.

## Release

Not released on its own: nothing is tagged until every plan is in `Plans/Completed/`. Then one major
version of `Corely.IAM` and `Corely.IAM.Web`; the migration CLI's major follows
`Corely.IAM`'s. A `MIGRATION-<version>.md` at the repository root lists every type that became
internal and every constant's new home, old name to new name.

## Open questions

- The name and home of the public resource type names.
