# IRegistrationService

Creates entities, establishes relationships between entities, and manages invitations.

## Methods

| Method | Parameters | Returns |
|--------|-----------|---------|
| `RegisterUserAsync` | `RegisterUserRequest` | `RegisterUserResult` |
| `RegisterAccountAsync` | `RegisterAccountRequest` | `RegisterAccountResult` |
| `RegisterGroupAsync` | `RegisterGroupRequest` | `RegisterGroupResult` |
| `RegisterRoleAsync` | `RegisterRoleRequest` | `RegisterRoleResult` |
| `RegisterPermissionAsync` | `RegisterPermissionRequest` | `RegisterPermissionResult` |
| `RegisterUserWithAccountAsync` | `RegisterUserWithAccountRequest` | `RegisterUserWithAccountResult` |
| `RegisterUsersWithGroupAsync` | `RegisterUsersWithGroupRequest` | `RegisterUsersWithGroupResult` |
| `RegisterRolesWithGroupAsync` | `RegisterRolesWithGroupRequest` | `RegisterRolesWithGroupResult` |
| `RegisterRolesWithUserAsync` | `RegisterRolesWithUserRequest` | `RegisterRolesWithUserResult` |
| `RegisterPermissionsWithRoleAsync` | `RegisterPermissionsWithRoleRequest` | `RegisterPermissionsWithRoleResult` |
| `CreateInvitationAsync` | `CreateInvitationRequest` | `CreateInvitationResult` |
| `AcceptInvitationAsync` | `AcceptInvitationRequest` | `AcceptInvitationResult` |
| `RevokeInvitationAsync` | `Guid invitationId` | `RevokeInvitationResult` |
| `ListInvitationsAsync` | `ListInvitationsRequest` | `RetrieveListResult<Invitation>` |

> **Note:** TOTP and Google Auth methods have been moved to dedicated services. See [MFA](../mfa.md) (`IMfaService`) and [Google Sign-In](../google-signin.md) (`IGoogleAuthService`).

## Usage

### Register a User

```csharp
var result = await registrationService.RegisterUserAsync(
    new RegisterUserRequest("jdoe", "jdoe@example.com", "P@ssw0rd!"));
```

### Create an Account

```csharp
var result = await registrationService.RegisterAccountAsync(
    new RegisterAccountRequest("Acme Corp"));
```

The creating user is automatically added to the account and assigned the Owner role.

### Create a Permission

```csharp
var result = await registrationService.RegisterPermissionAsync(
    new RegisterPermissionRequest(
        resourceType: PermissionConstants.USER_RESOURCE_TYPE,
        resourceId: Guid.Empty,
        create: true, read: true, update: false, delete: false, execute: false,
        description: "Create and read users"));
```

### Assign Roles to a User

```csharp
var result = await registrationService.RegisterRolesWithUserAsync(
    new RegisterRolesWithUserRequest(userId, [roleId1, roleId2]));
```

### Create and Accept an Invitation

```csharp
var createResult = await registrationService.CreateInvitationAsync(
    new CreateInvitationRequest("newuser@example.com"));

var acceptResult = await registrationService.AcceptInvitationAsync(
    new AcceptInvitationRequest(invitationToken, "newuser", "P@ssw0rd!"));
```

## Authorization

- **Service level**: requires user context for entity creation; requires account context for relationship methods
- **Processor level**: CRUDX permission checks on the target resource type

## Notes

- `RegisterUserAsync` does not require authentication — it is the initial registration endpoint
- `RegisterAccountAsync` bootstraps an Owner role with full permissions and assigns it to the creating user
- Relationship methods (e.g., `RegisterUsersWithGroupAsync`) are idempotent for already-existing relationships
