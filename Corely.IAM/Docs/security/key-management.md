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

A signed in user reaches the keys of accounts in their `AvailableAccounts`. System context reaches any
account's keys, so code running as the system proves the account id belongs to the work it is doing
before it asks ([Gotchas](../gotchas.md)).

User key providers use the current user context (no user ID parameter):

```csharp
var result = await retrievalService.GetUserSymmetricEncryptionProviderAsync();
```

## Rotation

Each key has versions, starting at 1. Rotating adds the next version, and the newest one encrypts and signs:

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

Earlier versions are kept, never deleted:

- **Ciphertext** carries the key version it was made with (`provider:version:data`), which is the `Version` of the key row that made it, so a provider decrypts values from any version. `ReEncrypt` moves a value onto the newest version; data encrypted outside IAM is re-encrypted by whoever holds it, when they choose.
- **Signatures** made with an earlier signing key still pass `Verify`, unless that version is marked compromised. `PublicKey` is the newest key, so anyone verifying outside IAM needs it for new signatures.
- **Sign in tokens** are signed with the user's newest signing key and validated against every version, so rotating a user's signing key does not end their sessions.
- **The algorithm may change between versions.** Each version records its provider, and a value is decrypted with the provider it names.

Account keys need Update permission on the account. User keys are a self operation: only the signed in user rotates their own, and system context is refused.

## Compromised Keys

Two calls mark a version compromised. `RotateCompromisedAccountKeyAsync` rotates a key and marks the
version it replaces, in one transaction. `MarkAccountKeyCompromisedAsync` marks any older version, for a
leak found after that version was rotated out normally. It refuses the current version with
`CurrentVersionError`, because the account would have no usable key: rotate instead. Marking cannot be
undone. `RotateAccountKeyAsync` marks nothing.

```csharp
var proof = new AccountKeyProof(currentPassword, totpCode);
await modificationService.MarkAccountKeyCompromisedAsync(
    new MarkAccountKeyCompromisedRequest(accountId, KeyType.AsymmetricSignature, version: 2, proof));
```

Both need Update on the account and the same proof as [revealing a key](#revealing-account-keys), so a
stolen session cannot do it: the password when the user has one, otherwise a fresh sign in with a linked identity provider, then
two factor when enabled. A wrong password counts toward lockout. System context and API keys are refused,
because they have nothing to prove. Both are always recorded in the audit log, refusals included.

| Key | A compromised version |
|-----|-----------------------|
| Signing | Stops verifying in `Verify`, stops validating API key access tokens, and leaves the JSON Web Key Set. API key callers renew with their refresh token and carry on. Anything signed with it must be signed again |
| Encryption, either kind | Still decrypts what it encrypted, or the data would be lost. Nothing new is encrypted with it, and it is not published. `CompromisedVersions` on the provider names it, and `ReEncrypt` moves values onto the current key |

Marking stops this application trusting the version. It does nothing to a copy someone holds: they can
still read whatever was encrypted with it, and anyone who stored the old public key can still verify with
it, so tell whoever relies on your signatures.

## Public Keys as a JSON Web Key Set

`GetAccountJsonWebKeySetAsync(accountId)` returns an account's public keys in the RFC 7517 format. It
needs no user context and asks for no permission, because the keys are public. It writes only public
fields: `AccountJsonWebKey` has no property for a private one.

| Key | `use` | `kty` | `alg` | Listed |
|-----|-------|-------|-------|--------|
| Signing, ECDSA P-256 with SHA-256 | `sig` | `EC` | `ES256` | Every version not marked compromised, newest first |
| Signing, RSA PKCS#1 with SHA-256 | `sig` | `RSA` | `RS256` | Every version not marked compromised, newest first |
| Encryption, RSA OAEP with SHA-256 | `enc` | `RSA` | `RSA-OAEP-256` | The current version |

- **`kid`** is `{accountId:N}-{use}-{version}` (`AccountJsonWebKey.KeyIdFor`). `Sign` returns a bare
  signature with no `kid`, so a host that wants verifiers to pick the key sends it alongside, built from
  the provider's `Version`.
- **Every trusted signing version stays listed**, because signatures made with an earlier key still verify.
- **Only the current encryption key is listed**, so nobody encrypts to a retired key. A value encrypted
  to an earlier version still decrypts.
- A key with no standard form, such as ECDSA on a curve JOSE gives no SHA-256 algorithm, is left out and
  logged.
- These are the account's keys. API key access tokens are signed with the account's signing key, so
  this set verifies their signature, though IAM also needs the token's row to be active. User sign in
  tokens are signed with the user's own key, so this set does not verify them.
- A compromised version is left out. See [Compromised Keys](#compromised-keys).

`Corely.IAM.Web` serves the set from `MapIAMJwksEndpoints`; see its [Bearer API](../../../Corely.IAM.Web/Docs/bearer-api.md) page.

## Revealing Account Keys

`RevealAccountKeyAsync` returns the current version of an account key in the clear, for the rare case a
customer needs it outside the app. Anyone holding one can read or sign as the account, and rotating does
not cancel a copy, so it is guarded beyond an ordinary read:

- **Permission**: Execute on `account_secret_keys` (`Account.SECRET_KEYS_RESOURCE_TYPE`), an owner default.
- **Who**: a signed in user acting in that account who holds the permission, whether in the account or
  through the platform account. System context and API keys are refused.
- **Reauthentication**: see below.
- **Audit**: every call is recorded, whatever the audit settings say, refusals included. A platform
  member who does not belong to the account is recorded in the `PlatformMember` cohort, so the customer's
  audit log shows it.
- **Never serialized**: the key is read with `ReadKey()` and is not a property, so a serializer or a
  structured log cannot reach it, and the request's password and code are internal. No endpoint calls it.

```csharp
var result = await retrievalService.RevealAccountKeyAsync(
    new RevealAccountKeyRequest(accountId, KeyType.AsymmetricSignature, currentPassword, totpCode));
var key = result.ReadKey();
```

| `KeyType` | `ReadKey()` returns |
|-----------|---------------------|
| `SymmetricEncryption` | The key, Base64 |
| `AsymmetricEncryption`, `AsymmetricSignature` | The private key, Base64 PKCS#8 |

**Proving it is you.** A user with a password proves it with that password, whatever else they have. A
user with no password proves it with a fresh `Reauthenticate` round trip to one of their linked identity
providers (see [Federated Sign-In](../federated-sign-in.md#proving-it-is-you)). The proof counts for five
minutes, once, and only in the session that made it; until then the call returns
`FederatedSignInRequiredError`. Either way the two
factor code follows when the user has two factor on. A user with neither gets
`NoReauthenticationMethodError`, whose message tells them to set a password on their profile.

Only the current version is returned. Ciphertext names the version that made it, so a copy reads only
values encrypted with that version: copy again after a rotation, and re-encrypt older values first if
they are to be read outside the app.

## Provider Interfaces

All three provider interfaces follow a consistent pattern, and each has `Version`, the version of the key it encrypts or signs with:

Each also has `Versions`, one `KeyVersionInfo` per version with its number, provider name, public key (empty for a symmetric key) and whether it is compromised. It never holds a private or symmetric key.

- **`IIamSymmetricEncryptionProvider`**: `Encrypt(string)`, `Decrypt(string)`, `ReEncrypt(string)`
- **`IIamAsymmetricEncryptionProvider`**: `Encrypt(string)`, `Decrypt(string)`, `GetPublicKey()`
- **`IIamAsymmetricSignatureProvider`**: `Sign(string)`, `Verify(string, string)`, `GetPublicKey()`

## Notes

- Private keys are stored encrypted in the database and are decrypted in memory only when a provider is requested
- A provider holds every version of its key; an earlier signing key contributes only its public key
- Key providers are returned as ready-to-use objects, with no additional setup required
- The system key must be provisioned externally (environment variable, key vault, etc.)
- See [Corely.Security docs](https://github.com/ultrabstrong/Corely/tree/master/Corely.Security/Docs) for the underlying crypto primitives
