# Corely.IAM.Demos.SharedAccount

A team notes app. One person starts a team and invites others; everyone on the team shares the same
notes. There are no groups, roles, or permissions to manage - IAM's account is the team, and the app
keys its data by `UserContext.CurrentAccount.Id`.

## Run it

Needs SQL Server LocalDB (installed with Visual Studio). From the repository root:

```powershell
dotnet run --project Corely.IAM.DataAccessMigrations.Cli -- db create -p MsSql -c "Server=(localdb)\MSSQLLocalDB;Database=CorelyIamDemoSharedAccount;Trusted_Connection=True;"
dotnet run --project Corely.IAM.Demos.SharedAccount
```

The first command creates the IAM schema - the same `corely-iam-db` tool a real deployment uses. The
app creates its own notes database on startup. Open https://localhost:7102.

To see two people on one team, use a second browser profile or a private window:

1. Sign up as the first user and start a team.
2. On the team page, invite the second user's email address and copy the token.
3. Sign up as the second user with that email, and paste the token under "Join a team".

`appsettings.Development.json` holds a committed system key. It exists so the demo runs without
setup and protects nothing but local demo data; a real app supplies its own key.

## What to look at

- `Components/Pages/Home.razor` - a user with no team creates one or joins with a token. Both end at
  `/switch-account`, which issues a token for the new membership. Notes are filtered by account id.
- `Components/Pages/Team.razor` - the owner lists members and creates invitations. A member sees
  neither: they hold no roles, so IAM refuses both calls. The app shows that refusal instead of
  checking roles itself.
- The owner is the only role anyone has. The app never creates roles, groups, or permissions - the
  owner role and its permission come with every account.
- Sign-in switches a user with exactly one team straight into it, so most people never see the team
  picker.
