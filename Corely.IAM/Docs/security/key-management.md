# Key Management

Three tiers of encryption keys: system (host-provisioned), account-scoped, and user-scoped.

## System Keys

The host application provides the system encryption key via `ISecurityConfigurationProvider`:

```csharp
public interface ISecurityConfigurationProvider
{
    ISymmetricKeyStoreProvider GetSystemSymmetricKey();
}
```

This key encrypts all stored key material in the database. Generate one with the [DevTools CLI](../../../Corely.IAM.DevTools/Docs/index.md):

```bash
cd Corely.IAM.DevTools
dotnet run -- sym-encrypt --create
```

## Account Keys

Each account has three key pairs:

| Key Type | Provider Interface | Purpose |
|----------|-------------------|---------|
| Symmetric | `IIamSymmetricEncryptionProvider` | Encrypt/decrypt account data |
| Asymmetric (encryption) | `IIamAsymmetricEncryptionProvider` | Public-key encryption |
| Asymmetric (signature) | `IIamAsymmetricSignatureProvider` | Digital signatures |

Account keys are created automatically when an account is registered.

## User Keys

Each user has the same three key types, following the same pattern.

## Retrieving Key Providers

```csharp
var result = await retrievalService.GetAccountSymmetricEncryptionProviderAsync(accountId);
if (result.ResultCode == RetrieveResultCode.Success)
{
    var provider = result.Item;
    var encrypted = provider.Encrypt("sensitive data");
    var decrypted = provider.Decrypt(encrypted);
}
```

User key providers use the current user context (no user ID parameter):

```csharp
var result = await retrievalService.GetUserSymmetricEncryptionProviderAsync();
```

## Provider Interfaces

All three provider interfaces follow a consistent pattern:

- **`IIamSymmetricEncryptionProvider`** — `Encrypt(string)`, `Decrypt(string)`, `ReEncrypt(string)`
- **`IIamAsymmetricEncryptionProvider`** — `Encrypt(string)`, `Decrypt(string)`, `GetPublicKey()`
- **`IIamAsymmetricSignatureProvider`** — `Sign(string)`, `Verify(string, string)`, `GetPublicKey()`

## Notes

- Private keys are stored encrypted in the database — they are decrypted in memory only when a provider is requested
- Key providers are returned as ready-to-use objects — no additional setup required
- The system key must be provisioned externally (environment variable, key vault, etc.)
- See [Corely.Security docs](https://github.com/ultrabstrong/Corely/tree/master/Corely.Security/Docs) for the underlying crypto primitives
