# Invitation handoff

Creating an invitation on Account Detail opens an "Invitation Created" modal that shows the raw
token in plain text, under "Copy this token, it will not be shown again", with nothing else. It
doesn't say what the token is, who it is for, what to do with it, or how the recipient uses it. It
also shows the secret in clear by default, readable by anyone watching the screen, a screen share or
a recording. Found on DocsToData dev, 2026-11-09.

What "fixed" looks like is not decided. Part one of this plan settles that; part two builds it.

## What exists

1. `IInvitationService.CreateInvitationAsync` returns the token once, in `CreateInvitationResult`.
   Only the token is a secret; the invitation's email, description and expiry are listed on the page.
2. IAM sends no email. Delivery is the host's (`account-invitations.md`), so today the inviter has to
   copy the token and get it to the recipient by hand.
3. `AcceptInvitation` (`Components/Invitations/AcceptInvitation.razor`) is a signed in page with a
   token box, prefilled from `?token=`, and an Accept button. The host gives it a path through
   `IIamWebLinks.AcceptInvitation()`. Accepting requires the signed in user's email to match the
   invitation's (`EmailMismatchError`).
4. So a link `{AcceptInvitation()}?token=...` already works end to end. Nothing builds it or tells
   anyone it exists.
5. The modal is inline markup in `AccountDetail.razor` (lines 242 to 268), not a shared component,
   and its text uses a dash as punctuation, which the documentation rules forbid.
6. `email-verification.md` (in `Plans/New/`) proposes a host registered sender,
   `IEmailVerificationSender`, with a Development preview page. The same pattern could deliver
   invitations.
7. Invitations can also be created through the API (`MapIAMAccountEndpoints`), which returns the
   token the same way.
8. The modal wraps its text and token in an `alert-success` box, more clutter than one line and a
   field need. `ApiKeySecretModal` already shows a once only secret as a line of text and a
   monospace field with Copy.
9. A recipient with no login loses the token on sign up: the sign in page's Sign Up link drops
   `ReturnUrl`, and `Register` takes no return URL.
10. `InvitationEntity.Token` is stored in clear and looked up by equality, where password recovery
    stores only a hash.

## Part one: decide the fix

Settle each question with the owner, write the answer in place of the question, then build.

### Questions

1. **What does the inviter hand over?** *Decided:* a link, `{AcceptInvitation()}?token=...`,
   never a bare token; nothing at all when the host's sender delivered it (question 3). The accept
   page drops the token from the address bar once read. `Referer` is no concern: browsers default to
   `strict-origin-when-cross-origin`, which sends another site only the origin. A host's request log
   may record the query string; what makes that acceptable is that a token is useless to anyone not
   signed in with the invited address, and it is single use.
2. **Hidden by default?** *Decided: no.* The link is shown once, to the person who just made it, and
   Copy is the main action. API keys are not masked either.
3. **Does IAM get a delivery seam?** *Decided: yes, `IInvitationSender`, landing here without
   waiting for email verification.* It is the usual shape: ASP.NET Core Identity calls a host's
   `IEmailSender<TUser>`, and hosted services (Auth0, Clerk) send themselves with a flag to hand
   the link back instead. The library owns when to send, the host owns how. A decorator on
   `IInvitationService` was considered and rejected: IAM's modal would still need to learn the
   outcome, so IAM defines the seam either way, and every host would rebuild the plumbing.
   1. **Optional, channel agnostic.** A host registers one or none. IAM never knows whether it is
      email, chat or anything else.
   2. **Called by the service**, in Corely.IAM, right after the invitation is saved, so the portal
      modal and the API behave the same. It receives the invitation's facts (email, account, who
      invited, description, expiry) and the token, and builds its own link from its own routes.
   3. **It returns a result**, not an exception: sent, with an optional message, or not sent. An
      exception from the sender is logged and treated as not sent, because the invitation already
      exists and the inviter needs the link.
   4. **`CreateInvitationResult` carries the delivery outcome.** Sent: no token is returned, and the
      modal and API show the host's message, or by default "Invitation sent to x@example.com". Not
      sent, or no sender: the token is returned and the modal shows the copy link flow.
   5. **No fixed "show link" setting**: whether the link shows depends on this send, so a failed
      send never loses an invitation.
   6. `Corely.IAM.WebApp` registers a Development preview sender, as email verification proposes.
   DocsToData is the first host that would register one, through its SES notification path
   (`DocsToData/Plans/New/invitation-email.md`).
4. **What does the modal explain?** *Decided:* "Send this link to x@example.com. It works only when
   they sign in with that address, and expires on [date]." Then the field with Copy, then "Shown
   once. If it is lost, revoke the invitation and invite again." The date is the invitation's own.
5. **The recipient's side.** *Decided:* with a token in the link, the page shows an Accept button and
   no box. The paste box shows only when the page is opened without a token. Sign up carries the
   return URL (finding 9), so a new user lands back on the page with the token.
6. **Is the modal a component?** *Decided: yes.* One shared "shown once" modal, no green box: a line
   of text and a monospace field with Copy attached. Invitations and API keys both use it.
7. **Finding 9, sign up loses the token.** *Decided:* fixed in this release. The Sign Up link and
   `Register` carry `ReturnUrl`, for password and federated sign up.
8. **Finding 10, tokens stored in clear.** *Decided:* fixed in this release, the way password
   recovery does it: the token is `{invitationId}.{secret}`, and only the secret's hash is stored.
   Invitations pending when the migration runs stop working and are invited again; no host has
   customers yet.

### How to settle them

1. Walk the flow in `Corely.IAM.WebApp` (`.\Start-LocalStack.ps1`): create an invitation, hand it
   to a second seeded user, accept it, including the not yet registered case.
2. Look at how two or three products handle a team invite handoff, for the explanation text and
   whether they show a link.
3. Read the API key creation modal for question 6.

## Part two: build

Built, as Corely.IAM 7.2.0, both compiled models 7.2.0, Corely.IAM.DataAccessMigrations.Cli 7.1.0
and Corely.IAM.Web 11.2.0. Not yet tagged: the consumer proof below comes first.

1. **Seam**: `IInvitationSender` in `Corely.IAM.Invitations.Providers`, taking `InvitationDelivery`
   and returning `InvitationSendResult`. `InvitationProcessor` takes it as an optional constructor
   parameter and calls it after saving, so the sender sits behind the authorization decorator. The
   plan said the service would call it; the processor already holds the account name and the
   inviter, so it calls it instead.
2. **Result**: `CreateInvitationResult` gains `ExpiresUtc`, `Delivery` (`InvitationDeliveryStatus`:
   `NotAttempted`, `Sent`, `Failed`) and `DeliveryMessage`. The create telemetry no longer logs the
   result, which carries the token. The API's created response carries the same fields.
3. **Token**: `InvitationToken`, `{invitationId}.{secret}`; `InvitationEntity.SecretHash` replaces
   `Token` and its unique index (migration `HashInvitationTokens`).
4. **Web**: `ShownOnceModal` in `Components/Shared` replaces `ApiKeySecretModal` and the inline
   invitation modal. `AcceptInvitation` shows an Accept button for a link and drops the token from
   the address bar. Sign in's Sign Up tab and `Register` carry `ReturnUrl` (`string.WithReturnUrl`).
5. **WebApp**: `PreviewInvitationSender` and `/invitation-preview`, switched by
   `DemoFeatures:InvitationPreview` (`Send`, `Off`, `Fail`).
6. **Not built**: the accept page does not name the account before accepting. That needs a lookup
   by token before acceptance, a new service method, and the page reads clearly without it.

## Proving it in a consumer

DocsToData hosts the accept page (`AcceptInvitationPage.razor`) and its links
(`PortalIamLinks.cs`). If the link or the accept page changes, prove it there with
`scripts/Use-LocalBuild.ps1` before tagging.

## Tests

1. Unit: `InvitationTokenTests`; `InvitationProcessorTests` for the stored hash, a wrong secret,
   and each delivery outcome (no sender, sent with and without a message, not sent, a throwing
   sender) and the facts the sender receives; the two create telemetry tests assert the result is
   not logged (shown red with logging restored); `StringExtensionsTests.WithReturnUrl`.
2. Functional: `ReturnUrlTests` for the Sign Up link and sign up landing on the return URL, and never
   off site. Both new cases were shown red against the old pages.
3. Integration: the provider matrix on SQL Server and MySQL containers, for the migration.
