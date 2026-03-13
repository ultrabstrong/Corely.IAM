# Step-by-Step Setup

Integrate Corely.IAM into a host application in 8 steps.

## 1) Install Packages

```bash
dotnet add package Corely.IAM
```

The Corely.IAM package includes `Corely.DataAccess` and `Corely.Security` as transitive dependencies.

## 2) Create a Security Configuration Provider

Implement `ISecurityConfigurationProvider` to supply the system encryption key. This key encrypts all stored key material in the database.

```csharp
public class SecurityConfigurationProvider(IConfiguration configuration)
    : ISecurityConfigurationProvider
{
    public ISymmetricKeyStoreProvider GetSystemSymmetricKey()
    {
        var key = configuration["Security:SystemKey"]
            ?? throw new InvalidOperationException("System key not configured");
        return new InMemorySymmetricKeyStoreProvider(key);
    }
}
```

Generate a key with the DevTools CLI:

```bash
cd Corely.IAM.DevTools
dotnet run -- sym-encrypt --create
```

Note: Store the system key securely (environment variable, key vault, etc.). Never commit it to source control.

## 3) Choose a Database Provider

Create an `IEFConfiguration` factory for your database. See the [Corely.DataAccess configuration docs](https://github.com/ultrabstrong/Corely/tree/master/Corely.DataAccess/Docs) for details.

```csharp
Func<IServiceProvider, IEFConfiguration> efConfig = sp =>
    new MsSqlEFConfiguration(connectionString, sp.GetRequiredService<ILoggerFactory>());
```

| Provider | Configuration Class | Connection String Example |
|----------|-------------------|--------------------------|
| SQL Server | `MsSqlEFConfiguration` | `Server=(localdb)\MSSQLLocalDB;Database=CorelIAM;Trusted_Connection=True;` |
| MySQL | `MySqlEFConfiguration` | `Server=localhost;Database=CorelIAM;Uid=root;Pwd=password;` |
| MariaDB | `MySqlEFConfiguration` | Same as MySQL |

## 4) Configure IAMOptions

```csharp
var options = IAMOptions.Create(builder.Configuration, securityConfigProvider, efConfig);
```

Optionally register custom resource types or override crypto algorithms:

```csharp
var options = IAMOptions.Create(builder.Configuration, securityConfigProvider, efConfig)
    .RegisterResourceType("invoice", "Customer invoices")
    .RegisterResourceType("report", "Financial reports")
    .UseSymmetricEncryption(SymmetricEncryptionConstants.AES_CODE);
```

See [IAMOptions Configuration](iam-options.md) for all options.

## 5) Register Services

```csharp
builder.Services.AddIAMServices(options);
```

This single call registers all IAM services, processors, repositories, validators, security providers, and the resource type registry.

## 6) Apply Migrations

Use the migration CLI tool to create the database and apply migrations:

```bash
cd Corely.IAM.DataAccessMigrations.Cli
dotnet run -- config init mssql -c "your-connection-string"
dotnet run -- db create
```

## 7) Set User Context

After authenticating a user (e.g., validating a JWT or cookie), set the user context so IAM services can enforce authorization:

```csharp
var contextSetter = serviceProvider.GetRequiredService<IUserContextSetter>();
var result = await contextSetter.SetUserContextAsync(token);
```

If using `Corely.IAM.Web`, the `AuthenticationTokenMiddleware` handles this automatically.

## 8) Use Services

Resolve any of the five services from DI and start managing identities:

```csharp
var registration = serviceProvider.GetRequiredService<IRegistrationService>();
var authentication = serviceProvider.GetRequiredService<IAuthenticationService>();
var retrieval = serviceProvider.GetRequiredService<IRetrievalService>();
var modification = serviceProvider.GetRequiredService<IModificationService>();
var deregistration = serviceProvider.GetRequiredService<IDeregistrationService>();
```

Register a user, sign in, and create an account:

```csharp
var userResult = await registration.RegisterUserAsync(
    new RegisterUserRequest("jdoe", "jdoe@example.com", "P@ssw0rd!"));

var signIn = await authentication.SignInAsync(
    new SignInRequest("jdoe", "P@ssw0rd!", "device-1"));

var accountResult = await registration.RegisterAccountAsync(
    new RegisterAccountRequest("Acme Corp"));
```

## 9) Where to Next?

- [IAMOptions Configuration](iam-options.md) — full builder API reference
- [Authentication](authentication.md) — sign-in, tokens, account switching
- [Authorization](authorization.md) — CRUDX model, effective permissions
- [Services](services/index.md) — all five service interfaces
- [Domains](domains/index.md) — entity models and relationships
- [Security](security/index.md) — key management, user context
