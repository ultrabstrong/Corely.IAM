# Email verification

**Status:** built as Corely.IAM 7.5.0, Corely.IAM.CompiledModel.MsSql and .MySql 7.5.0,
Corely.IAM.DataAccessMigrations.Cli 7.3.0 and Corely.IAM.Web 11.4.0. See "As built" at the end.

IAM never confirms that a user controls the email on their account. Password sign up takes whatever
is typed, and `UserEntity.Email` has a unique index, so someone can register an address that is not
theirs and the owner later gets "user exists". Federated sign up (`sign-in-providers.md`) relies on
the provider having confirmed it; nothing else does.

IAM sends no email itself. It does what it does for invitations: builds a delivery carrying a token,
hands it to the host's sender, and checks the token when it comes back.

## What exists

1. **Password recovery** (`IPasswordRecoveryService`, `Docs/services/password-recovery.md`) is the
   pattern for the token: `{recoveryId}.{secret}`, only the secret's hash stored, a newer request
   invalidates older pending ones, a fixed expiry, single use.
2. **Invitations** are the pattern for delivery: `IInvitationSender` in core
   (`Invitations/Providers`) takes an `InvitationDelivery` holding the token, the host builds the link
   with `IIamWebLinks`, and with no sender registered the token goes back to the caller. Invitations
   also carry an email, but bind a membership to an address; accepting one checks the signed in
   user's email matches (`InvitationProcessor`, `EmailMismatchError`). They prove nothing about the
   user's own address.
3. **The MFA challenge** is the pattern for pausing a sign in: `SignInAsync` stores an
   `MfaChallengeEntity` (user, device, account, expiry) and returns `MfaRequiredChallenge` with a
   token, and `VerifyMfaAsync` exchanges it for the session.
4. `UpdateUserRequest` can change a user's email.
5. `FederatedIdentities.EmailVerified` records whether a provider vouched for a linked identity's
   email, and federated sign up refuses an address already in use (`FederatedSignInService`).

## The shape

1. **`EmailVerifiedUtc` on the user**, null until confirmed, exposed on the `User` model. Changing the
   email clears it and invalidates pending verifications.
2. **A verification is bound to the address**, not just the user: the record stores the email it was
   sent to, and confirming succeeds only while the user's email still equals it. Changing the email
   after sending cannot confirm the new one with the old link.
3. **`IEmailVerificationService`**, beside `IPasswordRecoveryService`:

   | Method | Context | Does |
   |---|---|---|
   | `RequestEmailVerificationAsync()` | The signed in user, not system | Profile's Resend. Sends a link for the user's current email, invalidating older pending ones. `AlreadyVerifiedError` when it is confirmed |
   | `RequestEmailVerificationAsync(Guid userId)` | System context | The same, for a host that sends at sign up or in a batch |
   | `RequestEmailVerificationAsync(string challengeToken)` | None; the challenge token is the authority | The waiting page's Resend (item 9) |
   | `GetEmailVerificationStatusAsync(string challengeToken)` | None; the challenge token is the authority | Item 7 |
   | `VerifyEmailAsync(VerifyEmailRequest)` | None | Takes the link's token; confirms when it is valid, pending, unexpired and the address still matches |

   The token is `{verificationId}.{secret}` with the secret hashed, as for recovery. Result codes
   mirror recovery's: not found, expired, already used, invalidated, plus `EmailChangedError`, and
   `ResendCooldownError` with when the next one is allowed.
4. **Sending goes through the host, as invitations do.** `IEmailVerificationSender` in core
   (`EmailVerifications/Providers`) takes an `EmailVerificationDelivery` (user id, email, expiry,
   token) and returns a send result. The host builds the link with `IIamWebLinks.ConfirmEmail()` and
   `?token=`. With no sender registered, the request methods return the token instead, the way an
   invitation does, and the requirement in item 9 does not apply.
5. **Federated identities count.** A user whose current email matches a linked identity the provider
   vouched for is confirmed, at sign up, at link, and once for existing rows by the migration that
   adds the column (item in Tables).
6. **Lifetime is a security setting**, `EmailVerificationTtlSeconds` on `SecurityPolicy` with a range,
   in configuration until a platform account and on the Security page after, like every other
   setting. 1800 (30 minutes) by default.
7. **The status is readable**, so a page can say the right thing:
   `GetEmailVerificationStatusAsync(challengeToken)` returns sent (with when, and when it expires),
   expired, confirmed, or none sent yet, and when the next resend is allowed.
8. **Resend has a cooldown**, so the page cannot be used to flood an inbox: a new link at most once
   a minute per user (`EmailVerificationResendSeconds`, also a setting), and each new link
   invalidates the last.
9. **Confirmation is required by default once a host can send.** `RequireVerifiedEmail` on
   `SecurityPolicy` is on by default, and applies only while an `IEmailVerificationSender` is
   registered; a host without one sees no change, and the setting cannot be saved on without one. A
   host or platform member can turn it off. While it applies:
   1. **Sign in checks in a fixed order:** the credential (password or provider), then MFA when the
      user has it, then the email. A user without MFA goes straight from the credential to the email
      check. Only someone who has fully proved who they are can make IAM send a link.
   2. **An unconfirmed user gets `EmailVerificationRequiredChallenge`** with a challenge token
      instead of a session, the way `MfaRequiredChallenge` works. The challenge is stored like an MFA
      challenge (token hash, user, device, account, expiry) in `EmailVerificationChallenges`. The
      first link is sent when the challenge is created, unless a pending one is still inside its
      cooldown.
   3. **The challenge lives as long as its link.** It expires with the newest link it sent, so a
      Resend extends it and the waiting tab never runs out before the link does. It needs no setting
      of its own.
   4. **A confirmed challenge finishes the sign in.**
      `IAuthenticationService.CompleteEmailVerificationAsync(challengeToken)` exchanges the token for
      the session once the email is confirmed, and returns `EmailVerificationPendingError` until
      then, so the waiting tab carries on by itself and nobody types a password twice. Like
      `VerifyMfaAsync`, the exchange is single use.
   5. **Joining an account needs a confirmed email**, by every path: `AcceptInvitationAsync`,
      `RegisterUserWithAccountAsync` (an admin adding the user) and `RegisterAccountAsync` (the user
      creating one). Each method checks it itself, rather than relying on sign in having stopped
      the user.
   6. **Password recovery is unaffected:** it already proves the inbox, so completing one also sets
      `EmailVerifiedUtc`.
10. **A link opened twice still says confirmed.** Mail scanners open links before people do; a link
    whose verification is already complete for that address shows success, not "already used".
11. **Who is confirmed without a link:**
    1. A federated sign up or link the provider vouches for.
    2. The platform owner at bootstrap: the operator supplies the address.
    3. A user registered under system context with `EmailVerified = true` on the request: the host
       vouches, as for imported users and the demo seed (`SeedWebAppDemo.ps1`, `Start-LocalStack.ps1`).
    4. A completed password recovery.

## Squatting

Someone can register an address they do not own. The known attack built on this is account
pre-hijacking (S4): the squatter sets a password, the real owner later arrives through a provider or
recovery and gets the same user, and the squatter's password, MFA or session still works. IAM closes
it three ways:

1. **An unconfirmed user can do nothing** while the requirement applies (item 9): no session, no
   invitation. A host with no sender has neither the requirement nor this protection, and the docs
   say so beside the setting.
2. **Proving the address takes the user over.** Any confirmation of an address whose user was never
   confirmed (the link, a completed recovery, or a provider vouching for it at sign up or link)
   first removes everything that lets anyone act as that user: the password, TOTP, other linked
   identities, auth tokens and sessions, and pending recoveries and verifications. The confirming
   person then holds the only way in. The takeover is audited.
   While the requirement applies an unconfirmed user cannot have joined any account (item 9.5). On a
   host without a sender it can have, so the takeover also removes its memberships, except accounts
   it is the only owner of: those pass to the new holder, so no account is left without an owner.
3. **Sign up does not stop at an unconfirmed holder.** Federated sign up with a vouched email for an
   address held by a user never confirmed is item 2's takeover, not `EmailInUseError`. Password sign
   up for such an address replaces the holder when it has never signed in (which, while the
   requirement applies, is every unconfirmed user): the old user and its credentials are deleted and
   the new one starts unconfirmed, with its own link. Whoever controls the inbox is the only one who
   can ever finish. A holder that has signed in (a host without the requirement) still answers
   "user exists".

Unconfirmed users are not deleted on a timer. Item 3 frees an address whenever its owner turns up.

## Corely.IAM.Web

Corely.IAM.Web provides as much as it can out of the box, as it does for bot checks: it owns the
pages, and the host supplies only what it alone can do, sending the email.

1. **The pages are Corely.IAM.Web's**: the "Confirm your email to continue" page and the confirm
   link's page, Razor pages at authentication paths in `AppRoutes`, like sign in.
2. **Sending is the host's**, through `IEmailVerificationSender` (item 4), building the link from
   `IIamWebLinks.ConfirmEmail()`. With no sender registered the requirement does not apply, and the
   Security page says so beside the setting. `Corely.IAM.WebApp` registers a sender that shows the
   link on a preview page in Development, as `PreviewInvitationSender` does.
3. **The link confirms on GET.** The CLAUDE.md rule against cross site GETs that change state guards
   against ambient authority: another site using the signed in user's cookie. Here the authority is
   the unguessable token, which confirms only the one address it was sent to and signs nobody in. A
   link someone was tricked into clicking can only confirm the trickster's own address.
   `CrossSiteRequestTests` names the confirm path as an exemption with that reason.
4. **The profile shows the state**: "Confirmed", or "Not confirmed" with Resend.

### The user's path

1. Sign up, or sign in unconfirmed (after MFA, when the user has it): a "Confirm your email to
   continue" page, naming the address, with "Check your spam folder" and a Resend button that counts
   down its cooldown.
2. Click the link in the email: that tab says the email is confirmed.
3. The waiting tab notices (it checks the status every few seconds) and continues signed in.
4. Link missed or expired: signing in lands on the same page, saying "Your link expired" with Resend.

## Tables

| Table | Columns |
|---|---|
| `Users` | adds `EmailVerifiedUtc` (nullable) |
| `EmailVerifications` (new) | `Id`, `UserId`, `Email`, `SecretHash`, `ExpiresUtc`, `CompletedUtc`, `InvalidatedUtc`, `CreatedUtc` |
| `EmailVerificationChallenges` (new) | `Id`, `UserId`, `TokenHash`, `DeviceId`, `AccountId`, `ExpiresUtc`, `CompletedUtc`, `CreatedUtc` |

1. Both new tables reference `Users` with a cascading delete, as `PasswordRecoveries` and
   `FederatedIdentities` do, so deleting a user or purging an account removes them.
2. The migration sets `EmailVerifiedUtc` on existing users whose email matches a linked identity
   with `EmailVerified` set (item 5).

## Sources

The token practices (random, single use, expiring, stored hashed) are OWASP's for any token sent to
an inbox. **To read** at the cited sections before building, as `sign-in-providers.md` was:

| | Source |
|---|---|
| S1 | OWASP, forgot password cheat sheet (URL tokens): https://cheatsheetseries.owasp.org/cheatsheets/Forgot_Password_Cheat_Sheet.html |
| S2 | OWASP, authentication cheat sheet: https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html |
| S3 | NIST SP 800-63B, out of band and email guidance: https://pages.nist.gov/800-63-4/sp800-63b.html |
| S4 | Sudhodanan and Paverd, "Pre-hijacked accounts: An Empirical Study of Security Failures in User Account Creation on the Web", USENIX Security 2022 |

## Releasing

A minor release of Corely.IAM and Corely.IAM.Web, on its own. Nothing in it breaks a host: the
tables and column are additive, the sign in result code is a new value, and the requirement takes
effect only once a host registers a sender. No released host has users yet, so nobody is
grandfathered.

## Tests

1. Unit: request, verify, expiry, reuse, invalidation by a newer request, `EmailChangedError`,
   clearing on email change, the resend cooldown, federated sign up and link setting it, the system,
   user and challenge token contexts, the sign in challenge for every sign in method with the setting
   on and none with it off, MFA before the email check, a Resend extending the challenge, the
   challenge exchanged once and only after confirming, all three ways of joining an account refused, recovery
   setting it, bootstrap and system registration setting it, no sender returning the token.
2. Squatting: each kind of confirmation removing the squatter's password, TOTP, other identities,
   sessions and pending tokens, and its memberships except sole owned accounts; federated sign up taking over an unconfirmed holder; password sign
   up replacing a never signed in holder and refusing one that has signed in.
3. Integration: the new tables and column, only hashes stored, the cascade on user deletion and
   account purge, and the migration's backfill.
4. Functional: a sign in needing confirmation lands on the waiting page; the confirm link confirms
   on a cross site GET and signs nobody in; the waiting page continues once confirmed; resend
   honors the cooldown; the `CrossSiteRequestTests` exemption.
5. `CrossAccountCoverageTests`: a reasoned exemption; verification belongs to users, not accounts.

## As built

What turned out differently:

1. **The link never takes a user over.** The person opening a link is normally the one who signed up,
   so wiping credentials on it would wipe their own password. Only proof from someone else takes over:
   a completed password recovery, and a provider vouching for the address at federated sign up. A
   provider vouching through an identity already linked (at sign in or at link) confirms without a
   takeover, and so does bootstrap. The confirmation email says to ignore it when the reader did not
   sign up.
2. **Password sign up replaces an unconfirmed holder only while the requirement applies.** Without a
   sender nobody can prove the inbox, so a replacement there would let any later sign up delete the
   earlier one. Elsewhere the holder still answers "user exists". A holder that is the only owner of an
   account is kept.
3. **Distinct method names, not overloads:** `RequestEmailVerificationForUserAsync(Guid)` and
   `ResendEmailVerificationAsync(string)`, because the audit and coverage tests key on method names.
4. **The challenge outlives its newest link by one link lifetime**, so the waiting page can still offer
   Resend after the link expires; each Resend extends it.
5. **`EmailVerifications.SentUtc`** carries the send time for the cooldown, because `CreatedUtc` is
   set by the database clock, which a test clock cannot move.
6. **No `IIamWebLinks.ConfirmEmail()`.** The confirm page is a Corely.IAM.Web Razor page at
   `AppRoutes.ConfirmEmail`, so a sender builds the link from that route, as `PreviewEmailVerificationSender`'s
   preview page does.
7. **The setting can be saved on without a sender.** It is stored either way and applies only while a
   sender is registered; the Security page says so instead of refusing the save.
8. **The takeover is logged, not given an audit entry of its own.** The call that caused it (a
   verification, a recovery, a federated sign in) is audited as usual.
9. **`EmailVerificationChallenges` holds the sign in's account id**, so `PurgeAccountDataAsync` deletes
   its rows with the account, beside MFA challenges.
10. **Seeding:** the DevTools `register user` command runs as the system when a request sets
    `EmailVerified`, which `SeedWebAppDemo.ps1` does. The functional test host turns the requirement
    off by default and confirms its seeded owner; `EmailVerificationTests` turns it on.

11. **The confirm page sends `Referrer-Policy: no-referrer`**, since its address carries the token
    (S1, URL tokens step 3).

12. **The SQL Server backfill runs through `EXEC(N'...')`**, as `SessionHardening`'s does. The
    idempotent script puts the `ALTER TABLE` and the `UPDATE` in one batch, and SQL Server compiles
    the batch before the column exists. DocsToData's portal tests, which build from that script, found
    it; the provider matrix's idempotent script test proves the fix.

13. **The link no longer confirms on GET.** Opening it shows "Confirm my email" and "I didn't sign up",
    each a post with an antiforgery token, so a mail scanner that opens it first confirms nothing and
    `/confirm-email` needs no exception to the cross site GET rule. "I didn't sign up" calls
    `RejectEmailVerificationAsync`, which, on a link still pending, deletes the unconfirmed user and so
    ends its waiting sign in. It deletes only while confirmation applies, since only then has an
    unconfirmed user surely done nothing; otherwise, or for one that is the only owner of an account,
    the user is kept with its links invalidated and sessions ended. This
    narrows the gap item 1 left: someone sent a link they did not ask for can remove the sign up
    instead of confirming it. Item 3 of the Web section is replaced by this.

14. **Takeover and deletion narrowed after the first release attempt.** A push security review found
    that every user existing before the upgrade counted as unconfirmed, and that a host without a
    sender keeps every user unconfirmed forever, so a real user who reset their password or signed in
    with a provider would be taken over. The 7.5.0 release run was cancelled before it published.
    Now:
    1. The migration confirms every user that already exists, not only those a provider vouched for.
    2. The takeover (recovery, or a provider claiming the address at sign up) happens only while
       confirmation applies; otherwise those confirm the email and touch nothing else, and federated
       sign up over an unconfirmed holder answers `EmailInUseError` as before.
    3. The takeover never removes account memberships. While confirmation applies an unconfirmed user
       cannot have gained any, so the sole owner rule in the Squatting section is gone.
    4. Neither password sign up nor "I didn't sign up" deletes a user that belongs to any account.

    This supersedes the membership parts of the Squatting section and of items 2 and 13.

### Upgrading a consumer

A consumer that builds IAM's schema from a generated script regenerates it. DocsToData's is
`iac/local/iam-schema.sql`, per its `iac/local/README.md`; with it regenerated, DocsToData's full
pass is green on a local build of these versions.

### Checked against the sources

Read after building, at the cited sections:

1. **S1 (OWASP forgot password, URL tokens):** random from a secure generator, long enough against
   brute force, stored hashed, single use, expiring, bound to one user: all met. Build the link
   without trusting the Host header: the docs' sender hard codes the origin. No referrer: added (item
   11). Rate limit token requests: the resend wait.
2. **S2 (OWASP authentication):** registration should answer the same whether or not the user exists
   ("A link to activate your account has been emailed"). Corely.IAM.Web's sign up still says which
   username or email is taken. That predates this plan and is not changed by it.
3. **S3 (NIST 800-63B-4):** email must not be an authenticator, but confirmation codes sent to
   validate an address are outside that rule. Recovery codes sent by email last at most 24 hours; the
   link lifetime's range tops out at 86400 seconds.
4. **S4 (pre-hijacking):** the root cause of all five attacks is using an account before its
   identifier is verified, and the fix is to verify before allowing any action (item 9). Its defence
   in depth: a password reset signs out every session (recovery already did), cancels pending email
   changes (IAM changes email at once, so there are none) and unlinks identities (the takeover); a
   federated sign up over a classic account must make sure one person controls both (the takeover
   wipes the classic credentials); limit how often a link can be requested (the resend wait). Pruning
   unverified accounts is optional and was left out.
