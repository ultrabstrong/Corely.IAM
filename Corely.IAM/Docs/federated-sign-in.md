# Federated Sign-In

Users can sign in with an identity provider (IdP) instead of, or as well as, a password. IAM is the
relying party: it sends the browser to the provider, the provider sends it back with an authorization
code, and IAM redeems the code on the server. Google and Microsoft use OpenID Connect; GitHub,
Facebook and Amazon use OAuth 2.0.

| Provider | Protocol | Credential | Signs a user up | Forces a fresh sign in to prove it is you |
|----------|----------|------------|-----------------|--------------------------------------------|
| [Google](identity-providers/google.md) | OpenID Connect | Client secret | When Google verified the email | No |
| [Microsoft](identity-providers/microsoft.md) | OpenID Connect | Certificate (preferred) or client secret | For a personal account, or when `xms_edov` is true | Yes |
| [GitHub](identity-providers/github.md) | OAuth 2.0 | Client secret | From the primary verified email | No |
| [Facebook](identity-providers/facebook.md) | OAuth 2.0 | App secret | When an email is shared | No |
| [Amazon](identity-providers/amazon.md) | OAuth 2.0 | Client secret | When an email is shared | No |

## The flow

Every provider takes the same path: the authorization code flow with PKCE (S256), `state`, and a
`nonce` for OpenID Connect.

```csharp
var start = await federatedSignInService.StartFederatedSignInAsync(
    new StartFederatedSignInRequest(IdentityProviderType.Google, FederatedSignInPurpose.SignIn, redirectUri));
// Keep start.Correlation for this browser only (Corely.IAM.Web seals it in a cookie),
// then send the browser to start.AuthorizationUrl.

var result = await federatedSignInService.CompleteFederatedSignInAsync(
    new CompleteFederatedSignInRequest(correlation, redirectUri, deviceId,
        query["code"], query["state"], query["iss"], query["error"]));
```

`CompleteFederatedSignInAsync` refuses a sign in older than ten minutes, a `state` that does not match,
an `iss` from another issuer, and a response carrying `error`, before it redeems anything. It then
redeems the code with the verifier and the provider's credential, and for OpenID Connect validates the
ID token's signature, issuer, audience, lifetime and nonce. Codes, tokens and credentials are never
logged, and the provider's tokens are discarded once the identity is read.

A host gives each provider its own redirect URI, so a response can only be redeemed with the provider
it was meant for.

## Purposes

| Purpose | Needs | Does |
|---------|-------|------|
| `SignIn` | Nothing | Signs in the user linked to the identity. With none linked, creates one when the provider allows sign up and vouches for the email |
| `Link` | A signed in user | Links the identity to that user |
| `Reauthenticate` | A signed in user | Records proof, for five minutes and this session only, that the user just signed in with an identity linked to them |

Signing in goes through two factor like a password sign in: `MfaRequiredChallenge` comes back when
the user has TOTP on.

## Identity and takeover

- A linked identity is keyed by issuer and subject, never by email. An email alone never signs in to
  an existing user.
- One identity per provider per user, and one user per identity.
- Sign up refuses an email another user already has (`EmailInUseError`): the user signs in their usual
  way and links the provider from their profile.
- Sign up refuses an email the provider calls unverified (`EmailNotVerifiedError`), and an identity
  with no email (`NoEmailError`). Both can still be linked.
- The username comes from the email's local part, with a random suffix when it is taken.
- A user's last way to sign in cannot be unlinked (`LastAuthMethodError`); an identity whose provider
  is off does not count as a way in.

## Linked identities

```csharp
var methods = await federatedIdentityService.GetAuthMethodsAsync();
// methods.HasBasicAuth, methods.LinkedIdentities (type, email, linked time)

var result = await federatedIdentityService.UnlinkFederatedIdentityAsync(IdentityProviderType.GitHub);
```

Unlinking ends the user's other sessions.

## Proving it is you

A user with a password proves it with the password before a sensitive action such as revealing an
account key. A user without one starts a `Reauthenticate` round trip to a linked provider, then
retries the action; `VerifyAccountKeyProofAsync` consumes the proof recorded for the current session.
Microsoft is asked for a fresh sign in (`prompt=login`, `max_age=0`) and its `auth_time` must be
recent. The other providers may carry the person on their own session.

## Settings

Providers are platform security settings, configured the same way as the rest of them: in
configuration until there is a platform account, and on the Security page after. Bootstrap copies the
configured providers into the platform's settings.

```json
{
  "SecurityOptions": {
    "IdentityProviders": {
      "Google": { "ClientId": "...apps.googleusercontent.com", "ClientSecret": "..." },
      "Microsoft": { "ClientId": "...", "CertificatePfxBase64": "...", "CertificatePassword": "...", "Tenant": "common" },
      "GitHub": { "ClientId": "...", "ClientSecret": "...", "AllowSignUp": false }
    }
  }
}
```

| Key | Default | Meaning |
|-----|---------|---------|
| `Enabled` | `true` | Off keeps the settings and the linked identities, and hides the provider everywhere |
| `AllowSignUp` | `true` | Off still signs in and links, and creates no users |
| `ClientId` | none | The client or app ID the provider issued |
| `ClientSecret` | none | The shared secret |
| `CertificatePfxBase64`, `CertificatePassword` | none | Microsoft only: a PFX with its private key, used instead of a secret |
| `Tenant` | `common` | Microsoft only: `common`, `organizations`, `consumers` or one tenant ID |

Keep credentials in the host's secret store, not in `appsettings.json`. Saved in the platform's
settings they are encrypted with the system key, never shown again, and replaced rather than edited.

```csharp
await platformService.UpdateIdentityProviderAsync(new UpdateIdentityProviderRequest(
    IdentityProviderType.GitHub, enabled: true, allowSignUp: true, clientId: "Iv23li...",
    IdentityProviderCredentialKind.ClientSecret, clientSecret: "..."));

var dependents = await platformService.CountSoleSignInUsersAsync(IdentityProviderType.GitHub);
```

`CountSoleSignInUsersAsync` counts the users who would be locked out by turning a provider off.

## Result codes

### StartFederatedSignInResultCode

| Code | Meaning |
|------|---------|
| `Success` | `AuthorizationUrl` and `Correlation` are set |
| `ProviderNotAvailableError` | The provider is off or not configured |
| `UnauthorizedError` | Linking or proving needs a signed in user |
| `ProviderUnreachableError` | The provider's discovery document could not be loaded |

### CompleteFederatedSignInResultCode

| Code | Meaning |
|------|---------|
| `Success` | Signed in, linked or proved |
| `MfaRequiredChallenge` | TOTP on; finish with `VerifyMfaAsync` |
| `ProviderDeclinedError` | The provider returned an error, such as the person declining |
| `InvalidStateError` | The response does not belong to a sign in this browser started |
| `ExpiredError` | The sign in took longer than ten minutes |
| `IssuerMismatchError` | The response names another issuer |
| `ProviderNotAvailableError` | The provider is off or not configured |
| `InvalidResponseError` | The code or token could not be verified |
| `NotLinkedError` | No user is linked and sign up is off |
| `EmailNotVerifiedError` | The provider calls the email unverified |
| `NoEmailError` | The provider shared no email |
| `EmailInUseError` | Another user has the email |
| `IdentityInUseError` | Another user has the identity |
| `AlreadyLinkedError` | The user already has an identity at this provider |
| `NotLinkedToUserError` | Proving it is you with an identity not linked to the user |
| `UnauthorizedError` | Linking or proving needs a signed in user |
| `UserLockedError` | The user is locked out |
| `SignInError` | Signing in failed for another reason |

## Database tables

| Table | Purpose |
|-------|---------|
| `FederatedIdentities` | Linked identities: user, provider type, issuer, subject, email |
| `IdentityProviders` | The platform's provider settings, one row per provider |

## DevTools commands

```
identity status                 # Show the current user's sign in methods
identity unlink <provider>      # Unlink a provider from the current user
```
