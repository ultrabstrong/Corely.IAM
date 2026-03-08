# Account Encryption & Signature Keys — Current State

## Summary

Each account gets three keys at creation time: one AES symmetric encryption key, one RSA
asymmetric encryption key pair, and one ECDSA-SHA256 asymmetric signature key pair. All private
keys are encrypted with the system-level symmetric key before storage. The infrastructure is
in place (entities, EF configurations, mappers, generation), but no service or UI currently
exposes these keys for use. This document captures the current state as a foundation for
iterative improvement.

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
| Read back from DB | ✅ Yes (Include in AuthenticationProvider) | ❌ Never |
| Used by system | ✅ JWT signing/validation | ❌ Not used |
| Exposed via services | ✅ GetAsymmetricSignatureVerificationKeyAsync | ❌ Not exposed |
| Key rotation | ❌ Not implemented | ❌ Not implemented |
| Web UI | ❌ None | ❌ None |
