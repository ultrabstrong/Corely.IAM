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
    string ProviderTypeCode { get; }
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ReEncrypt(string ciphertext);
}

public interface IIamAsymmetricEncryptionProvider
{
    string ProviderTypeCode { get; }
    string PublicKey { get; }
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string ReEncrypt(string ciphertext);
}

public interface IIamAsymmetricSignatureProvider
{
    string ProviderTypeCode { get; }
    string PublicKey { get; }
    string Sign(string payload);
    bool Verify(string payload, string signature);
}
```

All interfaces expose `ProviderTypeCode` (the algorithm identifier, e.g., "AES", "RSA",
"ECDSA_SHA256"). Asymmetric interfaces also expose `PublicKey` for external use.

Implementations bind the key store to the provider at construction time and expose metadata:

```csharp
public class IamSymmetricEncryptionProvider(
    ISymmetricEncryptionProvider provider,
    ISymmetricKeyStoreProvider keyStore,
    string providerTypeCode) : IIamSymmetricEncryptionProvider
{
    public string ProviderTypeCode => providerTypeCode;
    public string Encrypt(string plaintext) => provider.Encrypt(plaintext, keyStore);
    public string Decrypt(string ciphertext) => provider.Decrypt(ciphertext, keyStore);
    public string ReEncrypt(string ciphertext) => provider.ReEncrypt(ciphertext, keyStore);
}
```

Asymmetric implementations additionally accept and expose `publicKey`.

### Layer-by-Layer Implementation

**1. AccountProcessor** — `GetAccountKeysAsync(Guid accountId)`:
- Loads the account entity with `.Include(a => a.SymmetricKeys).Include(a => a.AsymmetricKeys)`
- Access control: user must belong to the account (checked via `UserContext.AvailableAccounts`)
- Authorization decorator: `AuthAction.Read` on `ACCOUNT_RESOURCE_TYPE`
- Telemetry decorator: standard `ExecuteWithLoggingAsync` wrapper

**2. SecurityProvider** — three `Build*Provider` methods:
- Take key models, decrypt private keys using `ISymmetricEncryptedValue.GetDecrypted(systemKeyStore)`
  (uses the model's built-in decryption rather than the standalone `DecryptWithSystemKey` method)
- Hydrate `InMemorySymmetricKeyStoreProvider` / `InMemoryAsymmetricKeyStoreProvider`
- Use `GetProvider(key.ProviderTypeCode)` (not `GetDefaultProvider()`) because key material is
  algorithm-specific — the provider must match the algorithm that generated the key
- Return `IIam*Provider` wrappers binding provider + key store + metadata (type code, public key)

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

## Phase 2: ConsoleTest Usage Example & DevTools Commands ✅ Implemented

### ConsoleTest Additions

Added a new `ACCOUNT KEY PROVIDERS` section to `Program.cs` after the existing retrieval demos.
Demonstrates full round-trip operations for all three provider types:

- **Symmetric encryption**: Encrypt a plaintext string, decrypt it back, verify round-trip match
- **Asymmetric encryption**: Same encrypt/decrypt/verify pattern with the RSA key pair
- **Asymmetric signature**: Sign a payload, verify the signature, verify a tampered payload is rejected

### DevTools Commands

Three new subcommands under the `retrieval` command group:

| Command | Description | Flags |
|---------|-------------|-------|
| `retrieval account-sym-encrypt <accountId>` | Symmetric encryption using account key | `-e` encrypt, `-d` decrypt, `-r` re-encrypt |
| `retrieval account-asym-encrypt <accountId>` | Asymmetric encryption using account key | `-e` encrypt, `-d` decrypt, `-r` re-encrypt |
| `retrieval account-asym-sign <accountId>` | Asymmetric signature using account key | `-s` sign, `-v` verify (requires `--signature`) |

All commands:
- Require authentication (reads token from auth token file via `SetUserContextFromAuthTokenFileAsync`)
- Show help with a warning if no operation flag is provided
- Display error message if provider retrieval fails (account not found, key not found, etc.)
- Follow the existing `Retrieval` partial class / nested class pattern for auto-discovery

## Phase 3: Web UI — Account Encryption & Signing ✅ Implemented

### AccountDetail.razor — Encryption & Signing Section

Added a new "Encryption & Signing" section to the account detail page between the Users section
and EffectivePermissionsPanel. Uses Blazor-managed tabs (no Bootstrap JS dependency) for three
operation types:

| Tab | Operations | UI Pattern |
|-----|------------|------------|
| Symmetric Encryption | Encrypt, Decrypt, Re-encrypt | Algorithm badge → input → buttons → output |
| Asymmetric Encryption | Encrypt, Decrypt, Re-encrypt | Algorithm badge → public key display → input → buttons → output |
| Digital Signature | Sign, Verify | Algorithm badge → public key display → payload + signature → buttons → result alert |

Each tab shows:
- **Algorithm badge** (e.g., `AES-256-CBC-PKCS7`) via `ProviderName` from the `IIam*` interface
- **Public key** (asymmetric tabs only) in a readonly textarea for external use
- **Input/output** forms for the crypto operations

### IIam* Interface Enhancements

All three interfaces expose `ProviderName` for algorithm identification.
Asymmetric interfaces additionally expose `PublicKey`:

- `IIamSymmetricEncryptionProvider` — added `string ProviderName { get; }`
- `IIamAsymmetricEncryptionProvider` — added `string ProviderName { get; }`, `string PublicKey { get; }`
- `IIamAsymmetricSignatureProvider` — added `string ProviderName { get; }`, `string PublicKey { get; }`

### Key Decryption Refactor

`SecurityProvider.Build*Provider` methods now use the model's built-in decryption
(`ISymmetricEncryptedValue.GetDecrypted(systemKeyStore)`) instead of calling the standalone
`DecryptWithSystemKey` method. This uses the API as designed rather than reimplementing the
same logic separately.

**Key design decisions:**

- **Permission-gated**: Entire section wrapped in `PermissionView` requiring `Read` on
  `ACCOUNT_RESOURCE_TYPE` — same permission that `GetAccountKeysAsync` enforces
- **Eager provider loading**: Providers fetched in `LoadCoreAsync` at page init and cached.
  All three providers load sequentially (EF Core DbContext cannot run concurrent queries)
  so the algorithm badge and description show immediately without requiring a user action.
- **Isolated error handling**: Key operation errors use per-tab error state (`_symError`,
  `_asymError`, `_sigError`) instead of the page-level `_message` to avoid interfering with
  other page operations (edit, delete, invitations, etc.)
- **Decrypt/verify wrapped in try-catch**: Invalid ciphertext or signature inputs show friendly
  error messages instead of crashing the page
- **Signature flow**: Sign populates the signature textarea; Verify requires both payload and
  signature. Result uses color-coded alerts (green = valid, red = invalid).

### _Imports.razor

Added `@using Corely.IAM.Security.Models` to make `IIam*Provider` interfaces available to all
Blazor components without per-file imports.

---

## Corely.Security 1.0.2 Package Update ✅ Implemented

Upgraded `Corely.Security` from 1.0.1 → 1.0.2 across all four projects that reference it.

### Breaking API Changes

`ProviderTypeCode` was removed from all Corely.Security provider interfaces and replaced with
`ProviderName` (a human-readable algorithm string, e.g., `"AES-256-CBC-PKCS7"`). A new
`ProviderDescription` virtual property (defaulting to `GetType().Name`) was also added.

All references migrated across:
- IAM models, entities, interfaces (`SymmetricKey`, `AsymmetricKey`, `IIam*Provider`)
- Concrete IAM provider implementations (`IamSymmetricEncryptionProvider`, etc.)
- `SecurityProvider` and `AuthenticationProvider`
- Entity configurations and validators
- Mappers (`SymmetricKeyMapper`, `AsymmetricKeyMapper`)
- DevTools commands (`sym-encrypt`, `asym-encrypt`, `sym-sign`, `asym-sign`, `hash`)
- Web UI (`AccountDetail.razor`)
- All affected unit tests

### EF Core Migrations

Added `RenameProviderTypeCodeToProviderName` migration across all three DB providers
(MySQL, MariaDB, SQL Server). Uses EF's `RenameColumn` — safe, no data loss.

### ProviderDescription Added to IIam* Interfaces

`ProviderDescription` exposed through all three IAM provider interfaces and delegated to
the underlying Corely.Security provider at the concrete implementation level:

```csharp
public string ProviderDescription => provider.ProviderDescription;
```

---

## Auth Bug Fix ✅ Implemented

### Problem

Unauthenticated users navigating to `/` saw the "Select an Account" dashboard block instead
of being redirected to sign-in. `Home.razor` was missing `@attribute [Authorize]`, so
`AuthorizeRouteView` allowed unauthenticated rendering. When `_userContext` was null, the
`null == 0` condition evaluated to `false`, falling through to the else block.

### Fix

- Added `@attribute [Authorize]` to `Corely.IAM.WebApp/Components/Pages/Home.razor`
- Added `@attribute [Authorize]` to `Corely.IAM.Web/Components/_Imports.razor` — applies
  globally to all 11 Blazor pages in the Web component library
- Removed the now-unreachable State 3 else block from `Home.razor`

---

## Phase 3 Follow-Up: UI Polish ✅ Implemented

Additional UI improvements to `AccountDetail.razor` after initial Phase 3 delivery.

### Provider Info on Page Load

Previously the algorithm badge only appeared after the user clicked Encrypt/Sign/etc. (lazy
load). Providers are now loaded eagerly in `LoadCoreAsync`, so the badge and description show
immediately when the page opens. Sequential `await` calls required (not `Task.WhenAll`)
due to EF Core's single-operation-per-DbContext constraint.

### ProviderDescription Display

Added `ProviderDescription` as subtle muted text next to each algorithm badge:

```razor
<span class="badge bg-secondary">@_symProvider.ProviderName</span>
<small class="text-muted ms-2">@_symProvider.ProviderDescription</small>
```

### Auto-Sizing Read-Only Textareas

Public key boxes and output boxes use `field-sizing: content` (CSS) with `rows="1"` so they
start compact and grow to fit their content without a scrollbar. `resize: none` prevents
manual resizing on display-only fields.

### Click-to-Select-All

All readonly textareas have `onfocus="this.select()"` — clicking or tabbing into a public key
or output box immediately selects all text for easy copying.

### Copy Buttons

Each readonly box (public keys + outputs) has an inline copy button (`bi-copy` icon) that:
- Copies text to the clipboard via `navigator.clipboard.writeText`
- Briefly shows a green `bi-check` for 1.5 s then reverts to `bi-copy`
- Implemented via a shared `CopyWithFeedbackAsync` helper to avoid repetition

