# IAMOptions Configuration

`IAMOptions` is the single builder for all Corely.IAM configuration. It consolidates the security provider, database configuration, crypto algorithms, and custom resource types into a fluent API.

## Features

- **Static factory** — `Create()` enforces required parameters via method signature, not runtime surprises
- **Fluent chaining** — optional configuration methods return `this` for chaining
- **Crypto defaults** — sensible defaults for all algorithms; override only when needed
- **Custom resource types** — register host-app resource types for validation and UI dropdowns
- **Two paths** — EF production path and mock testing path from the same API

## Usage

### Production Setup (Entity Framework)

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfigFactory);
services.AddIAMServices(options);
```

### Test Setup (Mock Repositories)

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider);
services.AddIAMServices(options);
```

The `Create()` overload without `efConfigFactory` registers in-memory mock repositories instead of EF Core.

### With Custom Resource Types

```csharp
var options = IAMOptions.Create(configuration, securityConfigProvider, efConfigFactory)
    .RegisterResourceType("invoice", "Customer invoices")
    .RegisterResourceType("report", "Financial reports");
```

Resource type names are case-insensitive. Duplicates (including case variants like `"Account"` and `"account"`) are silently overwritten with the latest value. IAM-defined types are pre-registered in the `IResourceTypeRegistry` and do not need to be added here.

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
| `RegisterResourceType` | `name`, `description` | — | Adds a custom resource type to the registry |
| `UseSymmetricEncryption` | `code` | `AES_CODE` | Symmetric encryption algorithm |
| `UseAsymmetricEncryption` | `code` | `RSA_CODE` | Asymmetric encryption algorithm |
| `UseAsymmetricSignature` | `code` | `ECDSA_SHA256_CODE` | Asymmetric signature algorithm |
| `UseHash` | `code` | `SALTED_SHA256_CODE` | Hashing algorithm |

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

`AddIAMServices` binds two configuration sections from `IConfiguration`:

| Section | Options Class | Properties |
|---------|--------------|------------|
| `SecurityOptions` | `SecurityOptions` | `MaxLoginAttempts` (5), `LockoutCooldownSeconds` (900), `AuthTokenTtlSeconds` (3600) |
| `PasswordValidationOptions` | `PasswordValidationOptions` | Minimum length, complexity requirements |

Configure these in `appsettings.json`:

```json
{
  "SecurityOptions": {
    "MaxLoginAttempts": 5,
    "LockoutCooldownSeconds": 900,
    "AuthTokenTtlSeconds": 3600
  }
}
```
