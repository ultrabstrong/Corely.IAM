# Accounts

Top-level multi-tenant container. All groups, roles, permissions, and invitations are scoped to an account.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `AccountName` | `string` | Display name |
| `SymmetricKeys` | `List<SymmetricKey>?` | Account encryption keys (hydrated) |
| `AsymmetricKeys` | `List<AsymmetricKey>?` | Account encryption/signature keys (hydrated) |
| `Users` | `List<ChildRef>?` | Users in this account (hydrated) |
| `Groups` | `List<ChildRef>?` | Groups in this account (hydrated) |
| `Roles` | `List<ChildRef>?` | Roles in this account (hydrated) |
| `Permissions` | `List<ChildRef>?` | Permissions in this account (hydrated) |

## Relationships

- **Users** — M:M (users can belong to multiple accounts)
- **Groups, Roles, Permissions, Invitations** — 1:M (scoped to this account)

## Key Behaviors

- Creating an account bootstraps an **Owner Role** with full CRUDX permissions on all resource types
- The creating user is automatically added to the account and assigned the Owner role
- Account deletion requires all M:M relationships to be manually cleared (SQL Server constraint)
- Account name is the only mutable property

## Result Codes

| Code | Meaning |
|------|---------|
| `CreateAccountResultCode.Success` | Account created |
| `CreateAccountResultCode.AccountExistsError` | Duplicate name |
| `CreateAccountResultCode.UserOwnerNotFoundError` | Creating user not found |
| `DeleteAccountResultCode.AccountNotFoundError` | Account not found |
