# Migrating to Corely.IAM 5.0

No schema change and no behavior change. Three things move: types that were public by accident are
now internal, every constants class is gone, its values now declared on the type they describe, and
`AuthAction` changes namespace.
`corely-iam-db` 5.0 goes with it.

## Constants moved

Find and replace.

| Old | New |
|-----|-----|
| `PermissionConstants.ACCOUNT_RESOURCE_TYPE` | `Account.RESOURCE_TYPE` |
| `PermissionConstants.USER_RESOURCE_TYPE` | `User.RESOURCE_TYPE` |
| `PermissionConstants.GROUP_RESOURCE_TYPE` | `Group.RESOURCE_TYPE` |
| `PermissionConstants.ROLE_RESOURCE_TYPE` | `Role.RESOURCE_TYPE` |
| `PermissionConstants.PERMISSION_RESOURCE_TYPE` | `Permission.RESOURCE_TYPE` |
| `AuditConstants.AUDIT_RESOURCE_TYPE` | `AuditEntry.RESOURCE_TYPE` |
| `AuditConstants.AUDIT_SETTINGS_RESOURCE_TYPE` | `AccountAuditSettings.RESOURCE_TYPE` |
| `AuditConstants.PLATFORM_SETTINGS_RESOURCE_TYPE` | Split in two; see below |
| `RoleConstants.OWNER_ROLE_NAME` | `Role.OWNER_ROLE_NAME` |
| `RoleConstants.ROLE_NAME_MIN_LENGTH`, `ROLE_NAME_MAX_LENGTH` | `Role.NAME_MIN_LENGTH`, `Role.NAME_MAX_LENGTH` |
| `GroupConstants.GROUP_NAME_MIN_LENGTH`, `GROUP_NAME_MAX_LENGTH` | `Group.NAME_MIN_LENGTH`, `Group.NAME_MAX_LENGTH` |
| `AccountConstants.ACCOUNT_NAME_MIN_LENGTH`, `ACCOUNT_NAME_MAX_LENGTH` | `Account.ACCOUNT_NAME_MIN_LENGTH`, `Account.ACCOUNT_NAME_MAX_LENGTH` |
| `UserConstants.USERNAME_MIN_LENGTH`, `USERNAME_MAX_LENGTH` | `User.USERNAME_MIN_LENGTH`, `User.USERNAME_MAX_LENGTH` |
| `UserConstants.EMAIL_MAX_LENGTH`, and the `EMAIL_MAX_LENGTH` of `GoogleAuthConstants`, `InvitationConstants` and `PasswordRecoveryConstants` | `User.EMAIL_MAX_LENGTH` |
| `InvitationConstants.DESCRIPTION_MAX_LENGTH` | `Invitation.DESCRIPTION_MAX_LENGTH` |
| `InvitationConstants.MIN_EXPIRY_SECONDS`, `MAX_EXPIRY_SECONDS`, `DEFAULT_EXPIRY_SECONDS` | `CreateInvitationRequest.MIN_EXPIRY_SECONDS` and so on |
| `TotpAuthConstants.TOTP_CODE_LENGTH` | `ConfirmTotpRequest.CODE_LENGTH`, `DisableTotpRequest.CODE_LENGTH` |
| `SecurityPolicyConstants.*` | `SecurityPolicy.*`, same names |
| `AuditConstants.SUCCESS_RESULT_CODE`, `FAILED_RESULT_CODE`, `FAULT_RESULT_CODE`, `UNKNOWN_SOURCE` | `AuditEntry.SUCCESS_RESULT_CODE` and so on |
| `AuditConstants.DEFAULT_MAX_RETENTION_DAYS` | `PlatformSettings.DEFAULT_AUDIT_MAX_RETENTION_DAYS` |
| `AuditConstants.DEFAULT_RETENTION_DAYS` | `AccountAuditSettings.DEFAULT_RETENTION_DAYS` |
| `AuditConstants.EXPORT_MAX_ENTRIES` | `ExportAuditEntriesResult.MAX_ENTRIES` |

Now internal, with no replacement: the JWT audience and claim names in `UserConstants`, the column
lengths in `AuditConstants`, `BasicAuthConstants`, `GoogleAuthConstants.GOOGLE_SUBJECT_ID_MAX_LENGTH`,
`MfaChallengeConstants`, the token and hash lengths in `InvitationConstants`,
`PasswordRecoveryConstants` and `TotpAuthConstants`, and `AuditConstants.AUDIT_ENTRIES_TABLE_NAME`.

The `using Corely.IAM.<Domain>.Constants;` lines go. `AuthAction` moved from
`Corely.IAM.Security.Constants` to `Corely.IAM.Security.Enums`:

```csharp
// 4.x
using Corely.IAM.Security.Constants;

// 5.0
using Corely.IAM.Security.Enums;
```

## Platform settings permission split in two

`platform_settings` is gone. Each platform settings area has its own type, so a role can be given one
without the other:

| Area | Type |
|------|------|
| Audit settings (`IAuditService.GetPlatformSettingsAsync`, `UpdatePlatformSettingsAsync`) | `platform_audit_settings`, `PlatformSettings.RESOURCE_TYPE` |
| Security settings (`IPlatformService.GetSecuritySettingsAsync`, `UpdateSecuritySettingsAsync`) | `platform_security_settings`, `SecurityPolicy.RESOURCE_TYPE` |

The platform owner receives both at the next start, and its Owner role keeps its old system defined
`platform_settings` permission, which nothing checks. A role you built that holds `platform_settings`
keeps a permission nothing checks too: give it the new types it needs, then delete the old one.

## Out of range security configuration fails at registration

The `SecurityOptions` and `PasswordValidationOptions` ranges were checked when the host started.
`AddIAMServices` now checks them, so a console tool or script that builds a host without starting it
is refused too, with the same message.

Since no configured settings can be out of range,
`BootstrapPlatformResultCode.InvalidSecuritySettingsError` is gone; `corely-iam-db platform
bootstrap` refuses an out of range `--settings` file before it writes anything.

## Types now internal

None of these was taken or returned by a public service or provider, so code using only the public
API is unaffected.

- The join entities `UserAccount`, `UserGroup`, `UserRole`, `GroupRole`, `RolePermission`.
- Processor results and requests: `AddUserToAccountRequest`, `AddUserToAccountResult`,
  `RemoveUserFromAccountRequest`, `RemoveUserFromAccountResult`, `DeleteAccountResult`,
  `GetAccountResult`, `ListAccountsForUserResult`, `BasicAuth`, `VerifyBasicAuthResult`,
  `DeleteGroupResult`, `RemoveRolesFromGroupResult`, `RemoveUsersFromGroupRequest`,
  `RemoveUsersFromGroupResult`, `DeletePermissionResult`, `DeleteRoleResult`, `GetRoleResult`,
  `RemovePermissionsFromRoleResult`, `CreateDefaultSystemRolesResult`, `DeleteUserResult`,
  `GetUserResult`, `GetAsymmetricKeyResult`, `GetUserAuthTokenRequest`, `UserAuthTokenResult`,
  `RevokeUserAuthTokenRequest`, `IsSoleOwnerOfAccountResult`, `RemoveRolesFromUserResult`,
  `RetrieveAccountsRequest`, `RetrieveAccountsResult`, and each one's result code enum.
- Result code enums a service converts before returning: `CreateUserResultCode`,
  `CreateAccountResultCode`, `CreateBasicAuthResultCode`, `UpdateBasicAuthResultCode`,
  `DeleteBasicAuthResultCode`.
- `ValidationResult`, `ValidationError`, `GoogleIdTokenPayload`, `UpsertType`,
  `PermissionLabelProvider`, and the classes `IamSymmetricEncryptionProvider`,
  `IamAsymmetricEncryptionProvider`, `IamAsymmetricSignatureProvider` (their `IIam*` interfaces stay
  public).
