# IAMOptions Configuration

`IAMOptions` is the single builder for all Corely.IAM configuration. It consolidates the security provider, database configuration, crypto algorithms, and custom resource types into a fluent API.

## Features

- **Static factory**: `Create()` enforces required parameters via method signature, not runtime surprises
- **Fluent chaining**: optional configuration methods return `this` for chaining
- **Crypto defaults**: sensible defaults for all algorithms; override only when needed
- **Custom resource types**: register host-app resource types for validation and UI dropdowns
- **Two paths**: EF production path and mock testing path from the same API

## Usage

### Production Setup (Entity Framework)

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfigFactory);
services.AddIAMServices(options);
```

The configuration `efConfigFactory` builds is private to IAM: it is registered under a key only IAM's
DbContext resolves, never as a plain `IEFConfiguration`. A host's own DbContexts register their own.

### Test Setup (Mock Repositories)

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider);
services.AddIAMServices(options);
```

The `Create()` overload without `efConfigFactory` registers in-memory mock repositories instead of EF Core.

### With Custom Resource Types

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfigFactory)
    .RegisterResourceType("invoice", "Customer invoices", AuthAction.Read, AuthAction.Update)
    .RegisterResourceType("report", "Financial reports");
```

The actions after the description are what an account's Owner role gets on that type; none means
the owner gets nothing on it. Resource type names are case-insensitive. Duplicates (including case
variants like `"Invoice"` and `"invoice"`) are overwritten with the latest value. IAM-defined types
are pre-registered in the `IResourceTypeRegistry` and do not need to be added here. See
[Resource Types](resource-types.md).

### With Custom Crypto Algorithms

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfigFactory)
    .UseSymmetricEncryption(SymmetricEncryptionConstants.AES_CODE)
    .UseAsymmetricEncryption(AsymmetricEncryptionConstants.RSA_CODE)
    .UseAsymmetricSignature(AsymmetricSignatureConstants.ECDSA_SHA256_CODE)
    .UseHash(HashConstants.SALTED_SHA256_CODE);
```

## Create() Overloads

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `configuration` | `IConfiguration` | Yes | App configuration for options binding (`SecurityOptions`, `PasswordValidationOptions`) |
| `securityConfigurationProvider` | `ISecurityConfigurationProvider` | Yes | Supplies the system encryption key |
| `efConfigurationFactory` | `Func<IServiceProvider, IEFConfiguration>` | No | Database provider factory; omit for mock/testing path |

## Fluent Methods

| Method | Parameter | Default | Description |
|--------|-----------|---------|-------------|
| `RegisterResourceType` | `name`, `description`, `params ownerActions` | none | Adds a custom resource type to the registry, with the actions an account owner gets on it |
| `SetSupportActions` | `name`, `params supportActions` | Read, when the owner can read | What the platform account's support group gets on a registered type; see [Platform Account](platform.md#the-account-support-group) |
| `UseSymmetricEncryption` | `code` | `AES_CODE` | Symmetric encryption algorithm |
| `UseAsymmetricEncryption` | `code` | `RSA_CODE` | Asymmetric encryption algorithm |
| `UseAsymmetricSignature` | `code` | `ECDSA_SHA256_CODE` | Asymmetric signature algorithm |
| `UseHash` | `code` | `SALTED_SHA256_CODE` | Hashing algorithm |
| `UseCompiledModel` | `model` | none | An EF Core model built ahead of time; normally set through a compiled model package, below |

## Compiled Model

Entity Framework builds IAM's model the first time an app touches the database, which on a small
host can take tens of seconds and runs before IAM's startup jobs finish. A compiled model package
holds that model built ahead of time for one database provider:

| Package | Provider | Call |
|---------|----------|------|
| `Corely.IAM.CompiledModel.MsSql` | SQL Server | `.UseMsSqlCompiledModel()` |
| `Corely.IAM.CompiledModel.MySql` | MySQL and MariaDB | `.UseMySqlCompiledModel()` |

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfigFactory)
    .UseMsSqlCompiledModel();
```

Reference the package for the provider the app's `IEFConfiguration` uses; a model for another
provider describes the wrong column types. Without a package nothing changes: the model is built at
first use, as before.

A compiled model package is released with `Corely.IAM` at the same version, and its call refuses to
start against any other `Corely.IAM` version. Inside this repository, `AddMigration.ps1` and
`RemoveMigration.ps1` regenerate both models through `UpdateCompiledModels.ps1`, and
`CompiledModelTests` fails when either no longer describes the same tables, columns, keys and
indexes as the model built at runtime.

## Default Crypto Algorithms

| Purpose | Default | Constant |
|---------|---------|----------|
| Symmetric encryption | AES | `SymmetricEncryptionConstants.AES_CODE` |
| Asymmetric encryption | RSA | `AsymmetricEncryptionConstants.RSA_CODE` |
| Asymmetric signature | ECDSA SHA-256 | `AsymmetricSignatureConstants.ECDSA_SHA256_CODE` |
| Hashing | Salted SHA-256 | `HashConstants.SALTED_SHA256_CODE` |

## What AddIAMServices Registers

| Service | Lifetime | Purpose |
|---------|----------|---------|
| `IResourceTypeRegistry` | Singleton | Resource type lookup for validation and UI |
| `ISecurityConfigurationProvider` | Singleton | System key access |
| `ISymmetricEncryptionProviderFactory` | Singleton | Creates symmetric encryption providers |
| `IAsymmetricEncryptionProviderFactory` | Singleton | Creates asymmetric encryption providers |
| `IAsymmetricSignatureProviderFactory` | Singleton | Creates signature providers |
| `IHashProviderFactory` | Singleton | Creates hash providers |
| `ISecurityProvider` | Singleton | Security operations |
| `TimeProvider` | Singleton | Time abstraction for testability |
| `IRegistrationService` | Scoped | Entity creation and relationship management |
| `IDeregistrationService` | Scoped | Entity deletion and relationship removal |
| `IRetrievalService` | Scoped | Entity queries with filtering, ordering, pagination |
| `IModificationService` | Scoped | Entity updates |
| `IAuthenticationService` | Scoped | Sign-in, sign-out, account switching |
| `IUserContextProvider` | Scoped | Read user context |
| `IUserContextSetter` | Scoped | Write user context (host-only) |
| `IAuthorizationProvider` | Scoped | CRUDX permission checks |
| `IValidationProvider` | Scoped | FluentValidation integration |
| `IPasswordValidationProvider` | Scoped | Password strength validation |

All five services are wrapped with authorization and telemetry decorators via Scrutor.

## Configuration Sections

`AddIAMServices` binds three configuration sections from `IConfiguration`:

| Section | Options Class | Properties |
|---------|--------------|------------|
| `SecurityOptions` | `SecurityOptions` | `MaxLoginAttempts` (5), `LockoutCooldownSeconds` (900), `AuthTokenTtlSeconds` (3600), `AuthSessionTtlSeconds` (604800), `AuthSessionIdleTimeoutSeconds` (86400), `RefreshReuseGraceSeconds` (30) |
| `AuditOptions` | `AuditOptions` | `Source` (the entry assembly's name), `SettingsCacheTtlSeconds` (30). See [Auditing](auditing.md) |
| `PasswordValidationOptions` | `PasswordValidationOptions` | Minimum length (8), maximum length (128), complexity requirements. App-wide rather than per account: a user sets a password before joining any account, and can belong to several |

With a platform account, the password rules and every `SecurityOptions` setting except
`PermissionCacheTtlSeconds` come from the platform's security settings instead, and these keys are
ignored. The identity providers under `SecurityOptions:IdentityProviders` follow the same rule; see
[Federated Sign-In](federated-sign-in.md). Each has a range, checked at startup. See
[where security settings come from](platform.md#where-security-settings-come-from).

Configure these in `appsettings.json`:

```json
{
  "SecurityOptions": {
    "MaxLoginAttempts": 5,
    "LockoutCooldownSeconds": 900,
    "AuthTokenTtlSeconds": 3600,
    "AuthSessionTtlSeconds": 604800,
    "AuthSessionIdleTimeoutSeconds": 86400
  }
}
```
