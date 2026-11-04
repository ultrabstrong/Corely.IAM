# Sign-in providers in platform settings

The DocsToData owner (2026-11-04): turning on Google sign-in should be a platform setting, not a
redeploy, and the same place should later hold other providers. Today Google is switched on only by
`SecurityOptions.GoogleClientId` in configuration.

## What exists

1. `GoogleIdTokenValidator` validates a Google ID token against Google's published keys, with the
   configured client ID as the audience. No client secret is involved.
2. Sign in (`SignIn.cshtml`), sign up (`RegisterWithGoogle.cshtml`) and the profile's **Linked
   accounts** section (`LinkedAccountsSection.razor`, `google-link.js`) use Google's own sign-in script
   to get that token, and only appear when the client ID is set.
3. `GoogleAuths` holds one linked Google identity per user. An email address alone never signs in to
   an existing user; the user links Google first.
4. Platform security settings are stored, audited and edited by platform members
   (`PlatformSecuritySettings.razor`, `ISecurityPolicyProvider`).

## The shape

1. **A Sign-in providers section in platform security settings.** One row per provider: on or off,
   its client ID, and, for a provider that needs one, its secret. Changing it is audited like the
   rest of security settings and takes effect without a deploy.
2. **The platform setting is the only source.** `SecurityOptions.GoogleClientId` goes away in the same
   major release, rather than living beside the setting where one would silently win. An app that set
   it enters the client ID once in the new section.
3. **Secrets are write only.** A secret is encrypted with the system key before it is stored, never
   shown again (the page says "set" and offers "replace"), never logged or audited by value, and read
   only by the code that exchanges a sign-in with that provider.
4. **One provider abstraction.** Each provider turns what the browser hands back (an ID token or an
   authorization code) into a verified external identity: provider, subject, email and whether the
   provider verified that email. Sign in, sign up and linking work the same for every provider.
5. **One table for linked identities.** `GoogleAuths` becomes `ExternalLogins` (user, provider,
   subject, email), migrated row for row, so a user can link several providers.
6. **The takeover rule holds for every provider.** An email alone never signs in to an existing
   user, and an unverified email never creates one.

## Providers, in order

| Provider | What the browser returns | Secret needed |
|---|---|---|
| Google | ID token, from Google's sign-in script | No |
| Microsoft (work and personal accounts) | ID token, through MSAL with PKCE | No |
| Apple | ID token, from Sign in with Apple JS | No, to validate the ID token |
| GitHub | Authorization code; no ID token | Yes, for the code exchange |
| Facebook | Access token, checked against the app | Yes |
| Amazon (Login with Amazon) | Authorization code, then a profile call | Yes |

1. **Google first:** the section, the setting as the only source, and the existing Google flow reading
   from it. This alone is what DocsToData needs to turn Google on.
2. **The abstraction and `ExternalLogins`,** with Google moved onto them and nothing else changing.
3. **Microsoft and Apple,** which need no secret.
4. **The first secret storage,** then GitHub, Facebook and Amazon on top of it.

## Tests

1. Unit: each provider's validation (audience, issuer, expiry, unverified email), the takeover rule
   for every provider, and that a secret never appears in a setting read back, an audit entry or a
   log.
2. Integration: the `GoogleAuths` to `ExternalLogins` migration keeps every row, and settings round
   trip with the secret encrypted at rest.
3. No test reaches a real provider; tokens are signed with test keys the validator is pointed at.

## Releasing

A major version of Corely.IAM and Corely.IAM.Web: `GoogleClientId` is removed and the table is
renamed. Each step above can ship on its own.
