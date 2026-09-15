# Corely.IAM.Demos.UsersOnly

A personal notes app. People sign up with a username and password and see only their own notes.
There are no accounts, groups, roles, or permissions - IAM handles sign-in and the app keys its data
by `UserContext.User.Id`.

## Run it

Needs SQL Server LocalDB (installed with Visual Studio). From the repository root:

```powershell
dotnet run --project Corely.IAM.DataAccessMigrations.Cli -- db create -p MsSql -c "Server=(localdb)\MSSQLLocalDB;Database=CorelyIamDemoUsersOnly;Trusted_Connection=True;"
dotnet run --project Corely.IAM.Demos.UsersOnly
```

The first command creates the IAM schema - the same `corely-iam-db` tool a real deployment uses. The
app creates its own notes database on startup. Open https://localhost:7101 and sign up.

To start with users already there, seed once before running:

```powershell
dotnet run --project Corely.IAM.Demos.UsersOnly -- --seed
```

That creates `alice` and `marcus`, each with a few notes, password `Test1234`. Rerunning skips
users that already exist.

`appsettings.Development.json` holds a committed system key. It exists so the demo runs without
setup and protects nothing but local demo data; a real app supplies its own key.

## What to look at

- `Program.cs` - `MapRazorComponents` without the Corely.IAM.Web assembly, so none of its admin
  pages are routed. Sign-in and register still work: they are Razor Pages.
- `Components/Pages/Home.razor` - every query filters by the signed-in user's id.
- `Components/Pages/Profile.razor` - a profile page composed from the library's public MFA, password,
  and Google sections. Deleting the user also deletes their notes, because IAM does not know about
  the app's tables.
- `Pages/Shared/_AuthLayout.cshtml` - replaces the library's "Corely IAM" branding on the sign-in
  pages.
