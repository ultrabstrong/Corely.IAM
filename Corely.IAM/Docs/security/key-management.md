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

## Rotation

Each key has generations. Rotating adds the next generation, and the newest one encrypts and signs:

```csharp
await modificationService.RotateAccountKeyAsync(
    new RotateAccountKeyRequest(accountId, KeyType.SymmetricEncryption));

await modificationService.RotateCurrentUserKeyAsync(KeyType.AsymmetricSignature);
```

| `KeyType` | Rotates |
|-----------|---------|
| `SymmetricEncryption` | The symmetric encryption key |
| `AsymmetricEncryption` | The asymmetric encryption key pair |
| `AsymmetricSignature` | The signing key pair |

Earlier generations are kept, never deleted:

- **Ciphertext** carries the key version it was made with (`provider:version:data`), so a provider decrypts values from any generation. `ReEncrypt` moves a value onto the newest generation; data encrypted outside IAM is re-encrypted by whoever holds it, when they choose.
- **Signatures** made with an earlier signing key still pass `Verify`. `PublicKey` is the newest key, so anyone verifying outside IAM needs it for new signatures.
- **Sign in tokens** are signed with the user's newest signing key and validated against every generation, so rotating a user's signing key does not end their sessions.
- **The algorithm may change between generations.** Each generation records its provider, and a value is decrypted with the provider it names.

Account keys need Update permission on the account. User keys are a self operation: only the signed in user rotates their own, and system context is refused.

## Provider Interfaces

All three provider interfaces follow a consistent pattern:

- **`IIamSymmetricEncryptionProvider`**: `Encrypt(string)`, `Decrypt(string)`, `ReEncrypt(string)`
- **`IIamAsymmetricEncryptionProvider`**: `Encrypt(string)`, `Decrypt(string)`, `GetPublicKey()`
- **`IIamAsymmetricSignatureProvider`**: `Sign(string)`, `Verify(string, string)`, `GetPublicKey()`

## Notes

- Private keys are stored encrypted in the database and are decrypted in memory only when a provider is requested
- A provider holds every generation of its key; an earlier signing key contributes only its public key
- Key providers are returned as ready-to-use objects, with no additional setup required
- The system key must be provisioned externally (environment variable, key vault, etc.)
- See [Corely.Security docs](https://github.com/ultrabstrong/Corely/tree/master/Corely.Security/Docs) for the underlying crypto primitives
