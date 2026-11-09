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

## Part one: decide the fix

Settle each question with the owner, write the answer in place of the question, then build.

### Questions

1. **What does the inviter hand over: a token, a link, or nothing?**
   - Token (today): the recipient has to find the accept page and paste it.
   - Link: one click for the recipient; the modal shows and copies the link. The token then sits
     in a URL, so check whether it reaches logs or a `Referer` header, and whether the accept page
     should drop it from the address bar once read.
   - Nothing: the host emails the invitation (question 3) and the modal says "Invitation sent to
     x@example.com".
2. **Hidden by default?** Masked with a show toggle, Copy works without revealing it. If the answer to
   1 is a link, decide whether the link is masked too, since it carries the same secret.
3. **Does IAM get a delivery seam?** An `IInvitationSender` the host registers, as the email
   verification plan does for its links. If registered, the invitation is sent and the secret is
   never shown; if not, the inviter gets the copy flow. Decide whether this lands here or waits for
   the email verification work, so the two senders share one shape. DocsToData is the first host
   that would register one: it sends no invitation email today, but already sends others through
   SES (`DocsToData.Platform/Emails`, `SesNotificationSender`).
4. **What does the modal explain?** At least: who it's for, when it expires, that it works only for
   a user signed in with that email, and what the recipient does next (sign up or sign in with that
   email, then open the link). Whether "will not be shown again" stays, and whether a lost invitation
   is handled by revoking and inviting again, said in the modal.
5. **The recipient's side.** The accept page says "Paste your invitation token". If links become the
   normal path, decide whether the page still shows a token box, and what someone who isn't signed in
   sees: today the page requires sign in first, and after signing up they may not land back on it.
   Check whether `returnUrl` carries them back with the token intact.
6. **Is the modal a component?** If it shows a secret once with mask and copy, decide whether that
   becomes a shared component. API key creation shows a secret once too; check whether it has the
   same problem and should share the fix.

### How to settle them

1. Walk the flow in `Corely.IAM.WebApp` (`.\Start-LocalStack.ps1`): create an invitation, hand it
   to a second seeded user, accept it, including the not yet registered case.
2. Look at how two or three products handle a team invite handoff, for the explanation text and
   whether they show a link.
3. Read the API key creation modal for question 6.

## Part two: build

Filled in once part one is settled. Expected to touch at least `AccountDetail.razor`,
`AcceptInvitation.razor`, `Docs/components/invitations.md`, `Docs/components/accounts.md`, and
`Corely.IAM.WebApp`. A delivery seam would add a public interface in `Corely.IAM.Web` and a
Development preview sender in the WebApp.

## Proving it in a consumer

DocsToData hosts the accept page (`AcceptInvitationPage.razor`) and its links
(`PortalIamLinks.cs`). If the link or the accept page changes, prove it there with
`scripts/Use-LocalBuild.ps1` before tagging.

## Tests

Decided with the fix. At minimum: the link's URL comes from `IIamWebLinks.AcceptInvitation()`, the
secret is not in the rendered markup until revealed (if masked), and the accept page still accepts a
pasted token if it keeps the box.
