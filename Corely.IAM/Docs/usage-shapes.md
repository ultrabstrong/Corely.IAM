# Usage Shapes

Corely.IAM does not require accounts, groups, roles, or permissions. An app uses as much of it as it
needs, and the rest of the schema sits empty. The full schema is always deployed: the tables an app
does not use cost nothing, and an app can grow into them later without a migration.

| Shape | Uses | App data keyed by |
|-------|------|-------------------|
| Users only | Users, passwords, MFA, Google sign-in | `UserContext.User.Id` |
| One shared account | The above, plus accounts and invitations | `UserContext.CurrentAccount.Id` |
| Full RBAC | Everything, including groups, roles, and permissions | Resource ids checked with permissions |

Runnable examples of the first two are `Corely.IAM.Demos.UsersOnly` and
`Corely.IAM.Demos.SharedAccount` in the repository.

## Users Only

Register and sign in. No account is involved, and `CurrentAccount` stays `null`.

```csharp
await registrationService.RegisterUserAsync(
    new RegisterUserRequest("alice", "alice@example.com", "P@ssw0rd!"));

var signIn = await authenticationService.SignInAsync(
    new SignInRequest("alice", "P@ssw0rd!", deviceId));
```

Permissions live inside accounts, so this shape gets authentication and no authorization. A user
owns their own data outright; scope every query to them:

```csharp
var userId = userContextProvider.GetUserContext()!.User!.Id;
var notes = await db.Notes.Where(n => n.UserId == userId).ToListAsync();
```

Deleting the user through `IDeregistrationService.DeregisterUserAsync` removes IAM's rows only. The
app deletes its own.

## One Shared Account

The account is the team. Whoever creates it becomes its owner, holding the account's system-defined
owner role (`RoleConstants.OWNER_ROLE_NAME`) and its wildcard permission:

```csharp
var account = await registrationService.RegisterAccountAsync(
    new RegisterAccountRequest("Acme", userId));
```

The owner invites people by email. The invitee signs up with that email and accepts the token:

```csharp
var invitation = await invitationService.CreateInvitationAsync(
    new CreateInvitationRequest(accountId, "bob@example.com", null, ExpiresInSeconds: 604800));

// Signed in as the invitee
var accepted = await invitationService.AcceptInvitationAsync(
    new AcceptInvitationRequest(invitation.Token!));
```

Scope app data to the current account:

```csharp
var accountId = userContextProvider.GetUserContext()!.CurrentAccount!.Id;
var notes = await db.Notes.Where(n => n.AccountId == accountId).ToListAsync();
```

A member who joins by invitation has no roles. IAM refuses them anything guarded by a permission
(listing users, creating invitations, changing the account), which leaves the owner as the only one
who manages the team. An app that wants every member to be an admin assigns them the owner role
after they accept, with `IRegistrationService.RegisterRolesWithUserAsync`.

A user with exactly one account is switched into it at sign-in by `Corely.IAM.Web`, so they never
see an account picker.

## Notes

- Creating or joining an account changes the user's memberships, which a new auth token has to
  carry. In `Corely.IAM.Web`, send the user to `/switch-account?accountId={id}` afterwards.
- The account pages in `Corely.IAM.Web` (`/create-account`, `/select-account`) stay reachable in the
  users-only shape. An account created there is ignored by an app that keys data by user.
- Hosting these shapes with `Corely.IAM.Web` without its admin pages is covered in the
  [Corely.IAM.Web setup guide](../../Corely.IAM.Web/Docs/setup.md#simple-apps).
