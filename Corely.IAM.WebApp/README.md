# Corely.IAM.WebApp

Blazor Server host app for the Corely.IAM management portal.

## Prerequisites

1. **.NET 10.0 SDK** (or 9.0)
2. **SQL Server** (LocalDB or full instance) — or MySQL/MariaDB if you change the provider

## Setup

**1. Generate a system encryption key**

From the repo root:

```powershell
cd Corely.IAM.DevTools
dotnet run -- sym-encrypt --create
# Outputs a hex key string — copy it
```

**2. Configure `appsettings.json`**

Fill in the two required values (see `appsettings.template.json` for reference):

| Key | Value |
|-----|-------|
| `ConnectionStrings:DefaultConnection` | Your SQL Server connection string (e.g. `Server=(localdb)\MSSQLLocalDB;Database=CorelIAM;Trusted_Connection=True;`) |
| `Security:SystemKey` | The hex key from step 1 |

`Database:Provider` defaults to `"mssql"`. Change to `"mysql"` or `"mariadb"` if needed.

**3. Create the database and apply migrations**

```powershell
cd Corely.IAM.DataAccessMigrations.Cli
dotnet run -- config init mssql -c "your-connection-string"
dotnet run -- db create
```

- `config init` creates a local settings file for the migration CLI (provider + connection string)
- `db create` creates the database and applies all pending migrations

**4. Seed the demo environment (optional)**

From the WebApp folder:

```powershell
.\DemoSetup\SeedWebAppDemo.ps1
```

This seeds the WebApp's configured IAM database with the demo users/accounts used for local dogfooding. Demo users, including `admin` and `alice.johnson`, use the password `admin`.

**5. Run the app**

Set `Corely.IAM.WebApp` as the startup project in Visual Studio and press F5, or:

```powershell
cd Corely.IAM.WebApp
dotnet run
```

The app launches at **https://localhost:7100**.

### Password Recovery Demo

`Corely.IAM.WebApp` includes a demo forgot-password flow for the reference app:

- `/forgot-password` requests a recovery token through `IPasswordRecoveryService`
- `/forgot-password-preview` acts as a fake outbox and shows the generated reset token/link
- `/reset-password` demonstrates validating and consuming the recovery token

The preview page is controlled by `DemoFeatures:EnablePasswordRecoveryPreview` in `appsettings.json` / `appsettings.template.json`. In a real host, the token would be delivered through email or another trusted channel instead of being displayed in the UI.

## Optional: Seq Logging

The default config sends structured logs to [Seq](https://datalust.co/seq) at `http://localhost:5341`. If Seq isn't running, the app still works — console logging is unaffected. Remove the Seq entry from `Serilog:WriteTo` in `appsettings.json` to suppress connection warnings.
