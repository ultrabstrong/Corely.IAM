# Email verification

IAM never confirms that a user controls the email on their account. Password sign up takes whatever
is typed, and `UserEntity.Email` has a unique index, so someone can register an address that is not
theirs and the owner later gets "user exists". Federated sign up (`sign-in-providers.md`) relies on
the provider having confirmed it; nothing else does.

IAM sends no email. It does what it does for password recovery: makes a token, hands it to the host
to deliver however it likes, and checks it when it comes back.

## What exists

1. **Password recovery** (`IPasswordRecoveryService`, `Docs/services/password-recovery.md`) is the
   pattern to follow: a token `{recoveryId}.{secret}`, only the secret's hash stored, a newer request
   invalidates older pending ones, a fixed expiry, single use, and the host delivers the token.
2. **Invitations** carry an email too, but bind an account membership to an address; accepting one
   checks the signed in user's email matches (`InvitationProcessor`, `EmailMismatchError`). They
   prove nothing about the user's own address.
3. `UpdateUserRequest` can change a user's email.

## The shape

1. **`EmailVerifiedUtc` on the user**, null until confirmed, exposed on the `User` model. Changing the
   email clears it and invalidates pending verifications.
2. **A verification is bound to the address**, not just the user: the record stores the email it was
   sent to, and confirming succeeds only while the user's email still equals it. Changing the email
   after sending cannot confirm the new one with the old link.
3. **`IEmailVerificationService`**, beside `IPasswordRecoveryService`:

   | Method | Context | Does |
   |---|---|---|
   | `RequestEmailVerificationAsync()` | The signed in user, not system | Returns a token for the user's current email, invalidating older pending ones. `AlreadyVerifiedError` when it is confirmed |
   | `RequestEmailVerificationAsync(Guid userId)` | System context | The same for a host that sends at sign up or in a batch |
   | `VerifyEmailAsync(VerifyEmailRequest)` | None | Takes the token; confirms when it is valid, pending, unexpired and the address still matches |

   The token is `{verificationId}.{secret}` with the secret hashed, as for recovery. Result codes
   mirror recovery's: not found, expired, already used, invalidated, plus `EmailChangedError`.
4. **Federated identities count.** `sign-in-providers.md` records on each linked identity whether
   the provider vouched for its email (`FederatedIdentities.EmailVerified`). A user whose current
   email matches a linked identity the provider vouched for is confirmed, at sign up, at link, and
   once for existing rows when this plan lands. This plan reads that column; the federated plan
   does not depend on this one.
5. **Lifetime is a security setting**, `EmailVerificationTtlSeconds` on `SecurityPolicy` with a range,
   in configuration until a platform account and on the Security page after, like every other
   setting. 1800 (30 minutes) by default.
6. **Resend has a cooldown**, so the page cannot be used to flood an inbox: a new link at most once
   a minute per user (`EmailVerificationResendSeconds`, also a setting), and each new link
   invalidates the last.
7. **The status is readable**, so a page can say the right thing:
   `GetEmailVerificationStatusAsync(challengeToken)` returns sent (with when, and when it expires),
   expired, confirmed, or none sent yet, and when the next resend is allowed.
8. **A confirmed challenge finishes the sign in.** The challenge token from sign up or sign in is
   exchanged for a session once the email is confirmed, within the challenge's lifetime, so the
   waiting tab carries on by itself and nobody types a password twice.
9. **A link opened twice still says confirmed.** Mail scanners open links before people do; a link
   whose verification is already complete for that address shows success, not "already used".
10. **Confirmation is required by default once a host can send.** `RequireVerifiedEmail` on
    `SecurityPolicy` is on by default, and applies only while an `IEmailVerificationSender` is
    registered; a host without one sees no change, and the setting cannot be saved on without one. A
    host or platform member can turn it off. While it applies:
    1. Sign in by any method returns `EmailVerificationRequiredChallenge` with a challenge token, the
       way `MfaRequiredChallenge` works, instead of a session. The challenge token does one thing:
       `RequestEmailVerificationAsync(challengeToken)` sends a fresh link, and once the email is
       confirmed the same token is exchanged for the session (item 8).
    2. Accepting an invitation needs a confirmed email.
    3. Password recovery is unaffected: it already proves the inbox, so completing one also sets
       `EmailVerifiedUtc`.
11. **Who is confirmed without a link:**
    1. A federated sign up or link the provider vouches for (`sign-in-providers.md`).
    2. The platform owner at bootstrap: the operator supplies the address.
    3. A user registered under system context with `EmailVerified = true` on the request: the host
       vouches, as for imported users and the demo seed (`SeedWebAppDemo.ps1`, `Start-LocalStack.ps1`).
    4. A completed password recovery.
12. **Squatting is reclaimed through password recovery.** It mails the address, so the real owner
   can reset the squatter's password and take the user over. The docs say so; nothing else is built.

## Corely.IAM.Web

Corely.IAM.Web provides as much as it can out of the box, as it does for bot checks: it owns the
pages, and the host supplies only what it alone can do, sending the email.

1. **The pages are Corely.IAM.Web's**: the "Confirm your email to continue" page and the confirm
   link's page, Razor pages at authentication paths in `AppRoutes`, like sign in.
2. **Sending is the host's**, through `IEmailVerificationSender.SendAsync(email, confirmUrl)`,
   registered by the host. Corely.IAM.Web builds the URL. With no sender registered the requirement
   does not apply (item 10), and the Security page says so beside the setting. `Corely.IAM.WebApp` registers a sender that shows the link on a preview
   page in Development, as password recovery's preview does.
3. **The link confirms on GET.** The CLAUDE.md rule against cross site GETs that change state guards
   against ambient authority: another site using the signed in user's cookie. Here the authority is
   the unguessable token, which confirms only the one address it was sent to and signs nobody in. A
   link someone was tricked into clicking can only confirm the trickster's own address.
   `CrossSiteRequestTests` names the confirm path as an exemption with that reason.
4. **The profile shows the state**: "Confirmed", or "Not confirmed" with Resend.

### The user's path

1. Sign up, or sign in unconfirmed: a "Confirm your email to continue" page, naming the address,
   with "Check your spam folder" and a Resend button that counts down its cooldown.
2. Click the link in the email: that tab says the email is confirmed.
3. The waiting tab notices (it checks the status every few seconds) and continues signed in.
4. Link missed or expired: signing in lands on the same page, saying "Your link expired" with Resend.

## Tables

| Table | Columns |
|---|---|
| `Users` | adds `EmailVerifiedUtc` (nullable) |
| `EmailVerifications` (new) | `Id`, `UserId`, `Email`, `SecretHash`, `ExpiresUtc`, `CompletedUtc`, `InvalidatedUtc`, `CreatedUtc` |

## Sources

The token practices (random, single use, expiring, stored hashed) are OWASP's for any token sent to
an inbox. **To read** at the cited sections before building, as `sign-in-providers.md` was:

| | Source |
|---|---|
| S1 | OWASP, forgot password cheat sheet (URL tokens): https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html |
| S2 | OWASP, authentication cheat sheet: https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html |
| S3 | NIST SP 800-63B, out of band and email guidance: https://pages.nist.gov/800-63-4/sp800-63b.html |

## Releasing

A minor release of Corely.IAM and Corely.IAM.Web, on its own, independent of `sign-in-providers.md`.
Nothing in it breaks a host: the table and column are additive, the sign in result code is a new
value, and the requirement takes effect only once a host registers a sender. No released host has
users yet, so nobody is grandfathered.

## Tests

1. Unit: request, verify, expiry, reuse, invalidation by a newer request, `EmailChangedError`,
   clearing on email change, federated sign up and link setting it, the system and user context
   checks, the sign in challenge for every sign in method with the setting on and none with it off,
   invitation acceptance refused, recovery setting it, bootstrap and system registration setting it.
2. Integration: the new table and column, and that only the secret's hash is stored.
3. Functional: a sign in needing confirmation lands on the waiting page; the confirm link confirms
   on a cross site GET and signs nobody in; the waiting page continues once confirmed; resend
   honors the cooldown; the `CrossSiteRequestTests` exemption.
4. `CrossAccountCoverageTests`: a reasoned exemption; verification belongs to users, not accounts.
