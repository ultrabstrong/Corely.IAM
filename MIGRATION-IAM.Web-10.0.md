# Migrating to Corely.IAM.Web 10.0

Corely.IAM.Web 10.0 goes with Corely.IAM 7.0, whose [migration guide](MIGRATION-7.0.md) comes first.
Google's sign in script is gone: every identity provider signs in through a full page round trip, so
the pages load nothing from a provider.

## What breaks

| Before | After |
|--------|-------|
| `/google-callback` and `/register-with-google` (`AppRoutes.GoogleCallback`, `AppRoutes.RegisterWithGoogle`) | `/signin/start/{provider}` and `/signin/callback/{provider}` (`AppRoutes.FederatedSignInStart`, `AppRoutes.FederatedSignInCallback`) |
| `LinkedAccountsSection` (`HasGoogleAuth`, `GoogleEmail`) | `LinkedIdentitiesSection` (`LinkedIdentities` from `IFederatedIdentityService.GetAuthMethodsAsync()`) |
| `PasswordSection.HasGoogleAuth` | `PasswordSection.HasLinkedIdentity` |
| `EncryptionSigningPanel.RevealGoogleClientId` | `EncryptionSigningPanel.ProofProviders`, the user's linked and enabled providers |
| The panel's proof delegates took a Google ID token and nonce | They take the password and two factor code only |
| `IAuthCookieManager.SetReturnUrlCookie` and `TakeReturnUrlCookie` | Gone; the return URL travels inside the sign in's correlation cookie |
| `wwwroot/js/google-link.js` | Gone |

## What to do

1. Register `https://<host>/signin/callback/{provider}` with each provider you turn on.
2. Remove the Google entries from your Content-Security-Policy (`https://accounts.google.com/gsi/...`
   under `script-src`, `frame-src`, `connect-src` and `style-src`). No provider needs one.
3. Hosts running several instances share the ASP.NET Core Data Protection key ring, as antiforgery
   already requires: the correlation cookie is protected with it.
4. A host that composed its own profile page switches to `LinkedIdentitiesSection` and
   `HasLinkedIdentity`, as in [setup](Corely.IAM.Web/Docs/setup.md).

Platform members turn providers on and off, and set their credentials, in the new Identity providers
section of the platform Security page.
