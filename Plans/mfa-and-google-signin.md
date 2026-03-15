# Plan: MFA (TOTP) and Google Sign-In

## Status: Complete

## Overview

Add multi-factor authentication (TOTP authenticator apps) and Google OAuth sign-in to Corely.IAM. Users can:

- **Enable/disable TOTP MFA** on their account — when enabled, sign-in requires a TOTP code after password
- **Link/unlink a Google account** — allows sign-in with Google as an alternative to username/password
- **Use both or either** — a user can have BasicAuth + Google linked simultaneously, or just one
- **MFA applies to all sign-in methods** — if TOTP is enabled, it's required whether signing in with password or Google

---

## Design Decisions

### Authentication Method Model

A user can have **zero or more** authentication methods:

| Method | Entity | Relationship | Required? |
|--------|--------|-------------|-----------|
| Password | `BasicAuthEntity` | 1:1 with User | No (but at least one method must exist) |
| Google | `GoogleAuthEntity` | 1:1 with User | No |

At least one authentication method must be linked to a user at all times. The UI and service layer prevent unlinking the last method.

### MFA Model

MFA is **per-user, not per-account**. A single `TotpAuthEntity` stores the TOTP secret. When TOTP is enabled:

- Password sign-in returns `MfaRequiredChallenge` instead of a token
- Google sign-in returns `MfaRequiredChallenge` instead of a token
- The caller must complete the challenge with `VerifyMfaAsync()`
- Only then is the JWT issued

### Two-Phase Sign-In Flow

```
Phase 1: Credential verification
  SignInAsync(username, password, deviceId)     → Success or MfaRequiredChallenge
  SignInWithGoogleAsync(idToken, deviceId)      → Success or MfaRequiredChallenge

Phase 2: MFA verification (only if TOTP enabled)
  VerifyMfaAsync(mfaChallengeToken, totpCode)   → Success (JWT issued)
```

The `MfaRequiredChallenge` result includes a short-lived, single-use **MFA challenge token** (not a JWT — just a random token stored in the DB) that ties the MFA verification back to the authenticated user. This prevents replay attacks and decouples the two phases.

### Recovery Codes

When TOTP is enabled, 10 single-use recovery codes are generated. Each code can be used exactly once in place of a TOTP code. Users can regenerate codes (invalidates all existing ones).

### Google OAuth Flow (Server-Side)

Corely.IAM does **not** implement the OAuth redirect flow itself — that's the host app's responsibility (or the Web UI's). The library accepts a **Google ID token** (the JWT Google returns after consent) and validates it server-side:

1. Host/Web UI handles the Google OAuth redirect and consent
2. Google returns an ID token to the callback
3. Host calls `SignInWithGoogleAsync(googleIdToken, deviceId)`
4. Corely.IAM validates the ID token (signature, audience, expiry)
5. Looks up user by Google subject ID
6. Issues JWT (or MFA challenge if TOTP enabled)

This keeps Corely.IAM host-agnostic — no `HttpContext`, no redirect URLs, no cookie manipulation at the library level.

---

## Database Schema

### New Tables

#### `TotpAuths`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `GUID` | PK, ValueGeneratedNever | UUIDv7 |
| `UserId` | `GUID` | FK → Users, Unique, Cascade Delete | One TOTP config per user |
| `EncryptedSecret` | `nvarchar(500)` | Required | TOTP secret encrypted with system key |
| `IsEnabled` | `bit` | Required, Default: false | Whether TOTP is actively enforced |
| `CreatedUtc` | `DATETIME2` | Required, Default: SYSUTCDATETIME() | |
| `ModifiedUtc` | `DATETIME2` | Nullable | |

#### `TotpRecoveryCodes`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `GUID` | PK, ValueGeneratedNever | UUIDv7 |
| `TotpAuthId` | `GUID` | FK → TotpAuths, Cascade Delete | Parent TOTP config |
| `CodeHash` | `nvarchar(250)` | Required | Salted hash of the recovery code |
| `UsedUtc` | `DATETIME2` | Nullable | Null = unused, set when consumed |
| `CreatedUtc` | `DATETIME2` | Required, Default: SYSUTCDATETIME() | |

#### `GoogleAuths`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `GUID` | PK, ValueGeneratedNever | UUIDv7 |
| `UserId` | `GUID` | FK → Users, Unique, Cascade Delete | One Google link per user |
| `GoogleSubjectId` | `nvarchar(255)` | Required, Unique Index | Google `sub` claim (stable user identifier) |
| `Email` | `nvarchar(254)` | Required | Google email (informational, not used for lookup) |
| `CreatedUtc` | `DATETIME2` | Required, Default: SYSUTCDATETIME() | |
| `ModifiedUtc` | `DATETIME2` | Nullable | |

#### `MfaChallenges`

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| `Id` | `GUID` | PK, ValueGeneratedNever | UUIDv7 |
| `UserId` | `GUID` | FK → Users, Cascade Delete | User being challenged |
| `ChallengeToken` | `nvarchar(128)` | Required, Unique Index | Random token for MFA verification |
| `DeviceId` | `nvarchar(100)` | Required | Device from original sign-in |
| `AccountId` | `GUID` | Nullable | Account from original sign-in (if specified) |
| `ExpiresUtc` | `DATETIME2` | Required | Short-lived (5 minutes default) |
| `CompletedUtc` | `DATETIME2` | Nullable | Null = pending, set when verified |
| `CreatedUtc` | `DATETIME2` | Required, Default: SYSUTCDATETIME() | |

Index on `ExpiresUtc` for cleanup queries (same pattern as `UserAuthTokens`).

### Modified Tables

#### `Users` — No schema changes

MFA status is derived from `TotpAuths.IsEnabled`. No denormalization needed.

---

## Domain Structure

### New Domain: `TotpAuths/`

```
TotpAuths/
├── Constants/
│   └── TotpAuthConstants.cs
├── Entities/
│   ├── TotpAuthEntity.cs
│   ├── TotpAuthEntityConfiguration.cs
│   ├── TotpRecoveryCodeEntity.cs
│   └── TotpRecoveryCodeEntityConfiguration.cs
├── Models/
│   ├── TotpAuth.cs
│   ├── EnableTotpRequest.cs
│   ├── EnableTotpResult.cs
│   ├── DisableTotpRequest.cs
│   ├── DisableTotpResult.cs
│   ├── VerifyTotpRequest.cs
│   ├── VerifyTotpResult.cs
│   └── RegenerateTotpRecoveryCodesResult.cs
├── Processors/
│   ├── ITotpAuthProcessor.cs
│   ├── TotpAuthProcessor.cs
│   ├── TotpAuthProcessorAuthorizationDecorator.cs
│   └── TotpAuthProcessorTelemetryDecorator.cs
├── Mappers/
│   └── TotpAuthMapper.cs
├── Validators/
│   └── TotpAuthValidator.cs
└── Providers/
    ├── ITotpProvider.cs
    └── TotpProvider.cs          # TOTP generation/validation (RFC 6238)
```

### New Domain: `GoogleAuths/`

```
GoogleAuths/
├── Constants/
│   └── GoogleAuthConstants.cs
├── Entities/
│   ├── GoogleAuthEntity.cs
│   └── GoogleAuthEntityConfiguration.cs
├── Models/
│   ├── GoogleAuth.cs
│   ├── LinkGoogleAuthRequest.cs
│   ├── LinkGoogleAuthResult.cs
│   ├── UnlinkGoogleAuthRequest.cs
│   ├── UnlinkGoogleAuthResult.cs
│   └── GoogleIdTokenPayload.cs     # Parsed Google ID token claims
├── Processors/
│   ├── IGoogleAuthProcessor.cs
│   ├── GoogleAuthProcessor.cs
│   ├── GoogleAuthProcessorAuthorizationDecorator.cs
│   └── GoogleAuthProcessorTelemetryDecorator.cs
├── Mappers/
│   └── GoogleAuthMapper.cs
├── Validators/
│   └── GoogleAuthValidator.cs
└── Providers/
    ├── IGoogleIdTokenValidator.cs
    └── GoogleIdTokenValidator.cs   # Validates Google JWT (signature, audience, expiry)
```

### New Domain: `MfaChallenges/`

```
MfaChallenges/
├── Constants/
│   └── MfaChallengeConstants.cs
├── Entities/
│   ├── MfaChallengeEntity.cs
│   └── MfaChallengeEntityConfiguration.cs
└── Models/
    └── MfaChallenge.cs
```

MFA challenges are simple — no processor needed. Created and consumed by `AuthenticationService` directly.

---

## Service Interface Changes

### `IAuthenticationService` — Extended

```csharp
public interface IAuthenticationService
{
    // Existing
    Task<SignInResult> SignInAsync(SignInRequest request);
    Task<SignInResult> SwitchAccountAsync(SwitchAccountRequest request);
    Task<bool> SignOutAsync(SignOutRequest request);
    Task SignOutAllAsync();

    // New: Google sign-in
    Task<SignInResult> SignInWithGoogleAsync(SignInWithGoogleRequest request);

    // New: MFA verification (completes a two-phase sign-in)
    Task<SignInResult> VerifyMfaAsync(VerifyMfaRequest request);
}
```

### New Request/Result Models

```csharp
// Google sign-in
public record SignInWithGoogleRequest(
    string GoogleIdToken,       // JWT from Google OAuth
    string DeviceId,
    Guid? AccountId = null
);

// MFA verification
public record VerifyMfaRequest(
    string MfaChallengeToken,   // From MfaRequiredChallenge result
    string Code                 // TOTP code or recovery code
);
```

### `SignInResultCode` — Extended

```csharp
public enum SignInResultCode
{
    Success,
    MfaRequiredChallenge,       // NEW: credential verified, MFA needed
    UserNotFoundError,
    UserLockedError,
    PasswordMismatchError,
    SignatureKeyNotFoundError,
    AccountNotFoundError,
    InvalidAuthTokenError,
    GoogleAuthNotLinkedError,   // NEW: no Google link for this Google account
    InvalidGoogleTokenError,    // NEW: Google ID token validation failed
    InvalidMfaCodeError,        // NEW: TOTP code or recovery code invalid
    MfaChallengeExpiredError,   // NEW: challenge token expired or already used
}
```

### `SignInResult` — Extended

```csharp
public record SignInResult(
    SignInResultCode ResultCode,
    string? Message,
    string? AuthToken,
    Guid? AuthTokenId,
    string? MfaChallengeToken = null   // NEW: populated when ResultCode == MfaRequiredChallenge
);
```

### `IRegistrationService` — Extended

```csharp
// New methods
Task<EnableTotpResult> EnableTotpAsync();                           // Generates secret + recovery codes
Task<DisableTotpResult> DisableTotpAsync(DisableTotpRequest request); // Requires current TOTP code to disable
Task<RegenerateTotpRecoveryCodesResult> RegenerateTotpRecoveryCodesAsync();
Task<LinkGoogleAuthResult> LinkGoogleAuthAsync(LinkGoogleAuthRequest request);
```

### `IDeregistrationService` — Extended

```csharp
// New methods
Task<UnlinkGoogleAuthResult> UnlinkGoogleAuthAsync();
```

### `IRetrievalService` — Extended

```csharp
// New methods
Task<GetTotpStatusResult> GetTotpStatusAsync();                    // Is TOTP enabled? How many recovery codes left?
Task<GetAuthMethodsResult> GetAuthMethodsAsync();                  // Which auth methods are linked?
```

---

## TOTP Implementation Details

### TOTP Provider (`ITotpProvider`)

```csharp
internal interface ITotpProvider
{
    string GenerateSecret();                           // Random 20-byte base32 secret
    string GenerateSetupUri(string secret,             // otpauth:// URI for QR code
        string issuer, string userLabel);
    bool ValidateCode(string secret, string code);     // RFC 6238 validation (±1 step tolerance)
}
```

Implementation uses `System.Security.Cryptography.HMACSHA1` directly (standard .NET BCL). No Corely.Security dependency for TOTP — this is a standalone RFC 6238 implementation:

- **Algorithm**: HMAC-SHA1 (standard for Google Authenticator compatibility)
- **Digits**: 6
- **Period**: 30 seconds
- **Tolerance**: ±1 step (accepts codes from 30 seconds ago and 30 seconds in the future)
- **Secret**: 20 bytes, base32-encoded for QR code URI

### Secret Storage

TOTP secrets are encrypted with the **system symmetric key** before storage (same pattern as user/account key material). The `EncryptedSecret` column stores the ciphertext. Decryption happens in-memory only during code validation.

### Recovery Codes

- 10 codes generated on enable
- Each code: 8 alphanumeric characters (e.g., `A1B2-C3D4`)
- Stored as salted hashes (same hash provider as passwords)
- Single-use: `UsedUtc` set on consumption
- Regeneration creates 10 new codes and deletes all existing ones

---

## Google OAuth Implementation Details

### Google ID Token Validation (`IGoogleIdTokenValidator`)

```csharp
internal interface IGoogleIdTokenValidator
{
    Task<GoogleIdTokenPayload?> ValidateAsync(string idToken);
}
```

Validates the Google-issued JWT:

1. **Fetch Google's public keys** from `https://www.googleapis.com/oauth2/v3/certs` (cached with expiry)
2. **Validate JWT signature** using Google's public RSA keys
3. **Check `iss`**: must be `accounts.google.com` or `https://accounts.google.com`
4. **Check `aud`**: must match configured `GoogleClientId`
5. **Check `exp`**: must not be expired
6. **Extract claims**: `sub` (stable Google user ID), `email`, `email_verified`

Returns `GoogleIdTokenPayload` with the validated claims, or null on failure.

### Google Configuration

Add to `SecurityOptions`:

```csharp
public class SecurityOptions
{
    // Existing
    public int MaxLoginAttempts { get; set; } = 5;
    public int LockoutCooldownSeconds { get; set; } = 900;
    public int AuthTokenTtlSeconds { get; set; } = 3600;

    // New
    public int MfaChallengeTimeoutSeconds { get; set; } = 300;       // 5 minutes
    public int TotpRecoveryCodeCount { get; set; } = 10;
    public string? GoogleClientId { get; set; }                      // Required for Google sign-in
}
```

`GoogleClientId` is nullable — Google sign-in is only available when configured. The `GoogleIdTokenValidator` returns an error if called without a configured client ID.

No client secret needed — ID token validation is public-key-based using Google's published JWKS.

### Linking Flow

1. User signs in with existing method (password or already-linked Google)
2. User navigates to Profile and clicks "Link Google Account"
3. Host/Web UI handles Google OAuth consent → gets ID token
4. Calls `LinkGoogleAuthAsync(idToken)` with current user context
5. Library validates token, extracts `sub` claim, creates `GoogleAuthEntity`
6. If `sub` is already linked to another user → returns error

### Sign-In Flow

1. Web UI shows "Sign in with Google" button
2. Google OAuth consent → ID token returned to callback
3. Calls `SignInWithGoogleAsync(googleIdToken, deviceId)`
4. Library validates token, looks up user by `GoogleSubjectId`
5. If user has TOTP enabled → returns `MfaRequiredChallenge`
6. Otherwise → issues JWT

---

## Authentication Flow Diagrams

### Password Sign-In (with MFA)

```
Client                          AuthenticationService
  │                                       │
  │  SignInAsync(user, pass, device)       │
  │──────────────────────────────────────►│
  │                                       │── Verify password (BasicAuthProcessor)
  │                                       │── Check TOTP enabled (TotpAuthEntity)
  │                                       │
  │  ◄─ IF no TOTP: Success + JWT ────────│
  │                                       │
  │  ◄─ IF TOTP: MfaRequiredChallenge ────│── Create MfaChallengeEntity (5 min TTL)
  │     (includes mfaChallengeToken)      │
  │                                       │
  │  VerifyMfaAsync(challengeToken, code) │
  │──────────────────────────────────────►│
  │                                       │── Validate challenge (not expired, not used)
  │                                       │── Validate TOTP code OR recovery code
  │                                       │── Mark challenge completed
  │                                       │── Issue JWT
  │  ◄─ Success + JWT ───────────────────│
```

### Google Sign-In (with MFA)

```
Client              Web UI / Host            AuthenticationService
  │                      │                            │
  │  Click "Google"      │                            │
  │─────────────────────►│                            │
  │                      │── Google OAuth redirect ──►│ (Google)
  │                      │◄── ID token callback ─────│
  │                      │                            │
  │                      │  SignInWithGoogleAsync()    │
  │                      │───────────────────────────►│
  │                      │                            │── Validate Google ID token
  │                      │                            │── Lookup user by GoogleSubjectId
  │                      │                            │── Check TOTP enabled
  │                      │                            │
  │                      │  ◄─ MfaRequiredChallenge ──│
  │                      │                            │
  │  Enter TOTP code     │                            │
  │─────────────────────►│                            │
  │                      │  VerifyMfaAsync()          │
  │                      │───────────────────────────►│
  │                      │  ◄─ Success + JWT ─────────│
```

### Enable TOTP

```
Client                     Profile Page                   RegistrationService
  │                              │                               │
  │  Click "Enable 2FA"         │                               │
  │─────────────────────────────►│                               │
  │                              │  EnableTotpAsync()            │
  │                              │──────────────────────────────►│
  │                              │                               │── Generate secret
  │                              │                               │── Encrypt with system key
  │                              │                               │── Create TotpAuthEntity (IsEnabled=false)
  │                              │                               │── Generate recovery codes
  │                              │                               │── Hash and store codes
  │                              │  ◄─ EnableTotpResult ─────────│
  │                              │     (secret, setupUri,        │
  │                              │      recoveryCodes[])         │
  │                              │                               │
  │  ◄─ Show QR code + codes ──│                               │
  │                              │                               │
  │  Scan QR, enter TOTP code   │                               │
  │─────────────────────────────►│                               │
  │                              │  ConfirmTotpAsync(code)       │
  │                              │──────────────────────────────►│
  │                              │                               │── Validate code against secret
  │                              │                               │── Set IsEnabled = true
  │                              │  ◄─ Success ─────────────────│
```

Note: `EnableTotpAsync` creates the TOTP record with `IsEnabled = false`. A separate `ConfirmTotpAsync` call validates a code and flips `IsEnabled = true`. This ensures the user has actually configured their authenticator app before enforcement begins.

---

## Service Method Details

### `AuthenticationService.SignInAsync` — Modified

After successful password verification (line 107 in current code), before calling `GenerateAuthTokenAndSetContextAsync`:

```csharp
// Check if TOTP is enabled for this user
var totpAuth = await _totpAuthRepo.GetAsync(t => t.UserId == userEntity.Id);
if (totpAuth is { IsEnabled: true })
{
    // Create MFA challenge instead of issuing token
    var challenge = await CreateMfaChallengeAsync(
        userEntity.Id, request.DeviceId, request.AccountId);
    return new SignInResult(
        SignInResultCode.MfaRequiredChallenge,
        "MFA verification required",
        null, null,
        challenge.ChallengeToken);
}
```

### `AuthenticationService.SignInWithGoogleAsync` — New

```
1. Validate Google ID token via IGoogleIdTokenValidator
2. If invalid → return InvalidGoogleTokenError
3. Lookup GoogleAuthEntity by GoogleSubjectId
4. If not found → return GoogleAuthNotLinkedError
5. Load UserEntity from GoogleAuthEntity.UserId
6. Check lockout status (same as password flow)
7. Update login metrics (TotalSuccessfulLogins, etc.)
8. Check TOTP → if enabled, return MfaRequiredChallenge
9. Otherwise → GenerateAuthTokenAndSetContextAsync → return Success
```

### `AuthenticationService.VerifyMfaAsync` — New

```
1. Lookup MfaChallengeEntity by ChallengeToken
2. If not found or expired → return MfaChallengeExpiredError
3. If already completed → return MfaChallengeExpiredError
4. Load TotpAuthEntity for challenge.UserId
5. Try TOTP validation first (ITotpProvider.ValidateCode)
6. If TOTP fails, try recovery code (hash and compare against unused codes)
7. If both fail → return InvalidMfaCodeError
8. Mark challenge as completed (CompletedUtc = now)
9. If recovery code used → mark code as used (UsedUtc = now)
10. GenerateAuthTokenAndSetContextAsync with challenge.UserId, challenge.AccountId, challenge.DeviceId
11. Return Success + JWT
```

### `RegistrationService.EnableTotpAsync` — New

```
1. Requires user context (own user only)
2. Check if TotpAuthEntity already exists for user
3. If exists and IsEnabled → return AlreadyEnabledError
4. Generate TOTP secret via ITotpProvider
5. Encrypt secret with system key
6. Create TotpAuthEntity (IsEnabled = false)
7. Generate recovery codes, hash, store as TotpRecoveryCodeEntities
8. Return EnableTotpResult(secret, setupUri, recoveryCodes[])
```

### `RegistrationService.ConfirmTotpAsync` — New

```
1. Requires user context (own user only)
2. Load TotpAuthEntity
3. If not found or already enabled → return error
4. Decrypt secret, validate provided TOTP code
5. If valid → set IsEnabled = true
6. Return success
```

### `RegistrationService.LinkGoogleAuthAsync` — New

```
1. Requires user context (own user only)
2. Validate Google ID token
3. Check GoogleSubjectId not already linked to another user
4. Create GoogleAuthEntity
5. Return success
```

---

## Corely.IAM.Web Changes

### Sign-In Page (`SignIn.cshtml`)

Add "Sign in with Google" button below the password form:

```html
<hr class="my-3" />
<form method="post" asp-page-handler="Google">
    <button type="submit" class="btn btn-outline-dark w-100">
        <img src="google-icon.svg" width="20" /> Sign in with Google
    </button>
</form>
```

The Google button initiates the OAuth redirect. The callback page handles the token exchange and calls `SignInWithGoogleAsync`.

### New Razor Page: MFA Verification (`VerifyMfa.cshtml`)

Simple form between sign-in and dashboard:

- Title: "Two-Factor Authentication"
- Single input: 6-digit TOTP code
- Hidden field: `mfaChallengeToken`
- Link: "Use a recovery code instead" → toggles to recovery code input
- POST handler calls `VerifyMfaAsync`
- On success → set cookies, redirect to dashboard/select-account

### New Razor Page: Google Callback (`GoogleCallback.cshtml`)

Handles the OAuth callback:

1. Receives authorization code from Google
2. Exchanges code for tokens (via Google's token endpoint)
3. Calls `SignInWithGoogleAsync(idToken, deviceId)`
4. Handles `MfaRequiredChallenge` → redirect to `/verify-mfa`
5. Handles `Success` → set cookies, redirect

### Profile Page (`Profile.razor`) — Extended

Add two new sections:

#### Two-Factor Authentication Section

- **When disabled**: "Enable Two-Factor Authentication" button
  - Clicking it calls `EnableTotpAsync`
  - Shows QR code (rendered from `setupUri`) and recovery codes
  - Requires confirmation code before activation
- **When enabled**: Shows status, recovery code count, and action buttons:
  - "Regenerate Recovery Codes" → calls `RegenerateTotpRecoveryCodesAsync`
  - "Disable Two-Factor Authentication" → requires current TOTP code

#### Linked Accounts Section

- Shows linked Google account (email) if linked
- "Link Google Account" button (if not linked)
- "Unlink Google Account" button (if linked, and another auth method exists)

### New `AppRoutes` Constants

```csharp
public const string VERIFY_MFA = "/verify-mfa";
public const string GOOGLE_CALLBACK = "/google-callback";
```

---

## DevTools Changes

### New Auth Commands

```
auth signin-google <id-token-file> <device-id>     # Sign in with Google ID token from file
auth verify-mfa <challenge-token> <totp-code>       # Complete MFA verification
```

### New TOTP Commands

```
totp enable                     # Enable TOTP, outputs secret + setup URI + recovery codes
totp confirm <code>             # Confirm TOTP setup with a code from authenticator
totp disable <code>             # Disable TOTP (requires current code)
totp status                     # Show TOTP status and remaining recovery codes
totp regenerate-codes           # Regenerate recovery codes
totp generate-code <secret>     # (Standalone) Generate a TOTP code from a secret (for testing)
totp validate-code <secret> <code>  # (Standalone) Validate a TOTP code (for testing)
```

The last two commands (`generate-code`, `validate-code`) are standalone crypto utilities (no database) — useful for development and testing.

### New Google Commands

```
google link <id-token-file>     # Link Google account to current user
google unlink                   # Unlink Google account
google status                   # Show linked Google account info
```

---

## ConsoleTest Changes

Add a new demo section after the existing key provider demos:

### Phase: MFA Demo

```csharp
// Enable TOTP
var enableResult = await registrationService.EnableTotpAsync();
// enableResult.Secret, enableResult.SetupUri, enableResult.RecoveryCodes

// Confirm with generated code (use TotpProvider directly for demo)
var totpProvider = serviceProvider.GetRequiredService<ITotpProvider>();
var code = totpProvider.GenerateCode(enableResult.Secret);
await registrationService.ConfirmTotpAsync(new ConfirmTotpRequest(code));

// Sign in — now requires MFA
var signInResult = await authService.SignInAsync(signInRequest);
// signInResult.ResultCode == MfaRequiredChallenge

// Verify MFA
var mfaCode = totpProvider.GenerateCode(enableResult.Secret);
var mfaResult = await authService.VerifyMfaAsync(
    new VerifyMfaRequest(signInResult.MfaChallengeToken!, mfaCode));
// mfaResult.ResultCode == Success, mfaResult.AuthToken != null

// Use a recovery code
var recoveryResult = await authService.VerifyMfaAsync(
    new VerifyMfaRequest(challengeToken, enableResult.RecoveryCodes[0]));

// Disable TOTP
var disableCode = totpProvider.GenerateCode(enableResult.Secret);
await registrationService.DisableTotpAsync(new DisableTotpRequest(disableCode));
```

Note: For ConsoleTest, `ITotpProvider` needs to be `public` or the demo needs a wrapper. Consider making `ITotpProvider` public since DevTools also needs it, or expose `GenerateCode` through a service method.

---

## Configuration

### `appsettings.json` additions

```json
{
  "SecurityOptions": {
    "MfaChallengeTimeoutSeconds": 300,
    "TotpRecoveryCodeCount": 10,
    "GoogleClientId": "your-google-client-id.apps.googleusercontent.com"
  }
}
```

### `appsettings.template.json` additions

```json
{
  "SecurityOptions": {
    "MfaChallengeTimeoutSeconds": 300,
    "TotpRecoveryCodeCount": 10,
    "GoogleClientId": ""
  }
}
```

---

## Test Plan

### Unit Tests (Corely.IAM.UnitTests)

#### TotpProvider Tests
- `GenerateSecret_ReturnsBase32String_Of20Bytes`
- `GenerateSetupUri_ReturnsValidOtpauthUri`
- `ValidateCode_WithCurrentCode_ReturnsTrue`
- `ValidateCode_WithPreviousStepCode_ReturnsTrue` (tolerance)
- `ValidateCode_WithNextStepCode_ReturnsTrue` (tolerance)
- `ValidateCode_WithExpiredCode_ReturnsFalse`
- `ValidateCode_WithInvalidCode_ReturnsFalse`

#### TotpAuthProcessor Tests
- `EnableTotpAsync_CreatesEntityWithEncryptedSecret`
- `EnableTotpAsync_WhenAlreadyEnabled_ReturnsError`
- `ConfirmTotpAsync_WithValidCode_SetsIsEnabledTrue`
- `ConfirmTotpAsync_WithInvalidCode_ReturnsError`
- `DisableTotpAsync_WithValidCode_DeletesEntity`
- `DisableTotpAsync_WithInvalidCode_ReturnsError`
- `RegenerateRecoveryCodes_DeletesOldAndCreatesNew`

#### GoogleAuthProcessor Tests
- `LinkGoogleAuth_WithValidToken_CreatesEntity`
- `LinkGoogleAuth_WhenAlreadyLinked_ReturnsError`
- `LinkGoogleAuth_WhenSubjectIdTaken_ReturnsError`
- `UnlinkGoogleAuth_WhenLastAuthMethod_ReturnsError`
- `UnlinkGoogleAuth_WithOtherMethodAvailable_DeletesEntity`

#### GoogleIdTokenValidator Tests
- `ValidateAsync_WithValidToken_ReturnsPayload`
- `ValidateAsync_WithExpiredToken_ReturnsNull`
- `ValidateAsync_WithWrongAudience_ReturnsNull`
- `ValidateAsync_WithNoGoogleClientId_ReturnsNull`

#### AuthenticationService Tests (Extended)
- `SignInAsync_WithMfaEnabled_ReturnsMfaRequiredChallenge`
- `SignInAsync_WithMfaDisabled_ReturnsSuccess`
- `SignInWithGoogleAsync_WithValidToken_ReturnsSuccess`
- `SignInWithGoogleAsync_WithMfaEnabled_ReturnsMfaRequiredChallenge`
- `SignInWithGoogleAsync_WithUnlinkedAccount_ReturnsGoogleAuthNotLinkedError`
- `SignInWithGoogleAsync_WithInvalidToken_ReturnsInvalidGoogleTokenError`
- `VerifyMfaAsync_WithValidTotpCode_ReturnsSuccess`
- `VerifyMfaAsync_WithValidRecoveryCode_ReturnsSuccess`
- `VerifyMfaAsync_WithInvalidCode_ReturnsInvalidMfaCodeError`
- `VerifyMfaAsync_WithExpiredChallenge_ReturnsMfaChallengeExpiredError`
- `VerifyMfaAsync_WithUsedChallenge_ReturnsMfaChallengeExpiredError`
- `VerifyMfaAsync_RecoveryCode_MarksCodeAsUsed`

#### Registration/Deregistration Service Tests (Extended)
- `EnableTotpAsync_WithoutUserContext_ReturnsUnauthorized`
- `LinkGoogleAuthAsync_WithoutUserContext_ReturnsUnauthorized`
- `UnlinkGoogleAuthAsync_WhenOnlyAuthMethod_ReturnsError`

### Web Unit Tests (Corely.IAM.Web.UnitTests)

- `VerifyMfaPage_WithValidCode_RedirectsToDashboard`
- `VerifyMfaPage_WithInvalidCode_ShowsError`
- `GoogleCallbackPage_WithValidCode_SignsInUser`
- `Profile_TotpSection_ShowsEnableWhenDisabled`
- `Profile_TotpSection_ShowsDisableWhenEnabled`

---

## Documentation

### Corely.IAM/Docs/ — New Files

- **`mfa.md`** — TOTP setup, enable/disable flow, recovery codes, configuration
- **`google-signin.md`** — Google OAuth setup, linking, sign-in flow, configuration

### Corely.IAM/Docs/ — Updated Files

- **`index.md`** — Add MFA and Google Sign-In to capabilities list and Topics
- **`authentication.md`** — Add two-phase sign-in flow, MFA challenge flow, Google sign-in flow
- **`step-by-step-setup.md`** — Add optional step for Google OAuth configuration
- **`services/registration.md`** — Add EnableTotp, ConfirmTotp, LinkGoogleAuth methods
- **`services/deregistration.md`** — Add UnlinkGoogleAuth method
- **`services/retrieval.md`** — Add GetTotpStatus, GetAuthMethods methods
- **`services/authentication-service.md`** — Add SignInWithGoogleAsync, VerifyMfaAsync methods
- **`result-codes.md`** — Add new result code enums

### Corely.IAM.Web/Docs/ — Updated Files

- **`index.md`** — Add MFA and Google Sign-In to capabilities
- **`authentication-flow.md`** — Add MFA verification page, Google callback page, two-phase flow
- **`pages/index.md`** — Add `/verify-mfa` and `/google-callback` routes
- **`pages/profile.md`** — Add TOTP section and linked accounts section

### Corely.IAM.DevTools/Docs/ — Updated

- **`index.md`** — Add `totp` and `google` command groups

---

## Implementation Order

### Phase 1: TOTP Provider (Standalone Crypto)
- `TotpProvider` — RFC 6238 implementation
- Unit tests for TOTP generation/validation
- DevTools standalone commands (`totp generate-code`, `totp validate-code`)

### Phase 2: TOTP Domain (Database + Business Logic)
- Entities: `TotpAuthEntity`, `TotpRecoveryCodeEntity`, `MfaChallengeEntity`
- Entity configurations
- Constants, validators, mappers
- `TotpAuthProcessor` + decorators
- Migration: `AddTotpAuth`
- DI registration
- Unit tests for processor

### Phase 3: MFA Integration into Authentication
- Modify `SignInAsync` to check TOTP and return `MfaRequiredChallenge`
- Add `VerifyMfaAsync` to `AuthenticationService`
- Add `EnableTotpAsync`, `ConfirmTotpAsync`, `DisableTotpAsync`, `RegenerateTotpRecoveryCodesAsync` to `RegistrationService`
- Add `GetTotpStatusAsync` to `RetrievalService`
- Extend `SignInResult` and `SignInResultCode`
- Unit tests for modified auth flow

### Phase 4: Google Auth Domain (Database + Business Logic)
- `GoogleAuthEntity` + configuration
- `GoogleIdTokenValidator` — Google JWT validation
- `GoogleAuthProcessor` + decorators
- Migration: `AddGoogleAuth`
- DI registration
- Unit tests

### Phase 5: Google Sign-In Integration
- Add `SignInWithGoogleAsync` to `AuthenticationService`
- Add `LinkGoogleAuthAsync` to `RegistrationService`
- Add `UnlinkGoogleAuthAsync` to `DeregistrationService`
- Add `GetAuthMethodsAsync` to `RetrievalService`
- Extend `SecurityOptions` with `GoogleClientId`
- Unit tests

### Phase 6: Web UI — MFA
- `VerifyMfa.cshtml` Razor Page
- Profile page TOTP section (enable/disable/regenerate)
- QR code rendering (use a JS library or server-side SVG)
- Modify `SignIn.cshtml.cs` to handle `MfaRequiredChallenge` redirect

### Phase 7: Web UI — Google Sign-In
- Google Sign-In button on `SignIn.cshtml`
- `GoogleCallback.cshtml` Razor Page
- Profile page linked accounts section
- Google OAuth JavaScript SDK integration (or server-side redirect)

### Phase 8: DevTools + ConsoleTest
- DevTools `totp` command group (enable, confirm, disable, status, regenerate)
- DevTools `google` command group (link, unlink, status)
- DevTools `auth signin-google` and `auth verify-mfa` commands
- ConsoleTest MFA demo section

### Phase 9: Documentation
- New docs: `mfa.md`, `google-signin.md`
- Update existing docs (authentication, services, pages, result codes)
- Update DevTools docs

---

## Implementation Notes

Completed on branch `feature/mfa-google-signin` across 6 commits.

### Deviations from plan

- **Phases 1-5 combined into a single commit** (`b263032`) — 73 files, all core library work done together rather than phase-by-phase
- **DevTools standalone TOTP commands** were placed in Phase 8 (not Phase 1 as planned) — grouped with other DevTools work for cleaner commits
- **Migration combined** — plan called for separate `AddTotpAuth` and `AddGoogleAuth` migrations; implemented as a single `AddMfaAndGoogleAuth` migration covering all 4 tables
- **QR code rendering** — used CDN-hosted `qrcodejs` library with JS interop rather than server-side SVG
- **Google Sign-In** — used Google Identity Services (GIS) `data-login_uri` POST flow rather than JavaScript callback + AJAX
- **`ITotpProvider`** — interface was already `public`; `TotpProvider` implementation is `internal` but resolved via DI, so no visibility change needed
- **GoogleIdTokenValidator tests** — only 4 tests (no-client-id and invalid-token cases) since testing valid/expired/wrong-audience requires real Google OIDC infrastructure
- **Mock repo Include limitation** — `TotpAuthProcessorTests` required a `WireRecoveryCodesNavPropertyAsync` helper to manually link navigation properties, since the mock repo's LINQ-to-Objects doesn't support EF Core `Include`

### Test counts

| Suite | Tests |
|-------|-------|
| Corely.IAM.UnitTests | 1,240 |
| Corely.IAM.Web.UnitTests | 87 |
| **Total** | **1,327** |

### Commits

| Hash | Description |
|------|-------------|
| `b263032` | Phases 1-5: Core library (73 files) |
| `1693298` | Phases 6-9: Web UI, DevTools, ConsoleTest, processor tests, docs (34 files) |
| `961eade` | QR code, Google Sign-In web flow, DevTools docs (10 files) |
| `b065343` | Remaining tests, config templates, service docs (10 files) |
| `f93b838` | Web docs updates (4 files) |

## Notes

- **No Corely.Security changes** — TOTP uses standard .NET `HMACSHA1`; Google validation uses `System.IdentityModel.Tokens.Jwt`
- **No new NuGet packages for TOTP** — pure BCL implementation
- **Google validation** may need `Microsoft.IdentityModel.Protocols.OpenIdConnect` for JWKS fetching, or implement manually with `HttpClient` + `System.IdentityModel.Tokens.Jwt` (already referenced for JWT auth)
- **MFA challenge cleanup** — expired challenges should be cleaned up periodically. Consider a background service or lazy cleanup on read.
- **Rate limiting on MFA verification** — consider limiting attempts per challenge to prevent brute force (6-digit TOTP has only 1M possibilities). Lock challenge after N failed attempts.
- **TOTP secret display** — the secret and recovery codes are shown exactly once (on enable). The service does not provide a way to retrieve the decrypted secret after initial setup.
- **Google sign-in without existing account** — Phase 5 only supports linking to existing users. A future enhancement could auto-register new users from Google sign-in (JIT provisioning).
- **`ITotpProvider` visibility** — needs to be `public` for ConsoleTest demo, or expose code generation through a service method. Making it public is cleaner since DevTools also uses it directly.

---

## Follow-up: Service Architecture Refactor

### Context

The original plan wired MFA methods into `IRegistrationService`, `IDeregistrationService`, and `IRetrievalService` — following the existing 4-service CRUD-volatility split. During implementation, it became clear this doesn't scale for feature-specific flows. MFA, Google Auth, and Invitations each have their own mini-lifecycles that span create/read/delete, scattering a single feature's methods across 3-4 services.

### Decision

Create **feature-specific services** for non-CRUD flows, while keeping the 4 core services for entity CRUD (Users, Accounts, Groups, Roles, Permissions):

| New Service | Methods to move | From |
|-------------|----------------|------|
| `IMfaService` | `EnableTotpAsync`, `ConfirmTotpAsync`, `DisableTotpAsync`, `GetTotpStatusAsync`, `RegenerateTotpRecoveryCodesAsync` | `IRegistrationService`, `IRetrievalService` |
| `IGoogleAuthService` | `LinkGoogleAuthAsync`, `UnlinkGoogleAuthAsync`, `GetAuthMethodsAsync` | `IRegistrationService`, `IDeregistrationService`, `IRetrievalService` |
| `IAuthenticationService` | `SignInWithGoogleAsync`, `VerifyMfaAsync` — **no change** (these are auth actions, not configuration) | Already there |

Each new service gets its own Authorization + Telemetry decorators following the existing Scrutor pattern.

### Rationale

- Core CRUD entities share a lifecycle — the 4-service split works for them
- Feature flows (MFA, Google, Invitations) have distinct lifecycles that don't map to CRUD
- Host apps only inject what they need — apps without MFA don't touch `IMfaService`
- The inconsistency ("why is `CreateGroup` in `IRegistrationService` but `EnableTotp` in `IMfaService`?") is explainable: *core entities use CRUD services, feature flows get their own service*

### Scope

This refactor should be done as a **separate branch/PR** since it touches:
- `IRegistrationService` / `RegistrationService` + decorators (remove MFA + Google methods)
- `IDeregistrationService` / `DeregistrationService` + decorators (remove `UnlinkGoogleAuthAsync`)
- `IRetrievalService` / `RetrievalService` + decorators (remove `GetTotpStatusAsync`, `GetAuthMethodsAsync`)
- `ServiceRegistrationExtensions.cs` (register new services + decorators)
- All consumers: DevTools commands, ConsoleTest, Web Profile page, service-level tests
- Service documentation

See also: `Plans/account-invitations.md` Design Decision #11 — same refactor applies to `IInvitationService`.

---

## Follow-up: Flexible Authentication Method Management

### Context

The current implementation assumes users always start with a password (`RegisterUserAsync` requires a password). Google is treated as a secondary method that can be linked after registration. This limits user flexibility — there's no way to:

1. **Sign up with Google only** (no password)
2. **Remove a password** once set (there's no delete method for basic auth)
3. **Transition between auth methods** freely (e.g., start with Google, add password later, remove Google)

### Goal

Give users complete control over their authentication methods, with one constraint: **at least one method must remain active at all times**.

### Supported Flows (After This Change)

| Flow | Current State | Target State |
|------|--------------|--------------|
| Sign up with basic auth | ✅ Works | ✅ No change |
| Sign up with Google | ❌ Not supported | ✅ New: `RegisterUserWithGoogleAsync` |
| Sign up with Google → add basic auth | ❌ No Google signup | ✅ New signup + existing `CreateBasicAuthAsync` |
| Sign up with basic auth → add Google → remove basic auth | ❌ No remove basic auth | ✅ New: `RemoveBasicAuthAsync` (blocks if last method) |
| Sign up with basic auth → add Google → remove Google | ✅ Works (`UnlinkGoogleAuthAsync` blocks if last method) | ✅ No change |
| Sign up with Google → add basic auth → remove Google | ❌ No Google signup | ✅ New signup + existing unlink |
| Sign up with Google → set up MFA | ❌ No Google signup | ✅ New signup + existing MFA flow |

### New Methods

#### 1. `RegisterUserWithGoogleAsync` — on `IRegistrationService`

**Decision:** `IRegistrationService`. The host app explicitly controls whether Google sign-up is allowed. The Web UI chains: try `SignInWithGoogleAsync` → if `GoogleAuthNotLinkedError` → prompt "Create account?" → call `RegisterUserWithGoogleAsync` → then `SignInWithGoogleAsync`. This keeps the library non-opinionated about JIT provisioning.

```csharp
public record RegisterUserWithGoogleRequest(string GoogleIdToken);

public record RegisterUserWithGoogleResult(
    RegisterUserWithGoogleResultCode ResultCode,
    string Message,
    Guid CreatedUserId
);

public enum RegisterUserWithGoogleResultCode
{
    Success,
    InvalidGoogleTokenError,
    GoogleAccountInUseError,    // Google subject already linked to another user
    UserExistsError,            // Email from Google already exists as a user
    ValidationError,
}

Task<RegisterUserWithGoogleResult> RegisterUserWithGoogleAsync(
    RegisterUserWithGoogleRequest request);
```

**Flow:**
1. Validate Google ID token (same as `SignInWithGoogleAsync`)
2. Check if Google subject is already linked → `GoogleAccountInUseError`
3. Check if email already exists as a user → `UserExistsError`
4. **Generate username** from Google email prefix (e.g., `jdoe` from `jdoe@gmail.com`). If the username collides, append `-[a-zA-Z0-9]{5}` and retry until unique.
5. Create `UserEntity` with generated username and Google email
6. Create `GoogleAuthEntity` linking the new user to the Google account
7. **No `BasicAuthEntity` created** — user has Google as their only auth method
8. Return `RegisterUserWithGoogleResult` with the new user ID

**Username change:** Users can already change their username via `IModificationService.ModifyUserAsync(UpdateUserRequest)` which accepts `Username`. No new work needed — Google-registered users can update their auto-assigned username from the Profile page.

#### 2. `DeleteBasicAuthAsync` — on `IBasicAuthProcessor`, exposed via `IDeregistrationService`

**Decision:** Processor method is `DeleteBasicAuthAsync` (follows the `Delete*` convention for entity destruction). Service method is `DeregisterBasicAuthAsync` on `IDeregistrationService` (follows the `Deregister*` convention — we are removing a registered authentication method).

```csharp
// Processor level
Task<DeleteBasicAuthResult> DeleteBasicAuthAsync(Guid userId);

// Service level (IDeregistrationService)
Task<DeregisterBasicAuthResult> DeregisterBasicAuthAsync();

public record DeregisterBasicAuthResult(
    DeregisterBasicAuthResultCode ResultCode,
    string Message
);

public enum DeregisterBasicAuthResultCode
{
    Success,
    NotFoundError,          // User doesn't have basic auth
    LastAuthMethodError,    // Can't remove — no other auth method exists
    UnauthorizedError,
}
```

**Flow:**
1. Check if user has basic auth → `NotFoundError` if not
2. Check if user has another auth method (Google) → `LastAuthMethodError` if not
3. Delete `BasicAuthEntity`
4. Return `Success`

`BasicAuthProcessor` needs `IReadonlyRepo<GoogleAuthEntity>` injected for the last-method check (mirrors how `GoogleAuthProcessor` has `IReadonlyRepo<BasicAuthEntity>`).

### Changes Required

#### Backend — `DeleteBasicAuthAsync`
- `IBasicAuthProcessor` — add `DeleteBasicAuthAsync(Guid userId)`
- `BasicAuthProcessor` — implement with last-method check (inject `IReadonlyRepo<GoogleAuthEntity>`)
- `BasicAuthProcessorAuthorizationDecorator` / `TelemetryDecorator` — add pass-through
- New models: `DeleteBasicAuthResult`, `DeleteBasicAuthResultCode`
- `IDeregistrationService` — add `DeregisterBasicAuthAsync()`
- `DeregistrationService` + decorators — implement by delegating to processor

#### Backend — `RegisterUserWithGoogleAsync`
- New models: `RegisterUserWithGoogleRequest`, `RegisterUserWithGoogleResult`, `RegisterUserWithGoogleResultCode`
- `IRegistrationService` — add `RegisterUserWithGoogleAsync(RegisterUserWithGoogleRequest request)`
- `RegistrationService` — implement: validate token, check collisions, generate username, create user + Google auth entity (no basic auth)
- `RegistrationServiceAuthorizationDecorator` — no auth required (unauthenticated registration flow)
- Username generation helper — extract email prefix, retry with `-[a-zA-Z0-9]{5}` suffix on collision

#### Web UI
- **Sign-In page** — `GoogleCallback.cshtml.cs`: when `GoogleAuthNotLinkedError`, redirect to a registration prompt page (or show inline "No account found — create one?") instead of just showing an error
- **Register page** — consider adding a "Sign up with Google" button (same GIS integration as sign-in page, but routes to `RegisterUserWithGoogleAsync` instead)
- **Profile page** — add "Password" section:
    - If user has basic auth + another method: show "Remove Password" button
    - If user has basic auth only: show password (no remove option)
    - If user has no basic auth: show "Set Password" button (links to password creation flow)

#### DevTools
- Add `register user-with-google <id-token-file>` command
- Add `deregister basic-auth` command

#### Bugfix — `UpdateUserAsync` collision check
- `UserProcessor.UpdateUserAsync` does NOT check for username or email collisions before saving. Both columns have unique indexes (`UserEntityConfiguration` lines 32, 36), so a collision throws an unhandled DB exception instead of returning a clean result code.
- `CreateUserAsync` already has the correct pattern (lines 61-76) — checks `u.Username == request.Username || u.Email == request.Email` before creating.
- **Fix:** Add the same collision check to `UpdateUserAsync`. Must exclude the current user from the check (a user "updating" to their own existing username is not a collision). Return a new `ModifyResultCode.UsernameExistsError` or `ModifyResultCode.EmailExistsError`, or reuse `ModifyResultCode.ValidationError` with a descriptive message.
- **Recommendation:** Add `UsernameExistsError` and `EmailExistsError` to `ModifyResultCode` for explicit handling by callers.

#### Tests
- `BasicAuthProcessor` — `DeleteBasicAuthAsync` tests (success, not found, last method blocks)
- `RegistrationService` — `RegisterUserWithGoogleAsync` tests (success, duplicate Google, email collision, username generation)
- `UserProcessor` — `UpdateUserAsync` collision tests (username taken, email taken, own username unchanged)
- Web — GoogleCallback page tests for registration prompt flow
- Web — Profile page password section tests

### Implementation Notes

- `GoogleAuthProcessor.UnlinkGoogleAuthAsync` is the mirror pattern for `DeleteBasicAuthAsync` — it checks `hasBasicAuth` before allowing removal. `DeleteBasicAuthAsync` checks `hasGoogleAuth`.
- `RegisterUserWithGoogleAsync` returns `UserExistsError` when the Google email matches an existing user. The host app can then suggest the user sign in with their existing credentials and link Google from their profile.
- Verify that `DeregisterUserAsync` cascade handles users without `BasicAuthEntity` — the user may have only Google auth.
- Username change is already supported via `IModificationService.ModifyUserAsync(UpdateUserRequest)` — Google-registered users with auto-assigned usernames can change them from the Profile page. No new work needed.

### Implementation Status

**Backend (done):**
- ✅ `BasicAuthProcessor.DeleteBasicAuthAsync` + `IDeregistrationService.DeregisterBasicAuthAsync`
- ✅ `IRegistrationService.RegisterUserWithGoogleAsync` (username generation, UoW transaction)
- ✅ `UserProcessor.UpdateUserAsync` collision check (UsernameExistsError / EmailExistsError)
- ✅ Models: `DeleteBasicAuthResult`, `DeregisterBasicAuthResult`, `RegisterUserWithGoogleRequest/Result`
- ✅ Auth + telemetry decorators for all new methods
- ✅ Test constructor params updated for new dependencies

**Remaining work (all items below):**

#### Phase A: Unit Tests

**`Corely.IAM.UnitTests/BasicAuths/Processors/BasicAuthProcessorDeleteTests.cs`** (new file):
- `DeleteBasicAuthAsync_ReturnsSuccess_WhenBasicAuthExistsAndGoogleAuthExists`
- `DeleteBasicAuthAsync_ReturnsNotFoundError_WhenNoBasicAuth`
- `DeleteBasicAuthAsync_ReturnsLastAuthMethodError_WhenNoGoogleAuth`

**`Corely.IAM.UnitTests/GoogleAuths/Processors/GoogleAuthRegisterTests.cs`** (new file, or extend existing):
Tests for `RegisterUserWithGoogleAsync` at the service level — this method lives on `RegistrationService`, not on a processor, so tests go in Services:

**`Corely.IAM.UnitTests/Services/RegistrationServiceRegisterWithGoogleTests.cs`** (new file):
- `RegisterUserWithGoogleAsync_ReturnsSuccess_WithValidGoogleToken`
- `RegisterUserWithGoogleAsync_ReturnsInvalidGoogleTokenError_WhenTokenInvalid`
- `RegisterUserWithGoogleAsync_ReturnsGoogleAccountInUseError_WhenSubjectAlreadyLinked`
- `RegisterUserWithGoogleAsync_ReturnsUserExistsError_WhenEmailAlreadyExists`
- `RegisterUserWithGoogleAsync_GeneratesUniqueUsername_WhenPrefixCollides`

**`Corely.IAM.UnitTests/Users/Processors/UserProcessorUpdateCollisionTests.cs`** (new file):
- `UpdateUserAsync_ReturnsUsernameExistsError_WhenUsernameAlreadyTaken`
- `UpdateUserAsync_ReturnsEmailExistsError_WhenEmailAlreadyTaken`
- `UpdateUserAsync_ReturnsSuccess_WhenUpdatingToOwnExistingUsername` (no false collision)
- `UpdateUserAsync_ReturnsSuccess_WhenUsernameAndEmailAreUnique`

#### Phase B: DevTools Commands

**`Corely.IAM.DevTools/Commands/Registration/RegisterUserWithGoogle.cs`** (new file):
- Nested inside the existing `Registration` partial class
- Argument: `IdTokenFile` (string, required) — filepath to Google ID token file
- Needs `IRegistrationService`
- Reads ID token from file, calls `RegisterUserWithGoogleAsync`
- On success: display created user ID and generated username

**`Corely.IAM.DevTools/Commands/Deregistration/DeregisterBasicAuth.cs`** (new file):
- Nested inside the existing `Deregistration` partial class
- No arguments (uses current user context)
- Needs `IDeregistrationService`, `IUserContextProvider`
- Calls `SetUserContextFromAuthTokenFileAsync` then `DeregisterBasicAuthAsync`

**DevTools docs** (`Corely.IAM.DevTools/Docs/index.md`):
- Add `register user-with-google <id-token-file>` to Registration table
- Add `deregister basic-auth` to Deregistration table

#### Phase C: ConsoleTest

**`Corely.IAM.ConsoleTest/Program.cs`** — add a "Google Registration + Auth Method Management" demo section:
1. Call `RegisterUserWithGoogleAsync` with a mock/test Google token — this will fail in the ConsoleTest since there's no real Google token to validate. **Alternative:** Skip the Google registration demo in ConsoleTest (it requires a real Google OIDC endpoint), but demo the basic auth removal flow:
    - After the existing MFA demo, before deregistration:
    - Show `GetAuthMethodsAsync` (has basic auth + no Google)
    - Note that `DeregisterBasicAuthAsync` would block here (last method)
    - This demonstrates the guard without needing Google infrastructure

Actually, the ConsoleTest uses mock repos (no real DB), but `GoogleIdTokenValidator` calls Google's real OIDC endpoint. So `RegisterUserWithGoogleAsync` can't work in ConsoleTest without mocking the validator. **Decision:** Skip Google registration in ConsoleTest. Add a comment explaining why. Demo the `DeregisterBasicAuthAsync` guard instead.

#### Phase D: Web UI — Profile Password Section

**`Corely.IAM.Web/Components/Pages/Profile.razor`** — add a "Password" section between "Two-Factor Authentication" and "Linked Accounts":

Three states:
1. **Has basic auth + has Google auth** → show "Remove Password" button
    - Clicking calls `DeregistrationService.DeregisterBasicAuthAsync()`
    - Confirmation modal: "Are you sure? You'll only be able to sign in with Google."
2. **Has basic auth only (no Google)** → show "Password is set" status, no remove option (would be last method)
3. **No basic auth (Google only)** → show "Set Password" form
    - Two password fields (new password + confirm)
    - Calls `RegistrationService.CreateBasicAuthAsync(...)` — wait, this method doesn't exist on the service. It's on `IBasicAuthProcessor` internally. Need to check if there's a service-level method for creating basic auth independently.
    - **Problem:** `RegisterUserAsync` creates both user + basic auth together. There's no service method to add basic auth to an existing user. The processor `IBasicAuthProcessor.CreateBasicAuthAsync` is internal.
    - **Solution:** Expose `CreateBasicAuthAsync` on a service. Options:
        - Add to `IRegistrationService` — it's registering an auth method
        - Add to `IMfaService` — doesn't fit
        - New service — overkill
    - **Decision:** Add `SetPasswordAsync(SetPasswordRequest)` to `IRegistrationService`. Delegates to `_basicAuthProcessor.CreateBasicAuthAsync`. Requires user context.

**New model:** `SetPasswordRequest(string Password)` — password only, userId comes from context.

**New result:** Reuse `CreateBasicAuthResult` at processor level, map to a public `SetPasswordResult` at service level.

**`Corely.IAM.Web/Components/Pages/Profile.razor`** state variables:
- `_hasBasicAuth` (already exists from `GetAuthMethodsAsync`)
- `_hasGoogleAuth` (already exists)
- `_newPassword`, `_confirmPassword` for the set-password form
- `_showRemovePasswordConfirm` for the confirmation toggle

**`Corely.IAM.Web/Components/Pages/Profile.razor`** methods:
- `RemovePasswordAsync()` — calls `DeregistrationService.DeregisterBasicAuthAsync()`
- `SetPasswordAsync()` — validates passwords match, calls `RegistrationService.SetPasswordAsync(new SetPasswordRequest(_newPassword))`

#### Phase E: Web UI — GoogleCallback Registration Prompt

**`Corely.IAM.Web/Pages/Authentication/GoogleCallback.cshtml.cs`** — currently shows a static error for `GoogleAuthNotLinkedError`. Change to:
- Store the Google ID token in `TempData["GoogleIdToken"]`
- Redirect to a new `RegisterWithGoogle` page

**`Corely.IAM.Web/Pages/Authentication/RegisterWithGoogle.cshtml`** (new Razor Page):
- Shows: "No account found for this Google account. Create one?"
- Displays the Google email (from TempData or re-validation)
- "Create Account" button → calls `RegisterUserWithGoogleAsync` → on success, auto sign-in via `SignInWithGoogleAsync` → redirect to dashboard
- "Cancel" link → back to sign-in

**`Corely.IAM.Web/AppRoutes.cs`** — add `RegisterWithGoogle = "/register-with-google"`

#### Phase F: Web UI — Register Page Google Button

**`Corely.IAM.Web/Pages/Authentication/Register.cshtml`** — add Google sign-up button (same GIS integration as SignIn):
- Add `GoogleClientId` property to `RegisterModel` (same pattern as `SignInModel`)
- Add `GoogleCallbackUrl` computed in `OnGet`
- Add the Google button markup + `@section Scripts` (same as SignIn.cshtml)
- The GoogleCallback page handles both sign-in and registration (if `GoogleAuthNotLinkedError`, redirects to `RegisterWithGoogle`)

#### Phase G: Documentation

**`Corely.IAM/Docs/services/registration.md`** — add `RegisterUserWithGoogleAsync`, `SetPasswordAsync`
**`Corely.IAM/Docs/services/deregistration.md`** — add `DeregisterBasicAuthAsync`
**`Corely.IAM/Docs/result-codes.md`** — add `RegisterUserWithGoogleResultCode`, `DeregisterBasicAuthResultCode`, `ModifyResultCode.UsernameExistsError/EmailExistsError`
**`Corely.IAM/Docs/google-signin.md`** — add Google sign-up flow
**`Corely.IAM.Web/Docs/authentication-flow.md`** — add Google registration flow
**`Corely.IAM.Web/Docs/pages/index.md`** — add `/register-with-google` route
**`Corely.IAM.Web/Docs/pages/profile.md`** — add Password section
**`Corely.IAM.DevTools/Docs/index.md`** — add new commands
