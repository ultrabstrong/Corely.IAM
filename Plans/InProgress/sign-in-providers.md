# External identity providers

The DocsToData owner (2026-11-04): turning on Google sign in should be a platform setting, not a
redeploy, and the same place should later hold other providers. Today Google is switched on only by
`SecurityOptions.GoogleClientId` in configuration.

This is federated sign in over OpenID Connect and OAuth 2.0, both well specified, so the plan follows
the specifications and each provider's documentation rather than inventing. Every claim about how a
protocol or provider behaves cites a [source](#sources) as `[S#]`, with the section it was read
from. A choice no source dictates is marked **ours**. Anything still unconfirmed is marked
**unverified** and says how it gets confirmed.

## Terms

| Term | Meaning here |
|---|---|
| Identity provider (IdP), OpenID Provider (OP) | The service that signs the person in: Google, Microsoft, GitHub |
| Relying party (RP) | IAM: the "OAuth 2.0 Client application requiring End-User Authentication" [S4 §1.2] |
| Federation | IAM trusting an IdP to authenticate a user, over OpenID Connect or OAuth 2.0 now, SAML possibly later |
| Federated identity | One user's identity at one IdP, linked to an IAM user, identified by issuer and subject [S4 §5.7, S20 "Properties"] |
| SSO | What the user experiences: signed in once at the IdP, signed in wherever that IdP is trusted. A result of federation |
| Enterprise SSO | One customer account bringing its own IdP. Not in this plan; see [Per account configuration](#per-account-configuration) |

Consumers may market it under any name; IAM's code and docs use these.

## What exists

1. `GoogleIdTokenValidator` validates a Google ID token against Google's published keys, with the
   configured client ID as the audience.
2. Sign in (`SignIn.cshtml`), sign up (`RegisterWithGoogle.cshtml`), the profile's Linked accounts
   section (`LinkedAccountsSection.razor`, `google-link.js`) and the prove it is you dialog
   (`EncryptionSigningPanel.razor`, fed by `AccountDetail.razor`) use Google's sign in script to get
   that token. They show only when the client ID is set, and the profile otherwise says "Google
   sign-in is not configured for this application".
3. Proving it is you (`ReauthenticationProvider.VerifyIdentityAsync`): a user with no password proves
   their identity for a sensitive action, such as revealing an account key, with a fresh Google sign
   in tied to a nonce made for that attempt and at most five minutes old.
4. `GoogleAuths` holds one linked Google identity per user. An email address alone never signs in to
   an existing user; the user links Google first.
5. Security settings follow one rule (`Corely.IAM/Docs/platform.md`): configuration applies while
   there is no platform account; bootstrap copies it into the platform's saved settings, which then
   override it. `SecurityPolicyProvider` reads the saved row, falling back to
   `ConfiguredSecurityPolicy`, cached per instance for `AuditOptions.SettingsCacheTtlSeconds`.

## Providers in this release

| Provider | Protocol | Client credential | Key | Verified email |
|---|---|---|---|---|
| Google | OpenID Connect | Client secret; the only methods Google's metadata lists are `client_secret_post` and `client_secret_basic` [S10] | `iss` + `sub`; "Use `sub` within your application as the unique-identifier key for the user" [S7] | `email_verified` [S7, S8] |
| Microsoft (work and personal) | OpenID Connect | Certificate (`private_key_jwt`) or client secret, both in Microsoft's metadata [S17]. See [Client credentials](#client-credentials) | `iss` + `sub`, where `iss` carries the tenant [S11 "Validate tokens"] | Personal account, or `xms_edov` true [S13, S14] |
| GitHub | OAuth 2.0 through a GitHub App | Client secret, required on the exchange [S22] | Numeric user `id`, the "durable user ID", never `login`, which "can change over time" [S25] and whose old value "becomes available for anyone else to claim" [S26 "About username changes"] | `GET /user/emails`: the entry both `primary` and `verified` [S24] |
| Facebook | OAuth 2.0 | App secret [S27] | App scoped user `id` [S29] | By account policy: no flag, but the Graph API returns no email "unless the confirmation link sent to the inbox is explicitly clicked" [S45]; Meta's own reference says only "valid" [S29] |
| Amazon | OAuth 2.0 | Client secret, optional with PKCE, sent since IAM is a server [S32] | `user_id`, which "uniquely identifies the user's account", the same for every app of one company [S33 "profile", "profile:user_id"] | By account policy: no flag [S33, S34], but Amazon asks new customers to verify ownership of their email [S46] |

1. **GitHub through a GitHub App, not an OAuth App.** "In general, GitHub Apps are preferred to OAuth
   apps because they use fine-grained permissions" [S23]; the web flow is the same, with PKCE S256 and
   `state` [S22]. `GET /user/emails` takes a user access token with the "Email addresses" user
   permission, read [S43 "User permissions for Email addresses"].
2. **Facebook** uses its classic code flow; its OpenID Connect flow for the web is marked "still in
   testing" [S28]. Graph calls carry `appsecret_proof`, which Meta recommends for calls from a server
   [S30 "Verify Graph API Calls with appsecret_proof"]. Users without a role on the app need Advanced
   Access [S44 "Standard Access"], and "All newly created Consumer apps are automatically approved for
   Advanced Access for the email and public_profile permissions", switched on by hand, with the app
   Live [S44 "Automatic Approval"]. The setup doc says so. **Risk:** Meta also says Business
   Verification "is required to get Advanced Access" [S44] without saying whether the automatic
   approval is exempt; no page names a fee. A host that hits it completes verification or leaves
   Facebook off. The Graph API is versioned and each version is retired on Meta's schedule [S48]; the
   version is one constant on the Facebook provider type, and the docs say to move it.
3. **Amazon** documents `user_id` as unique, not as never changing [S33]. If it ever changed, the link
   would stop matching and the user would link again; nothing would sign them in as someone else,
   because no link matches on email.
4. **"By account policy"** means the provider confirms an email before an app can see it, but its API
   reference does not promise so; the evidence is an identity vendor's support article for Facebook
   [S45] and Amazon's help pages [S46]. The provider type records which kind of evidence it has, and
   the setup doc says so.
5. **Apple is out of this release.** It costs a paid Apple Developer membership. It comes later as its
   own plan, onto the abstraction this one proves, and is not researched here.

## The flow

Every provider, Google included, uses the authorization code flow with PKCE, from IAM's server.

1. **Authorization code with PKCE.** "Public clients MUST use PKCE" and for confidential clients it
   "is RECOMMENDED" [S1 §2.1.1]; clients "SHOULD NOT use the implicit grant" [S1 §2.1.2]. OWASP:
   "Clients **must** use the Authorization Code Grant with PKCE" [S38]. The challenge method is S256:
   "Currently, `S256` is the only such method" that does not expose the verifier [S1 §2.1.1]. Google
   lists S256 [S10], GitHub accepts only S256 [S22], Microsoft and Amazon take it [S15 "Request an
   authorization code", S32].
2. **Google's sign in script goes.** It hands the browser an ID token directly, while the server
   flow keeps tokens off the browser, which Amazon describes as "more secure since the token is never
   exposed to the user" [S32]. One flow is one path to build, test and secure **(ours)**.
3. **Start.** A sign in link (GET, see [CSP](#csp)) hits IAM's start endpoint, which creates `state`,
   the PKCE verifier and, for OpenID Connect, a nonce, stores them with the provider's issuer and the
   purpose (sign in, sign up, link, prove it is you) in a correlation cookie, and redirects to the
   IdP. `nonce` is required by Microsoft [S16 "Send the sign-in request"] and Google [S7].
4. **The correlation cookie** is encrypted and signed, because "clients MUST protect `state` against
   tampering and swapping" [S1 §4.7.1], with ASP.NET Core Data Protection in Corely.IAM.Web, whose
   key ring a host running several instances must share [S47]; antiforgery already needs that, and
   the setup doc says so. It is named with the `__Host-` prefix, so it must be `Secure`,
   have no `Domain` and have `Path=/` [S36 "Cookie prefixes"], which OWASP recommends [S41 "Cookie
   Name Prefixes"]. It is `HttpOnly`, `SameSite=Lax` and lives ten minutes **(ours)**.
5. **Callback, one per provider.** Each provider returns to its own callback path (for example
   `/signin/callback/google`). A client that talks to more than one authorization server "MUST
   prevent mix-up attacks" [S1 §4.4], and must store, "for each authorization request, the issuer
   they sent the authorization request to" [S1 §4.4.2]. Where the IdP sends RFC 9207's `iss` response
   parameter, the callback compares it to the stored issuer and rejects a mismatch [S6 §2.4, S1
   §4.4.2.1]; Google advertises it [S10] and Microsoft does not [S17]. Distinct redirect URIs per
   issuer are the defense for the others [S1 §4.4.2.2]. The redirect URI is registered with each IdP
   exactly, since the authorization server matches it exactly [S1 §2.1, S16].
6. **The callback checks, in order:** the cookie exists and its provider matches the path; `state`
   matches, then the cookie is deleted, since "the `state` value SHOULD be invalidated by the client
   after its first use" [S1 §4.2.4]; `iss` where sent; then an `error` response (the user declined)
   ends the attempt with a message and changes nothing [S4 §3.1.2.6]. Only then is the code redeemed,
   with the verifier and the client credential [S3 §4.5, S15 "Redeem a code for an access token"].
7. **ID token validation** follows OpenID Connect [S4 §3.1.3.7] and Google's steps [S8]: `iss` exactly
   matches (step 2), `aud` contains the client ID (step 3), `exp`, and the nonce (step 11). The
   signature is checked against the IdP's published keys. OpenID Connect allows TLS server validation
   in its place for a token straight from the token endpoint [S4 §3.1.3.7 step 6]; IAM checks the
   signature anyway, because Microsoft's multi tenant validation depends on the signing key's issuer
   [S11 "Validation of the signing key issuer"]. For Microsoft, `tid` must be a GUID and `iss` must
   equal `https://login.microsoftonline.com/{tid}/v2.0` with the token's own `tid` [S11].
8. **Keys** come from the issuer's discovery document (`jwks_uri`) [S5 §3], cached, and refetched
   when a token names an unfamiliar `kid` [S4 §10.1.1].
9. **The callback redirects to a local path only.** "Clients MUST NOT expose open redirectors" [S1
   §4.11.1]; the return path the start endpoint took is checked to be local before it is stored.
10. **Tokens are not kept.** IAM reads the identity and discards the IdP's tokens, asking for no
    refresh token and no scope beyond identity and email **(ours)**. Codes, tokens, secrets and the
    correlation cookie are never logged: OWASP lists access tokens and "primary secrets" as data to
    exclude [S39 "Data to exclude"].
11. **Signing in issues a fresh IAM session**, as password sign in does, since a session ID "must be
    renewed or regenerated by the web application after any privilege level change" [S41].

### The callback and the cross site GET rule

The callback is a GET that changes state when another site started it, which CLAUDE.md's UI rule
forbids. It is the redirection endpoint OAuth defines [S2 §3.1.2], and OAuth's own rule for it is
"The client MUST implement CSRF protection for its redirection URI" [S2 §10.12], which `state` bound
to this browser [S1 §2.1], PKCE [S3 §1.1] and the nonce [S4 §3.1.2.1] provide. `SameSite=Lax` sends
the correlation cookie on it, a top level navigation with a safe method [S36 "SameSite"]. The
exception is written into the UI rule, and `CrossSiteRequestTests` names the callback paths as the
only exemption, with a test that a callback without a matching cookie changes nothing.

## Protocols and providers

1. **A protocol is a seam; a provider is configuration on it.** `OpenIdConnect` and `OAuth2` are the
   protocols now, and SAML would be a third without touching what sits above. Each protocol turns a
   callback into one result: issuer, subject, email, whether the email is verified, and what the
   sign in proves about freshness.
2. **A provider type** fixes its protocol, endpoints (from discovery where the IdP publishes it
   [S5 §4]), scopes, its verified email rule and its credential methods. A platform member never
   types an endpoint. A generic OpenID Connect provider configured by a discovery URL fits later as
   one more type.
3. **Sign in, sign up, linking and proving it is you are one code path** over that result.

## Identity and takeover

1. **A federated identity is keyed by issuer and subject**, "the only Claims that an RP can rely
   upon as a stable identifier for the End-User" [S4 §5.7]. Microsoft Graph models linked identities
   the same way: "The combination of issuer and issuerAssignedId must be unique" [S20]. An OAuth 2.0
   provider with no issuer gets a fixed issuer per provider type, as Graph does (`facebook.com`)
   [S20]: the provider's own origin, `https://www.facebook.com` and `https://www.amazon.com`
   **(ours)**. GitHub's is `https://github.com/login/oauth`, the RFC 9207 `iss` it sends on its
   callback, which its docs do not state: found on the first real sign in.
2. **An email alone never signs in to an existing user.** Email "MUST NOT be used as unique
   identifiers", and an issuer "MAY re-use an email Claim Value across different End-Users" [S4 §5.7];
   Microsoft: never use `email`, `preferred_username` or `upn` to identify or authorize [S12 "Validate
   the subject"].
3. **Linking needs the user signed in.** A user adds an identity to their account only after proving
   they own it with a method it already has, as Microsoft's account linking pattern does [S42].
4. **An email the provider says is unverified never creates a user.** Google's `email_verified`
   false, or a Microsoft work account without `xms_edov`, links to an existing user but cannot sign
   up. Facebook and Amazon emails count as verified by account policy (see above). Password sign up
   does not confirm email at all, which is the larger squatting gap; IAM wide email confirmation is
   `Plans/New/email-verification.md`, a separate plan that reads `FederatedIdentities.EmailVerified`.
   This plan only records that column and does not depend on it **(ours)**.
5. **Sign up is a per provider setting**, on by default. With it off, the provider still signs in
   and links, and its button leaves the sign up page.
6. **One identity per provider per user**, unique on user and provider type, as `GoogleAuths` is one
   per user today, and one user per identity, unique on issuer and subject.
7. **A user's last way to sign in cannot be removed**, whatever it is: unlinking the only identity
   with no password is refused, as unlinking Google is today.
8. **Two factor applies to every federated sign in** when the user has TOTP on, as it does for Google
   today: the callback ends in the same `MfaRequiredChallenge`.
9. **Sign up names the user as Google sign up does today** (`RegistrationService`): the email's local
   part, then the same with a random suffix on a collision, retried. The user can change it later.
10. **No email, no sign up.** An identity with no usable email (a Facebook account registered with a
    phone, a GitHub account with no verified primary) can be linked but cannot sign up, and the page
    says why.
11. **An email already on another user refuses sign up.** `Users.Email` is unique, and joining the
    identity to that user would be the takeover the rules above forbid. The page says an account
    already uses the email, and to sign in and link the provider from the profile.
12. **Microsoft** identifies by issuer and subject, the issuer carrying `tid` [S11, S12]. The email
   counts as verified for a personal account (tenant `9188040d-6c67-4c5b-b112-36a304b66dad` [S11]) or
   when the `xms_edov` optional claim is true [S13]; a work account with neither links but cannot
   sign up. The app registration requests `xms_edov` and keeps unverified emails out of tokens
   (`removeUnverifiedEmailClaim`) [S14]. The tenant segment is a setting, `common` by default, with
   `organizations`, `consumers` or one tenant ID allowed [S16 "Send the sign-in request"].

## Proving it is you

1. A user with a password uses it, as today. A user without one makes a fresh round trip to one of
   their linked, enabled providers. The callback records a proof for that user for five minutes, and
   the action proceeds, followed by two factor when it is on.
2. **The page picks up where it left.** The dialog asking for proof is inside a component, and the
   round trip leaves the page. The start endpoint's return path carries which action was waiting, and
   the page reopens that dialog with the proof in place **(ours)**.
3. **Where the IdP supports forcing a fresh sign in, IAM asks for it.** Microsoft takes
   `prompt=login`, which "forces the user to enter their credentials" [S15 "Request an authorization
   code"]. OpenID Connect's `max_age=0` "is equivalent to prompt=login" and makes the ID token carry
   `auth_time` [S4 §3.1.2.1], which the client checks [S4 §3.1.3.7 step 13].
4. **Where it does not, IAM accepts what the IdP gives.** Google documents only `none`, `consent` and
   `select_account` [S9] and returns `auth_time` only when enabled [S7]; GitHub documents only
   `prompt=select_account` [S22]; Facebook's `auth_type=rerequest` re-asks permissions, not
   credentials [S27 "Re-asking for Declined Permissions"]; Amazon documents none [S32]. For these the
   proof is that the person completed a fresh round trip, bound by `state` and, for Google, the
   nonce, as the subject linked to the user; the IdP may carry them on its own session.

## Client credentials

1. **RFC 9700 recommends asymmetric client authentication** [S1 §2.5], and so does OWASP [S38].
   Microsoft supports it (`private_key_jwt` with a certificate) [S17, S19] and says client secrets
   "should **not be used** in production" [S18]. Google, GitHub, Facebook and Amazon offer only a
   shared secret [S10, S22, S27, S32].
2. So Microsoft takes either, and the platform member chooses: the Security page offers
   "Certificate (preferred)" and "Client secret", and the docs say why. The other providers take a
   secret.
3. **A certificate is uploaded as a PFX**; IAM keeps the private key, encrypted like a secret, and
   the thumbprint, which the signed assertion's header carries as Microsoft's certificate credential
   format requires [S19].
4. **Credentials are write only.** Stored as `ISymmetricEncryptedValue` under the system key,
   following OWASP's "robust encryption at rest" [S40 §4.1]; never shown again (the page says "set"
   and offers "replace"), as Microsoft never displays a secret again after creation [S18]; never
   logged [S39]; never audited by value; read only by the code that redeems a code.
5. **Rotation.** Microsoft secrets last at most 24 months, with under 12 recommended [S18], and OWASP
   says to "regularly rotate secrets" [S40 §2.7.2]. Replacing a credential is one save; the docs give
   each provider's rotation steps.

## Settings

1. **Configuration until there is a platform account, the platform's saved settings after**, the
   existing security settings rule. Hosts without a platform account configure providers under
   `SecurityOptions:IdentityProviders`, credentials through the host's secret store. Bootstrap's
   `--settings` copies them into the platform's settings, encrypting the credentials; startup warns
   when configuration still carries them alongside saved settings.
2. **A new table, one row per provider type**: enabled, sign up allowed (default on), client ID,
   encrypted credential, and the type's own settings (Microsoft's tenant segment). It sits under `platform_security_settings`
   (`SecurityPolicy.RESOURCE_TYPE`), on the Security page in its own section, audited and cached like
   the rest of the security settings.
3. **Turning a provider off keeps its configuration and its users' linked identities.** Turning it
   back on restores both.
4. **Turning a provider off warns first.** The confirm names how many users have that provider as
   their only way to sign in and says they cannot sign in, or prove it is them, until it is back on.
5. **A provider that is off is invisible.** Sign in, sign up, Linked accounts and the prove it is you
   dialog list only enabled providers, with no "not configured" text.

## Per account configuration

None in this release. Every setting is platform wide. Enterprise SSO (an account bringing its own
IdP, restricting its members to it, routing by email domain) is a separate feature that would add an
account owned provider row beside the platform ones; the protocol seam and the issuer and subject key
are what let it come later without rework.

## Names

Industry terms where they exist, house conventions for the rest.

| Thing | Name |
|---|---|
| Linked identities | Domain `FederatedIdentities`, entity `FederatedIdentityEntity` (user, provider type, issuer, subject, email), replacing `GoogleAuths` |
| Provider settings | Domain `IdentityProviders`, entity `IdentityProviderEntity` |
| Provider types | `IdentityProviderType` (`Google`, `Microsoft`, `GitHub`, `Facebook`, `Amazon`) |
| Protocols | `FederationProtocol` (`OpenIdConnect`, `OAuth2`) |
| Configuration | `SecurityOptions:IdentityProviders:<Type>` |
| Service methods | On the existing services, per CLAUDE.md: start and callback on `IAuthenticationService`, linking on `IRegistrationService`, unlinking on `IDeregistrationService`, a user's linked identities on `IRetrievalService` |

ASP.NET Core and Duende use `IdentityProvider` and `OpenIdConnectHandler` in their own namespaces;
shared generic names are resolved by namespace, as `DESIGN-DECISIONS.md` settles for result types.

## CSP

`Corely.IAM.Web/Docs/security.md` lists, per provider, exactly what a host's Content-Security-Policy
needs, even where the answer is nothing, so no host finds out by breaking.

1. The flow is top level navigation away and back. CSP's navigation directives are `form-action` and
   `frame-ancestors` [S35 §6.4], so a link that navigates the page is not governed, and no provider
   needs an entry, Google included once its script is gone. The Google entries leave the docs.
2. That holds only because the flow starts from a link. Whether `form-action` blocks a redirect after
   a form submission "is debated", and Chrome blocks it where Firefox does not [S37], so a form start
   would need every IdP's origin. A functional test pins the start endpoint to GET.
3. A functional test runs the flow under the documented policy.

## Order of the work

The order the pieces of this plan are built in. It says nothing about other plans or releases.

1. The protocol seam, the correlation cookie, start and callback, the provider settings table and
   configuration, and `FederatedIdentities` with `GoogleAuths` migrated row for row (issuer
   `https://accounts.google.com`, subject from `GoogleSubjectId`).
2. Google on the code flow through every path: sign in, sign up, link, unlink, prove it is you.
   Google's script, `google-link.js`, `GoogleIdTokenValidator` and the Google specific services and
   result codes go.
3. Microsoft with certificate and secret credentials, then GitHub, then Amazon and Facebook.
4. The Security page section, the turn off warning, CSP docs, a setup page per provider in `Docs/`
   with its registration and rotation steps (a CLI command where one exists, such as
   `az ad app create` for Microsoft), the CLAUDE.md rule change, and the migration guide.

## Tests

1. Unit: each provider's validation (audience, issuer, the Microsoft tenant issuer, expiry, nonce,
   `auth_time` where requested, unverified email), the takeover rule and verified email rule per
   provider, sign up refused when the provider's sign up setting is off, with no email, or with an
   email another user has; a second identity for the same provider refused; the last way to sign in
   kept; two factor after every federated sign in; the username suffix on a collision; that a credential never appears in a settings read, an audit entry or a log, and the turn
   off warning's count.
2. Integration: `GoogleAuths` to `FederatedIdentities` keeps every row, provider settings round trip
   with the credential encrypted at rest, and the configuration to bootstrap copy.
3. Functional: start is GET only; the callback rejects a missing cookie, a mismatched `state`, a
   reused `state`, a wrong `iss` and a cookie for another provider; an `error` response changes
   nothing; the return path is local only; the cookie's attributes; the `CrossSiteRequestTests`
   exemption; the flow under the documented CSP.
4. `PlatformSecuritySettingsMapperTests` loses its `GoogleClientId` exemption and the configuration
   test covers the new section. `CrossAccountCoverageTests` gets a reasoned exemption for each new
   method: provider settings are platform wide, and federated identities belong to users, not
   accounts.
5. No test reaches a real IdP: a test issuer signs tokens with keys the validator is pointed at, and
   the token and profile endpoints are stubs.

## Releasing

One major of Corely.IAM, Corely.IAM.Web and the migration CLI, together, with one migration guide at
the repository root:

1. `SecurityOptions:GoogleClientId` becomes `SecurityOptions:IdentityProviders:Google:ClientId`, and
   Google also needs a `ClientSecret`, created free in the same Google client.
2. `GoogleAuths` becomes `FederatedIdentities`; the migration moves every row.
3. The Google specific service methods and result codes are replaced by the provider neutral ones.
   DevTools keeps `status` and `unlink` for any provider and drops `link` and `signin-google`, which
   took a pasted Google ID token that the code flow no longer produces **(ours)**.
4. Hosts drop the Google entries from their CSP and register each provider's callback path.

## As built

Built locally on master, not yet released: Corely.IAM 7.0.0, both compiled model packages 7.0.0,
Corely.IAM.Web 10.0.0, the migration CLI 7.0.0. Waiting on a sign in against each real provider.

What turned out differently:

1. Two new services rather than methods on existing ones: `IFederatedSignInService` (list, start,
   complete) and `IFederatedIdentityService` (sign in methods, unlink). Platform settings stay on
   `IPlatformService`.
2. A stored client credential is a string encrypted with the system key, as TOTP secrets are, not an
   `ISymmetricEncryptedValue` with its own key.
3. Signing in with an unlinked identity signs up straight away when the provider allows it. There is no
   confirm page.
4. The proof of a fresh sign in lives on the `FederatedIdentities` row (`ReauthenticatedUtc` and the
   session it was made in), five minutes, used once.
5. The return URL cookie is gone: the return URL travels in the sealed correlation cookie.
6. The table name is set with `FederatedIdentityEntity.TABLE_NAME`, since EF's pluralizing gave
   `FederatedIdentitys`.
7. Every provider button is one `IdentityProviderButton` component: the same size and outline, the
   provider's own logo from its official asset pack bundled in the package, and "Sign in with" each
   provider, the only wording Microsoft allows. Google's G is cropped from Google's pre-approved
   button, Google Sans (OFL) is bundled, and Facebook is blue in dark mode because Meta allows only
   white or blue buttons. Amazon publishes no logo or custom button rules, so its gold button uses the
   Bootstrap Icons glyph: the one button outside its provider's published rules, by the owner's choice.
   Lists (linked identities, platform settings) show names only, since no provider's rules cover a
   logo outside a sign in button.
8. Sign up, and the provider buttons on sign in, show "By signing up (or continuing), you agree to our
   Terms of Use and Privacy Policy" from an overridable `_SignUpTerms` partial.
9. Proven against the real providers on localhost: Google, Microsoft (certificate credential),
   GitHub and Facebook, each through sign up or the duplicate email refusal, linking, unlinking and
   sign in, plus reauthentication through Google. Amazon is tested against stubs only: its developer
   registration is left for a real business account. Its issuer is the one unconfirmed value, so a
   first real sign in that reports an issuer mismatch shows the value in the callback URL.

## Sources

Read for this plan, at the sections cited. S47 and S48 are cited for their subject (sharing the
Data Protection key ring, and Meta's version schedule) and are read for the exact wording when that
piece is built.

| | Source |
|---|---|
| S1 | RFC 9700, OAuth 2.0 Security Best Current Practice: https://datatracker.ietf.org/doc/html/rfc9700 |
| S2 | RFC 6749, OAuth 2.0: https://datatracker.ietf.org/doc/html/rfc6749 |
| S3 | RFC 7636, PKCE: https://datatracker.ietf.org/doc/html/rfc7636 |
| S4 | OpenID Connect Core 1.0: https://openid.net/specs/openid-connect-core-1_0.html |
| S5 | OpenID Connect Discovery 1.0: https://openid.net/specs/openid-connect-discovery-1_0.html |
| S6 | RFC 9207, authorization server issuer identification: https://datatracker.ietf.org/doc/html/rfc9207 |
| S7 | Google, OpenID Connect: https://developers.google.com/identity/openid-connect/openid-connect |
| S8 | Google, verifying an ID token: https://developers.google.com/identity/gsi/web/guides/verify-google-id-token |
| S9 | Google, OAuth 2.0 for web server applications: https://developers.google.com/identity/protocols/oauth2/web-server |
| S10 | Google's discovery document (`code_challenge_methods_supported`, `authorization_response_iss_parameter_supported`, `token_endpoint_auth_methods_supported`), read 2026-11-06: https://accounts.google.com/.well-known/openid-configuration |
| S11 | Microsoft, validating tokens: https://learn.microsoft.com/entra/identity-platform/access-tokens#validate-tokens |
| S12 | Microsoft, validating claims: https://learn.microsoft.com/entra/identity-platform/claims-validation |
| S13 | Microsoft, optional claims (`xms_edov`): https://learn.microsoft.com/entra/identity-platform/optional-claims-reference |
| S14 | Microsoft Graph, `authenticationBehaviors`: https://learn.microsoft.com/graph/applications-authenticationbehaviors |
| S15 | Microsoft, authorization code flow: https://learn.microsoft.com/entra/identity-platform/v2-oauth2-auth-code-flow |
| S16 | Microsoft, OpenID Connect: https://learn.microsoft.com/entra/identity-platform/v2-protocols-oidc |
| S17 | Microsoft's `common` discovery document (`token_endpoint_auth_methods_supported` lists `private_key_jwt`; no `authorization_response_iss_parameter_supported`), read 2026-11-06: https://login.microsoftonline.com/common/v2.0/.well-known/openid-configuration |
| S18 | Microsoft, adding application credentials: https://learn.microsoft.com/entra/identity-platform/how-to-add-credentials |
| S19 | Microsoft, certificate credentials: https://learn.microsoft.com/entra/identity-platform/certificate-credentials |
| S20 | Microsoft Graph, `objectIdentity`: https://learn.microsoft.com/graph/api/resources/objectidentity |
| S21 | GitHub, authorizing OAuth apps: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/authorizing-oauth-apps |
| S22 | GitHub, user access tokens for a GitHub App: https://docs.github.com/en/apps/creating-github-apps/authenticating-with-a-github-app/generating-a-user-access-token-for-a-github-app |
| S23 | GitHub, GitHub Apps and OAuth apps: https://docs.github.com/en/apps/oauth-apps/building-oauth-apps/differences-between-github-apps-and-oauth-apps |
| S24 | GitHub, emails API: https://docs.github.com/en/rest/users/emails |
| S25 | GitHub, users API: https://docs.github.com/en/rest/users/users |
| S26 | GitHub, changing your username: https://docs.github.com/en/account-and-profile/setting-up-and-managing-your-personal-account-on-github/managing-user-account-settings/changing-your-github-username |
| S27 | Meta, manually building a login flow: https://developers.facebook.com/docs/facebook-login/guides/advanced/manual-flow |
| S28 | Meta, OIDC token flow: https://developers.facebook.com/docs/facebook-login/guides/advanced/oidc-token |
| S29 | Meta, Graph API user: https://developers.facebook.com/docs/graph-api/reference/user |
| S30 | Meta, securing requests: https://developers.facebook.com/docs/graph-api/securing-requests |
| S31 | Meta, permissions: https://developers.facebook.com/docs/permissions |
| S32 | Amazon, authorization code grant: https://developer.amazon.com/docs/login-with-amazon/authorization-code-grant.html |
| S33 | Amazon, customer profile: https://developer.amazon.com/docs/login-with-amazon/customer-profile.html |
| S34 | Amazon, obtaining the customer profile: https://developer.amazon.com/docs/login-with-amazon/obtain-customer-profile.html |
| S35 | W3C, Content Security Policy Level 3: https://www.w3.org/TR/CSP3/ |
| S36 | MDN, `Set-Cookie` (`SameSite`, cookie prefixes): https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Set-Cookie |
| S37 | MDN, `form-action`: https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/Content-Security-Policy/form-action |
| S38 | OWASP, OAuth 2.0 cheat sheet: https://cheatsheetseries.owasp.org/cheatsheets/OAuth2_Cheat_Sheet.html |
| S39 | OWASP, logging cheat sheet: https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html |
| S40 | OWASP, secrets management cheat sheet: https://cheatsheetseries.owasp.org/cheatsheets/Secrets_Management_Cheat_Sheet.html |
| S41 | OWASP, session management cheat sheet: https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html |
| S42 | Microsoft, account linking with matching criteria: https://learn.microsoft.com/azure/active-directory-b2c/b2c-global-identity-region-based-design#account-linking-with-matching-criteria |
| S43 | GitHub, permissions required for GitHub Apps: https://docs.github.com/en/rest/authentication/permissions-required-for-github-apps |
| S44 | Meta, access levels: https://developers.facebook.com/docs/graph-api/overview/access-levels/ |
| S45 | Auth0, Facebook connection does not return the email: https://support.auth0.com/center/s/article/facebook-social-connection-does-not-return-user-email-address |
| S46 | Amazon help, verify your email for a new account: https://digprjsurvey.amazon.com/csad/help/node/GU3U2EU63RCFDAP2 |
| S47 | Microsoft, configuring ASP.NET Core Data Protection (sharing the key ring across instances): https://learn.microsoft.com/aspnet/core/security/data-protection/configuration/overview |
| S48 | Meta, Graph API versioning: https://developers.facebook.com/docs/graph-api/guides/versioning |
