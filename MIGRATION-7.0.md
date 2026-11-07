# Migrating to Corely.IAM 7.0

Google sign in becomes one of five identity providers (Google, Microsoft, GitHub, Facebook, Amazon),
configured like any other security setting and signed in through the authorization code flow on the
server. One migration moves every linked Google account across. `corely-iam-db` 7.0 goes with it. See
[Federated Sign-In](Corely.IAM/Docs/federated-sign-in.md).

## 1. Apply the migration

```powershell
corely-iam-db db migrate -p MsSql -c "your-connection-string"
```

It creates `FederatedIdentities` and `IdentityProviders`, copies every `GoogleAuths` row into
`FederatedIdentities` (issuer `https://accounts.google.com`, the Google subject as the subject), and
drops `GoogleAuths`. Linked users keep signing in with Google.

## 2. Move the Google settings

```json
// 6.x
"SecurityOptions": { "GoogleClientId": "<id>.apps.googleusercontent.com" }

// 7.0
"SecurityOptions": {
  "IdentityProviders": {
    "Google": { "ClientId": "<id>.apps.googleusercontent.com", "ClientSecret": "<secret>" }
  }
}
```

The code flow redeems the code on the server, so Google now needs a client secret: create one in the
same Google client. Register `https://<host>/signin/callback/google` as a redirect URI.

With a platform account, configuration only seeds a new platform: an existing one turns Google on, with
its client ID and secret, on the Security page or through `IPlatformService.UpdateIdentityProviderAsync`.

## 3. Replace the Google API

| 6.x | 7.0 |
|-----|-----|
| `IAuthenticationService.SignInWithGoogleAsync(SignInWithGoogleRequest)` | `IFederatedSignInService.StartFederatedSignInAsync`, then `CompleteFederatedSignInAsync` with purpose `SignIn` |
| `IRegistrationService.RegisterUserWithGoogleAsync(...)` | The same `SignIn` purpose: it signs up when no user is linked and the provider allows it |
| `IGoogleAuthService.LinkGoogleAuthAsync(LinkGoogleAuthRequest)` | `StartFederatedSignInAsync` with purpose `Link` |
| `IGoogleAuthService.UnlinkGoogleAuthAsync()` | `IFederatedIdentityService.UnlinkFederatedIdentityAsync(IdentityProviderType.Google)` |
| `IGoogleAuthService.GetAuthMethodsAsync()` | `IFederatedIdentityService.GetAuthMethodsAsync()`; `HasGoogleAuth` and `GoogleEmail` become `LinkedIdentities` |
| `AccountKeyProof`, `RevealAccountKeyRequest` with a Google ID token and nonce | The password and TOTP code only. A user without a password proves it with a `Reauthenticate` round trip first |

## 4. Handle the changed result codes

| Before | After |
|--------|-------|
| `SignInResultCode.GoogleAuthNotLinkedError`, `InvalidGoogleTokenError` | Gone; see `CompleteFederatedSignInResultCode` |
| `RevealAccountKeyResultCode` and `CompromiseAccountKeyResultCode` `GoogleSignInRequiredError` | `FederatedSignInRequiredError` |
| `InvalidGoogleSignInError` on both | Gone |

## 5. DevTools

`google link`, `google unlink`, `google status`, `auth signin-google` and `register user-with-google`
are gone. `identity status` and `identity unlink <provider>` remain; linking and signing in need a
browser.
