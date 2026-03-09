# Account Encryption & Signature Keys — Usability Plan

## Summary

Each account gets three keys at creation time: one AES symmetric encryption key, one RSA
asymmetric encryption key pair, and one ECDSA-SHA256 asymmetric signature key pair. All private
keys are encrypted with the system-level symmetric key before storage. The infrastructure is
in place (entities, EF configurations, mappers, generation), but no service or UI currently
exposes these keys for use. This document captures the current state and the plan for making
account keys usable by consuming applications.

---

## What Exists Today

### Entities & Database

| Entity | Table | Fields |
|--------|-------|--------|
| `AccountSymmetricKeyEntity` | `AccountSymmetricKeys` | Id, AccountId, KeyUsedFor, ProviderTypeCode, Version, EncryptedKey, CreatedUtc, ModifiedUtc |
| `AccountAsymmetricKeyEntity` | `AccountAsymmetricKeys` | Id, AccountId, KeyUsedFor, ProviderTypeCode, Version, PublicKey, EncryptedPrivateKey, CreatedUtc, ModifiedUtc |

Both entities inherit from base `SymmetricKeyEntity` / `AsymmetricKeyEntity` and add `Id` +
`AccountId`. EF configurations enforce a **unique index on `(AccountId, KeyUsedFor)`**, meaning
one key per account per usage type. Cascade delete from `AccountEntity`.

### Models

- `Account` model has `List<SymmetricKey>?` and `List<AsymmetricKey>?` properties
- `SymmetricKey` model wraps the encrypted key as `ISymmetricEncryptedValue`
- `AsymmetricKey` model stores `PublicKey` (plain) + `PrivateKey` (`ISymmetricEncryptedValue`)
- `KeyUsedFor` enum: `Encryption`, `Signature`

### Key Generation (SecurityProvider)

`ISecurityProvider` exposes three methods that generate keys encrypted with the system key:

1. `GetSymmetricEncryptionKeyEncryptedWithSystemKey()` → AES key
2. `GetAsymmetricEncryptionKeyEncryptedWithSystemKey()` → RSA key pair
3. `GetAsymmetricSignatureKeyEncryptedWithSystemKey()` → ECDSA-SHA256 key pair

These are called in `AccountProcessor.CreateAccountAsync()` and `UserProcessor.CreateUserAsync()`
during registration.

### Mappers

`SymmetricKeyMapper` and `AsymmetricKeyMapper` handle both user and account key entities:

- `ToUserEntity(Guid userId)` / `ToAccountEntity(Guid accountId)` — model → entity
- `ToModel(entity, encryptionProviderFactory)` — entity → model (overloaded per entity type)

`AccountMapper.ToEntity()` maps the key collections via these mappers, mirroring how
`UserMapper.ToEntity()` handles user keys.

---

## What Consumes Keys Today

### User Keys (fully wired)

| Consumer | Key Type | Usage |
|----------|----------|-------|
| `AuthenticationProvider` | User asymmetric signature key | Signs JWT tokens (ECDSA-SHA256) |
| `AuthenticationProvider` | User asymmetric signature public key | Validates JWT tokens |
| `UserProcessor.GetAsymmetricSignatureVerificationKeyAsync` | User asymmetric signature public key | Exposed for external JWT verification |

### Account Keys (persisted but not yet consumed)

Nothing currently reads account keys back from the database:

- `GetAccountByIdAsync(hydrate=true)` includes Users/Groups/Roles/Permissions but **not** keys
- No service method exposes account keys to callers
- No processor method retrieves or uses account keys

---

## Service Layer Exposure

### Currently Exposed (via IRetrievalService / IRegistrationService)

- `GetAccountAsync(id, hydrate)` → returns `Account` model but keys are not included
- `RegisterAccountAsync(request)` → creates account with keys (persisted via mapper)

### Not Exposed

- No endpoint or service method to:
  - Retrieve account encryption/signature keys
  - Rotate account keys
  - Use account keys for encrypt/decrypt/sign/verify operations
  - Manage key versions

---

## Web & DevTools

- **WebApp**: No UI for account keys. `AccountDetail.razor` shows users, groups, roles,
  permissions, and invitations — no key panel.
- **DevTools**: Has general-purpose crypto utilities (`sym-encrypt`, `sym-decrypt`, `hash`,
  `sign`, `encode`, `decode`) but no account-specific key operations.

---

## Comparison: User Keys vs Account Keys

| Aspect | User Keys | Account Keys |
|--------|-----------|--------------|
| Entity types | `UserSymmetricKeyEntity`, `UserAsymmetricKeyEntity` | `AccountSymmetricKeyEntity`, `AccountAsymmetricKeyEntity` |
| Mapper support | ✅ Full | ✅ Full |
| Persisted to DB | ✅ Yes | ✅ Yes |
| Read back from DB | ✅ Yes (Include in AuthenticationProvider) | ✅ Yes (via GetAccountKeysAsync) |
| Used by system | ✅ JWT signing/validation | ✅ Exposed as IIam*Provider wrappers |
| Exposed via services | ✅ GetAsymmetricSignatureVerificationKeyAsync | ✅ GetAccount*ProviderAsync (3 methods) |
| Key rotation | ❌ Not implemented | ❌ Not implemented |
| Web UI | ❌ None | ❌ None |

---

## Design Decision

**Return Corely.Security providers + hydrated key stores** rather than wrapping every crypto
operation with IAM-specific service methods.

Rationale:
- Corely.Security already defines clean interfaces (`ISymmetricEncryptionProvider`,
  `IAsymmetricEncryptionProvider`, `IAsymmetricSignatureProvider`) with Encrypt, Decrypt,
  ReEncrypt, Sign, Verify, etc.
- Wrapping each operation in IAM would duplicate this surface area, require ongoing maintenance
  as Corely.Security evolves, and force consumers to learn a new API for identical operations.
- IAM's value-add is key lifecycle management (generation, encrypted storage, access control)
  — not crypto operations themselves. Returning the provider + key store keeps this separation clean.
- The key store is an abstraction (`ISymmetricKeyStoreProvider` / `IAsymmetricKeyStoreProvider`),
  not raw key material, so the consumer works through a controlled interface.
- Authorization is enforced at retrieval time — once a user is authorized to access an account's
  keys, there's no benefit to re-checking per crypto operation.

## Out of Scope

- **Key rotation** — adding new key versions, re-encrypting with new keys, version management
- **Key generation** — creating additional keys beyond what account registration already provides
- **Web UI** — key info panel in AccountDetail.razor or DevTools account-specific commands

These can be layered on after the core retrieval and usage pattern is established.

## Phase 1: Account Key Provider Retrieval ✅ Implemented

### IRetrievalService Methods

Three new methods on `IRetrievalService`, one per account key type:

```csharp
Task<RetrieveSingleResult<IIamSymmetricEncryptionProvider>> GetAccountSymmetricEncryptionProviderAsync(Guid accountId);
Task<RetrieveSingleResult<IIamAsymmetricEncryptionProvider>> GetAccountAsymmetricEncryptionProviderAsync(Guid accountId);
Task<RetrieveSingleResult<IIamAsymmetricSignatureProvider>> GetAccountAsymmetricSignatureProviderAsync(Guid accountId);
```

Each returns a wrapper that encapsulates both the Corely.Security provider and a hydrated key
store, so the caller works with a single object. The wrapper delegates to the underlying
provider, binding the key store automatically.

Note: Hashing is stateless in Corely.Security — `IHashProvider` generates salts internally and
doesn't use stored keys. Consumers can use `IHashProviderFactory` directly from DI. No
account-level hash provider needed.

### Provider Interfaces and Implementations

New interfaces in `Security/Models/` — reusable for both account and user keys:

```csharp
public interface IIamSymmetricEncryptionProvider
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ReEncrypt(string ciphertext);
}

public interface IIamAsymmetricEncryptionProvider
{
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ReEncrypt(string ciphertext);
}

public interface IIamAsymmetricSignatureProvider
{
    string Sign(string payload);
    bool Verify(string payload, string signature);
}
```

Implementations bind the key store to the provider at construction time:

```csharp
public class IamSymmetricEncryptionProvider(
    ISymmetricEncryptionProvider provider,
    ISymmetricKeyStoreProvider keyStore) : IIamSymmetricEncryptionProvider
{
    public string Encrypt(string plaintext) => provider.Encrypt(plaintext, keyStore);
    public string Decrypt(string ciphertext) => provider.Decrypt(ciphertext, keyStore);
    public string ReEncrypt(string ciphertext) => provider.ReEncrypt(ciphertext, keyStore);
}
```

Asymmetric encryption and signature implementations follow the same pattern with their
respective provider/key store types.

### Layer-by-Layer Implementation

**1. AccountProcessor** — `GetAccountKeysAsync(Guid accountId)`:
- Loads the account entity with `.Include(a => a.SymmetricKeys).Include(a => a.AsymmetricKeys)`
- Access control: user must belong to the account (checked via `UserContext.AvailableAccounts`)
- Authorization decorator: `AuthAction.Read` on `ACCOUNT_RESOURCE_TYPE`
- Telemetry decorator: standard `ExecuteWithLoggingAsync` wrapper

**2. SecurityProvider** — three `Build*Provider` methods:
- Take key models, decrypt private keys via `DecryptWithSystemKey`
- Hydrate `InMemorySymmetricKeyStoreProvider` / `InMemoryAsymmetricKeyStoreProvider`
- Use `GetProvider(key.ProviderTypeCode)` (not `GetDefaultProvider()`) because key material is
  algorithm-specific — the provider must match the algorithm that generated the key
- Return `IIam*Provider` wrappers binding provider + key store

**3. RetrievalService** — three `GetAccount*ProviderAsync` methods:
- Call `AccountProcessor.GetAccountKeysAsync` to load keys
- Find the matching key entity by `KeyUsedFor` enum
- Map entity to model via `SymmetricKeyMapper.ToModel` / `AsymmetricKeyMapper.ToModel`
- Call `SecurityProvider.Build*Provider` to build the wrapper
- Wrap in `RetrieveSingleResult`

**4. Decorators**:
- `RetrievalServiceAuthorizationDecorator`: `HasAccountContext()` guard for all three methods
- `RetrievalServiceTelemetryDecorator`: `ExecuteWithLoggingAsync` wrapper for all three methods
- `AccountProcessorAuthorizationDecorator`: `IsAuthorizedAsync(Read, ACCOUNT_RESOURCE_TYPE)` guard
- `AccountProcessorTelemetryDecorator`: `ExecuteWithLoggingAsync` wrapper

### Tests (35 new tests, 1163 total)

**New test files (9 tests):**
- `IamSymmetricEncryptionProviderTests` — 3 tests: Encrypt/Decrypt/ReEncrypt delegation
- `IamAsymmetricEncryptionProviderTests` — 3 tests: Encrypt/Decrypt/ReEncrypt delegation
- `IamAsymmetricSignatureProviderTests` — 3 tests: Sign/Verify delegation + invalid signature

**SecurityProcessorTests (6 new tests):**
- `BuildSymmetricEncryptionProvider` — round-trip encrypt/decrypt + ReEncrypt
- `BuildAsymmetricEncryptionProvider` — round-trip encrypt/decrypt
- `BuildAsymmetricSignatureProvider` — sign/verify + tampered payload rejection

**AccountProcessor decorator tests (3 new tests):**
- Authorization decorator: auth success + unauthorized for `GetAccountKeysAsync`
- Telemetry decorator: delegation + logging for `GetAccountKeysAsync`

**RetrievalServiceTests (9 new tests):**
- Success path for all 3 provider methods (key found, provider built)
- Account not found for all 3 methods
- Key not found for all 3 methods

**RetrievalService decorator tests (9 new tests):**
- Authorization decorator: 6 tests (auth pass/fail for each provider method)
- Telemetry decorator: 3 tests (delegation + logging for each provider method)
