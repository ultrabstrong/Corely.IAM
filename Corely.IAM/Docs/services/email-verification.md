# IEmailVerificationService

Confirms that a user controls the email on their user. IAM sends no email: it builds a link token and
hands it to the host's `IEmailVerificationSender`, then checks the token when the link comes back.
Corely.IAM.Web provides the pages.

## Methods

| Method | Context | Returns |
|--------|---------|---------|
| `RequestEmailVerificationAsync()` | The signed in user, not system | `RequestEmailVerificationResult` |
| `RequestEmailVerificationForUserAsync(Guid userId)` | System context | `RequestEmailVerificationResult` |
| `ResendEmailVerificationAsync(string challengeToken)` | None; the challenge token is the authority | `RequestEmailVerificationResult` |
| `GetEmailVerificationStatusAsync(string challengeToken)` | None; the challenge token is the authority | `GetEmailVerificationStatusResult` |
| `VerifyEmailAsync(VerifyEmailRequest)` | None; the link's token is the authority | `VerifyEmailResult` |
| `RejectEmailVerificationAsync(VerifyEmailRequest)` | None; the link's token is the authority | `RejectEmailVerificationResult` |

`IAuthenticationService.CompleteEmailVerificationAsync(CompleteEmailVerificationRequest)` exchanges a
sign in's challenge token for the session once the email is confirmed.

## Sending

The host registers a sender. It receives the token and builds the link to Corely.IAM.Web's
`AppRoutes.ConfirmEmail` (or its own page that calls `VerifyEmailAsync`):

```csharp
services.AddScoped<IEmailVerificationSender, EmailVerificationSender>();

internal sealed class EmailVerificationSender(IMailer mailer) : IEmailVerificationSender
{
    public async Task<EmailVerificationSendResult> SendAsync(EmailVerificationDelivery delivery)
    {
        var link = $"https://app.example.com{AppRoutes.ConfirmEmail}?token={Uri.EscapeDataString(delivery.Token)}";
        await mailer.SendAsync(delivery.Email, "Confirm your email", link);
        return new(EmailVerificationSendResultCode.Sent);
    }
}
```

Corely.IAM.Web's page for the link asks "Confirm my email" or "I didn't sign up"; opening the link
alone changes nothing. Confirming confirms the user as it stands. Refusing calls
`RejectEmailVerificationAsync`, which deletes an unconfirmed user (one that is the only owner of an
account is kept, with its links invalidated and its sessions ended, and `Removed` is false). With no
sender registered, the request methods return the token in the result instead, and
confirmation is never required.

## Requiring confirmation

`RequireVerifiedEmail` is a [security setting](../platform.md#where-security-settings-come-from), on
by default, and applies only while a sender is registered. While it applies:

1. Sign in checks the credential, then two factor when the user has it, then the email. An
   unconfirmed user gets `SignInResultCode.EmailVerificationRequiredChallenge` and an
   `EmailVerificationChallengeToken` instead of a session, and a link is sent unless one went out
   within the resend wait.
2. The challenge outlives its newest link by one link lifetime, so a waiting page can offer Resend
   after the link expires. Each Resend sends a new link, invalidates the last, and extends the
   challenge.
3. `CompleteEmailVerificationAsync` returns `EmailVerificationPendingError` until the email is
   confirmed, then the session, once.
4. Joining an account needs a confirmed email: accepting an invitation, `RegisterUserWithAccountAsync`
   and `RegisterAccountAsync` return `EmailNotVerifiedError` for an unconfirmed user.

Changing a user's email clears the confirmation and invalidates pending links. A link confirms only
while the user's email still equals the address it was sent to (`EmailChangedError` otherwise).

## Confirmed without a link

| How | |
|-----|--|
| An identity provider vouches for the email | At federated sign up, at link, and at sign in through a linked identity |
| The platform owner at bootstrap | The operator supplies the address |
| `RegisterUserRequest.EmailVerified = true` under system context | The host vouches, as for imported users. Refused outside system context |
| A completed password recovery | It proves the inbox |

## Squatting

Someone can register an address they do not own. Two kinds of proof from someone else take such a
user over: a completed password recovery, and an identity provider vouching for the address at
federated sign up. Before confirming, IAM removes the user's password (not for recovery, which sets a
new one), two factor, other linked identities, sessions, pending recoveries and challenges, and its
account memberships except accounts it is the only owner of, which pass to the new holder.

Password sign up for an address held by an unconfirmed user replaces that user while confirmation
applies, so whoever controls the inbox is the only one who can ever finish. A holder that is the only
owner of an account is kept.

A host with no sender has neither the requirement nor the replacement at sign up.

## Result Codes

### Request

| Code | Meaning |
|------|---------|
| `Success` | Link created; `Delivery` says whether the sender sent it |
| `UserNotFoundError` | No such user |
| `AlreadyVerifiedError` | The email is already confirmed |
| `ResendCooldownError` | A link went out within the resend wait; `NextResendUtc` says when |
| `ChallengeNotFoundError` | The challenge token is unknown, used or expired |
| `UnauthorizedError` | Wrong context for the method |

### Verify

| Code | Meaning |
|------|---------|
| `Success` | Confirmed, including a link opened again after it confirmed |
| `EmailVerificationNotFoundError` | Token malformed or unknown |
| `EmailVerificationExpiredError` | The link expired |
| `EmailVerificationAlreadyUsedError` | Used, and the email is no longer confirmed |
| `EmailVerificationInvalidatedError` | A newer link replaced this one |
| `EmailChangedError` | The user's email changed after the link was sent |
