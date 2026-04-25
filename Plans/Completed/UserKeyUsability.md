# User Encryption & Signature Keys — Usability Plan

## Summary

Mirror the account key usability work for user keys. Each user already gets three keys at
creation time (AES symmetric, RSA asymmetric, ECDSA-SHA256 signature), all encrypted with
the system key. Like account keys, these are persisted but have no service-level retrieval
or web UI beyond the existing JWT signature verification (`GetAsymmetricSignatureVerificationKeyAsync`).

This plan adds retrieval service methods and a web UI for the **currently logged-in user only**.
Unlike account keys (which take an accountId parameter), user key methods are parameterless —
they always operate on the authenticated user's own keys via `UserContext`.

---

## What Exists Today

### Entities & Database

| Entity | Table | Fields |
|--------|-------|--------|
| `UserSymmetricKeyEntity` | `UserSymmetricKeys` | Id, UserId, KeyUsedFor, ProviderName, Version, EncryptedKey, CreatedUtc, ModifiedUtc |
| `UserAsymmetricKeyEntity` | `UserAsymmetricKeys` | Id, UserId, KeyUsedFor, ProviderName, Version, PublicKey, EncryptedPrivateKey, CreatedUtc, ModifiedUtc |

`UserEntity` has navigation properties: `ICollection<UserSymmetricKeyEntity>? SymmetricKeys`
and `ICollection<UserAsymmetricKeyEntity>? AsymmetricKeys`.

### Key Generation (UserProcessor.CreateUserAsync)

```csharp
user.SymmetricKeys = [_securityProcessor.GetSymmetricEncryptionKeyEncryptedWithSystemKey()];
user.AsymmetricKeys =
[
    _securityProcessor.GetAsymmetricEncryptionKeyEncryptedWithSystemKey(),
    _securityProcessor.GetAsymmetricSignatureKeyEncryptedWithSystemKey(),
];
```

### What Currently Consumes User Keys

| Consumer | Key Type | Usage |
|----------|----------|-------|
| `AuthenticationProvider` | Asymmetric signature key (full key pair) | Signs JWT tokens |
| `AuthenticationProvider` | Asymmetric signature public key | Validates JWT tokens |
| `UserProcessor.GetAsymmetricSignatureVerificationKeyAsync` | Asymmetric signature public key only | Exposed for external JWT verification |

**Not exposed**: Symmetric encryption provider, asymmetric encryption provider, full signature
provider (sign + verify with private key access).

### Reusable Infrastructure (from Account Key Work)

These components already exist and are shared between account and user key flows:

- `IIamSymmetricEncryptionProvider` / `IamSymmetricEncryptionProvider`
- `IIamAsymmetricEncryptionProvider` / `IamAsymmetricEncryptionProvider`
- `IIamAsymmetricSignatureProvider` / `IamAsymmetricSignatureProvider`
- `SecurityProvider.BuildSymmetricEncryptionProvider(SymmetricKey)`
- `SecurityProvider.BuildAsymmetricEncryptionProvider(AsymmetricKey)`
- `SecurityProvider.BuildAsymmetricSignatureProvider(AsymmetricKey)`
- `SymmetricKeyMapper` / `AsymmetricKeyMapper` (handle both user and account entities)

No new models, interfaces, or mappers needed — only new wiring at the processor/service/UI layers.

---

## Design Decisions

### Parameterless API

User key provider methods take no parameters. The logged-in user's ID is read from
`UserContext.User.Id` internally. This differs from the account pattern (which takes `accountId`
because users can belong to multiple accounts).

```csharp
// Account pattern (takes accountId)
Task<RetrieveSingleResult<IIamSymmetricEncryptionProvider>> GetAccountSymmetricEncryptionProviderAsync(Guid accountId);

// User pattern (parameterless — always the current user)
Task<RetrieveSingleResult<IIamSymmetricEncryptionProvider>> GetUserSymmetricEncryptionProviderAsync();
```

### Authorization Model

- **Service decorators**: `HasUserContext()` guard — must be authenticated
- **Processor decorator**: `HasUserContext()` guard — no CRUDX permission check needed since
  users are only accessing their own keys (not another user's)
- No `IsAuthorizedForOwnUser` / `IsAuthorizedAsync` check needed — the processor reads
  `UserContext.User.Id` directly, so there's no way to request another user's keys

### Web UI: Own-Profile Only

The Encryption & Signing section on `UserDetail.razor` is only visible when viewing your
own profile (`Id == UserContext?.User.Id`). Other users' detail pages show the existing
info (username, email, groups, roles, permissions) but not key operations.

### No Account Context Required

Account key methods check `HasAccountContext()` — the user must have selected an account.
User key methods only check `HasUserContext()` — authentication is sufficient. This means
user key operations are available even before selecting an account (e.g. from a direct URL).

### Relationship to Existing GetAsymmetricSignatureVerificationKeyAsync

`UserProcessor.GetAsymmetricSignatureVerificationKeyAsync(Guid userId)` serves a different
purpose: it returns the **public key only** for JWT verification and works for **any user**
(not just the current user). It should remain as-is. Our new `GetCurrentUserKeysAsync()`
returns the **full key entities** (including encrypted private keys) for the **current user
only**, enabling full encrypt/decrypt/sign/verify operations.

---

## Phase 1: User Key Provider Retrieval

### UserProcessor — GetCurrentUserKeysAsync

New method on `IUserProcessor` and `UserProcessor`:

```csharp
Task<GetResult<UserEntity>> GetCurrentUserKeysAsync();
```

Implementation:
- Reads `UserContext.User.Id` from `IUserContextProvider`
- Loads `UserEntity` with `.Include(u => u.SymmetricKeys).Include(u => u.AsymmetricKeys)`
- Returns `GetResult<UserEntity>` with the entity (or NotFoundError)

Decorators:
- **Authorization**: `HasUserContext()` guard
- **Telemetry**: Standard `ExecuteWithLoggingAsync` wrapper

### IRetrievalService — Three New Methods

```csharp
Task<RetrieveSingleResult<IIamSymmetricEncryptionProvider>> GetUserSymmetricEncryptionProviderAsync();
Task<RetrieveSingleResult<IIamAsymmetricEncryptionProvider>> GetUserAsymmetricEncryptionProviderAsync();
Task<RetrieveSingleResult<IIamAsymmetricSignatureProvider>> GetUserAsymmetricSignatureProviderAsync();
```

### RetrievalService Implementation

Each method follows the same pattern as the account equivalents:

1. Call `_userProcessor.GetCurrentUserKeysAsync()`
2. Find the matching key entity by `KeyUsedFor` (Encryption / Signature)
3. Map entity to model via `SymmetricKeyMapper.ToModel` / `AsymmetricKeyMapper.ToModel`
4. Call `_securityProvider.Build*Provider(key)` to build the wrapper
5. Return `RetrieveSingleResult`

### Decorator Implementations

**RetrievalServiceAuthorizationDecorator** — `HasUserContext()` guard for all three methods:
```csharp
public async Task<RetrieveSingleResult<IIamSymmetricEncryptionProvider>>
    GetUserSymmetricEncryptionProviderAsync() =>
    _authorizationProvider.HasUserContext()
        ? await _inner.GetUserSymmetricEncryptionProviderAsync()
        : new RetrieveSingleResult<IIamSymmetricEncryptionProvider>(
            RetrieveResultCode.UnauthorizedError,
            "Unauthorized to get user encryption provider",
            default, null);
```

**RetrievalServiceTelemetryDecorator** — Standard logging (no request params to log):
```csharp
public async Task<RetrieveSingleResult<IIamSymmetricEncryptionProvider>>
    GetUserSymmetricEncryptionProviderAsync() =>
    await _logger.ExecuteWithLoggingAsync(
        nameof(RetrievalService),
        new { },
        () => _inner.GetUserSymmetricEncryptionProviderAsync(),
        logResult: true);
```

### Tests

Mirror the account key test structure:

**UserProcessor tests:**
- `GetCurrentUserKeysAsync` — success (returns entity with keys)
- `GetCurrentUserKeysAsync` — user not found returns NotFoundError

**UserProcessor decorator tests:**
- Authorization: authorized calls inner, unauthorized returns error
- Telemetry: delegates to inner with logging

**RetrievalService tests (3 × 3 = 9):**
- Success path for each provider method
- User not found for each method
- Key not found for each method

**RetrievalService decorator tests (3 × 2 + 3 = 9):**
- Authorization: pass/fail for each method (6 tests)
- Telemetry: delegation + logging for each method (3 tests)

---

## Phase 2: Shared EncryptionSigningPanel Component & Web UI

### Shared Component: EncryptionSigningPanel.razor

Extract the entire Encryption & Signing tab section from `AccountDetail.razor` into a
reusable Blazor component at `Corely.IAM.Web/Components/Shared/EncryptionSigningPanel.razor`.
This eliminates duplication between the account and user pages.

**Component parameters:**

```razor
[Parameter] public IIamSymmetricEncryptionProvider? SymProvider { get; set; }
[Parameter] public IIamAsymmetricEncryptionProvider? AsymProvider { get; set; }
[Parameter] public IIamAsymmetricSignatureProvider? SigProvider { get; set; }
```

The parent page is responsible for:
- Authorization (PermissionView on accounts, `Id == UserContext.User.Id` check on users)
- Loading the providers (calling the appropriate service methods)
- Passing the loaded providers as parameters

The shared component owns:
- Tab navigation (Symmetric / Asymmetric / Digital Signature)
- All input/output textareas and action buttons
- Encrypt/Decrypt/ReEncrypt/Sign/Verify operations (calls directly on provider objects)
- Auto-sizing readonly textareas, click-to-select-all, copy buttons with feedback
- Per-tab error handling
- Loading state management for individual operations

The component injects `IJSRuntime` for clipboard operations. No service dependencies needed —
all crypto operations happen directly on the provider objects.

### AccountDetail.razor Refactor

Replace the existing inline Encryption & Signing section with:

```razor
<PermissionView RequiredPermissions="@(new[] { new RequiredPermission(AuthAction.Read, PermissionConstants.ACCOUNT_RESOURCE_TYPE) })">
    <div class="section-divider-label">Encryption &amp; Signing</div>
    <EncryptionSigningPanel SymProvider="_symProvider" AsymProvider="_asymProvider" SigProvider="_sigProvider" />
</PermissionView>
```

Provider loading in `LoadCoreAsync` stays as-is (sequential awaits to the account provider
service methods).

### UserDetail.razor — Encryption & Signing Section

Add after the Roles section and before EffectivePermissionsPanel, only when viewing own profile:

```razor
@if (Id == UserContext?.User.Id)
{
    <div class="section-divider-label">Encryption &amp; Signing</div>
    <EncryptionSigningPanel SymProvider="_symProvider" AsymProvider="_asymProvider" SigProvider="_sigProvider" />
}
```

No `PermissionView` wrapper needed since the section is only visible on your own profile
and the service methods enforce `HasUserContext()`.

### Provider Loading (UserDetail.razor)

Eagerly load all three providers in `LoadCoreAsync` (sequential awaits):

```csharp
if (Id == UserContext?.User.Id)
{
    await GetSymProviderAsync();
    await GetAsymProviderAsync();
    await GetSigProviderAsync();
}
```

Each getter calls the parameterless service method (e.g.,
`RetrievalService.GetUserSymmetricEncryptionProviderAsync()`).

---

## Out of Scope

- **Key rotation** — not yet implemented for either account or user keys
- **Cross-user key access** — explicitly excluded per requirements

---

## Phase 3: ConsoleTest & DevTools

### ConsoleTest Additions

Add a `USER KEY PROVIDERS` section to `Program.cs` after the existing account key demos.
Same pattern — full round-trip operations for all three provider types:

- **Symmetric encryption**: Encrypt a plaintext string, decrypt it back, verify round-trip match
- **Asymmetric encryption**: Same encrypt/decrypt/verify pattern with the RSA key pair
- **Asymmetric signature**: Sign a payload, verify the signature, verify a tampered payload is rejected

Uses the parameterless service methods (user context already set from the login step earlier
in the console flow).

### DevTools Commands

Three new subcommands under the `retrieval` command group, mirroring the account equivalents:

| Command | Description | Flags |
|---------|-------------|-------|
| `retrieval user-sym-encrypt` | Symmetric encryption using current user's key | `-e` encrypt, `-d` decrypt, `-r` re-encrypt |
| `retrieval user-asym-encrypt` | Asymmetric encryption using current user's key | `-e` encrypt, `-d` decrypt, `-r` re-encrypt |
| `retrieval user-asym-sign` | Asymmetric signature using current user's key | `-s` sign, `-v` verify (requires `--signature`) |

Key difference from account commands: **no accountId argument** — these operate on the
authenticated user's own keys. Still require authentication via `SetUserContextFromAuthTokenFileAsync`.

Files follow the existing naming convention in `Commands/Retrieval/`:
- `UserSymEncrypt.cs`
- `UserAsymEncrypt.cs`
- `UserAsymSign.cs`
