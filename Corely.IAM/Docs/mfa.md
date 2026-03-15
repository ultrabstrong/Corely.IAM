# Multi-Factor Authentication (TOTP)

Time-based One-Time Password (TOTP) support per RFC 6238. Users can enable TOTP as a second factor — when enabled, all sign-in methods (password and Google) require a TOTP code after initial authentication.

## Features

- **TOTP (RFC 6238)** — HMAC-SHA1, 6-digit codes, 30-second period, 1-step tolerance
- **Recovery codes** — 10 single-use backup codes in `XXXX-XXXX` format
- **Two-phase sign-in** — credential verification → MFA challenge → JWT
- **MFA challenges** — short-lived (5 min), single-use tokens stored in the database
- **Applies to all auth methods** — password sign-in and Google sign-in both trigger MFA when enabled

## Enabling TOTP

```csharp
// 1. Enable — generates secret and recovery codes
var enableResult = await registrationService.EnableTotpAsync();
// enableResult.Secret — base32-encoded secret for authenticator app
// enableResult.SetupUri — otpauth:// URI for QR code scanning
// enableResult.RecoveryCodes — 10 single-use backup codes

// 2. Confirm — user enters a code from their authenticator app
var confirmResult = await registrationService.ConfirmTotpAsync(
    new ConfirmTotpRequest("123456"));
```

The secret and recovery codes are shown exactly once during enablement. The service does not provide a way to retrieve the decrypted secret after initial setup.

## Sign-In with MFA

When TOTP is enabled, `SignInAsync` returns `MfaRequiredChallenge` instead of a JWT:

```csharp
var signInResult = await authService.SignInAsync(signInRequest);

if (signInResult.ResultCode == SignInResultCode.MfaRequiredChallenge)
{
    // Prompt user for TOTP code, then verify
    var mfaResult = await authService.VerifyMfaAsync(
        new VerifyMfaRequest(signInResult.MfaChallengeToken!, totpCode));

    if (mfaResult.ResultCode == SignInResultCode.Success)
    {
        var token = mfaResult.AuthToken; // JWT issued after MFA verification
    }
}
```

The same flow applies to `SignInWithGoogleAsync` — if TOTP is enabled, it returns `MfaRequiredChallenge`.

## Managing TOTP

```csharp
// Check status
var status = await retrievalService.GetTotpStatusAsync();
// status.IsEnabled, status.RemainingRecoveryCodes

// Regenerate recovery codes (invalidates old ones)
var regenResult = await registrationService.RegenerateTotpRecoveryCodesAsync();

// Disable TOTP (requires a valid TOTP code)
var disableResult = await registrationService.DisableTotpAsync(
    new DisableTotpRequest("123456"));
```

## Recovery Codes

- 10 codes generated on enable
- Format: `XXXX-XXXX` (8 alphanumeric characters)
- Each code is single-use — marked as used after verification
- Stored as salted hashes in the database
- Can be regenerated at any time (invalidates previous codes)
- Accepted anywhere a TOTP code is accepted (MFA verification, disable)

## MFA Challenge Lifecycle

| Property | Value |
|----------|-------|
| Timeout | 300 seconds (5 minutes) |
| Usage | Single-use — consumed on successful verification |
| Storage | `MfaChallenges` table with `UserId`, `Token`, `ExpiresUtc`, `CompletedUtc` |

Expired or already-completed challenges return `MfaChallengeExpiredError`.

## DevTools Commands

```
totp enable                          # Enable TOTP, outputs secret + URI + recovery codes
totp confirm <code>                  # Confirm TOTP setup with authenticator code
totp disable <code>                  # Disable TOTP (requires valid code)
totp status                          # Show TOTP status and remaining recovery codes
totp regenerate-codes                # Regenerate recovery codes
totp generate-code <secret>          # (Standalone) Generate a TOTP code from a secret
totp validate-code <secret> <code>   # (Standalone) Validate a TOTP code
auth verify-mfa <token> <code>       # Complete MFA verification after sign-in
```

## Database Tables

| Table | Purpose |
|-------|---------|
| `TotpAuths` | TOTP configuration per user (encrypted secret, enabled flag) |
| `TotpRecoveryCodes` | Recovery code hashes linked to TotpAuth |
| `MfaChallenges` | Short-lived challenge tokens for two-phase sign-in |
