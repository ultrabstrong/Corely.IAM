# Google Sign-In

Users can link a Google account as an alternative authentication method. When linked, users can sign in with a Google ID token instead of (or in addition to) a username and password.

## Features

- **Link/unlink Google accounts** — users can link one Google account to their user profile
- **Google ID token validation** — server-side JWT validation via Google's OIDC discovery endpoint
- **MFA support** — if TOTP is enabled, Google sign-in also requires MFA
- **Auth method safety** — cannot unlink the last authentication method

## Linking a Google Account

```csharp
// Link a Google account to the current user
var result = await registrationService.LinkGoogleAuthAsync(
    new LinkGoogleAuthRequest(googleIdToken));

// Check auth methods
var methods = await retrievalService.GetAuthMethodsAsync();
// methods.HasBasicAuth, methods.HasGoogleAuth, methods.GoogleEmail
```

### Constraints

- Each user can link **one** Google account
- Each Google account (by subject ID) can be linked to **one** user
- At least one auth method must remain linked — cannot unlink Google if no password exists

## Sign Up with Google

New users can register directly with a Google account, bypassing the traditional username/password flow:

```csharp
var result = await registrationService.RegisterUserWithGoogleAsync(
    new RegisterUserWithGoogleRequest(googleIdToken));

if (result.ResultCode == RegisterUserWithGoogleResultCode.Success)
{
    var userId = result.CreatedUserId;
    // User is created with Google as their sole auth method — no password set
}
```

The flow validates the Google ID token, extracts the email and subject ID, creates a new user, and links the Google account in a single operation. The user can later add a password via `SetPasswordAsync` or sign in exclusively with Google.

### Result Codes

| Code | Meaning |
|------|---------|
| `Success` | User created and Google account linked |
| `InvalidGoogleTokenError` | Google ID token validation failed |
| `GoogleAccountInUseError` | This Google account is already linked to another user |
| `UserExistsError` | Username derived from Google email already taken |
| `ValidationError` | Input validation failed |

## Signing In with Google

```csharp
var result = await authService.SignInWithGoogleAsync(
    new SignInWithGoogleRequest(googleIdToken, deviceId));

if (result.ResultCode == SignInResultCode.Success)
{
    var token = result.AuthToken;
}
else if (result.ResultCode == SignInResultCode.MfaRequiredChallenge)
{
    // TOTP is enabled — verify MFA to complete sign-in
    var mfaResult = await authService.VerifyMfaAsync(
        new VerifyMfaRequest(result.MfaChallengeToken!, totpCode));
}
```

### Result Codes

| Code | Meaning |
|------|---------|
| `Success` | Sign-in complete, JWT issued |
| `MfaRequiredChallenge` | TOTP enabled — verify MFA to complete |
| `InvalidGoogleTokenError` | Google ID token validation failed |
| `GoogleAuthNotLinkedError` | No user linked to this Google account |

## Unlinking

```csharp
var result = await deregistrationService.UnlinkGoogleAuthAsync();
```

Returns `LastAuthMethodError` if the user has no other authentication method (e.g., no password set).

## Google ID Token Validation

The `GoogleIdTokenValidator` validates tokens server-side:

1. Fetches Google's OIDC discovery document from `https://accounts.google.com/.well-known/openid-configuration`
2. Retrieves the JSON Web Key Set (JWKS) for signature verification
3. Validates the token signature, expiration, issuer, and audience
4. Extracts the subject ID and email from the payload

### Configuration

```json
{
  "SecurityOptions": {
    "GoogleClientId": "your-client-id.apps.googleusercontent.com"
  }
}
```

The `GoogleClientId` is used as the audience claim during token validation.

## DevTools Commands

```
google link <id-token-file>     # Link Google account (reads ID token from file)
google unlink                   # Unlink Google account
google status                   # Show linked account info
auth signin-google <id-token-file>  # Sign in with Google ID token
```

## Database Tables

| Table | Purpose |
|-------|---------|
| `GoogleAuths` | Google account links (subject ID, email) per user |
