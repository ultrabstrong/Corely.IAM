# Invitations

Token-based invitation system for onboarding users to accounts. Invitations are scoped to an account and targeted to an email address.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Unique identifier |
| `AccountId` | `Guid` | Target account |
| `CreatedByUserId` | `Guid` | Inviting user |
| `Email` | `string` | Invitee's email |
| `Description` | `string?` | Optional note |
| `ExpiresUtc` | `DateTime` | Expiry timestamp |
| `AcceptedByUserId` | `Guid?` | User who accepted (null if pending) |
| `AcceptedUtc` | `DateTime?` | Acceptance timestamp |
| `RevokedUtc` | `DateTime?` | Revocation timestamp |
| `CreatedUtc` | `DateTime` | Creation timestamp |

## Lifecycle

```
Created (Pending) → Accepted
                  → Expired (automatic, time-based)
                  → Revoked (manual)
```

## Status Methods

| Method | Returns `true` when |
|--------|-------------------|
| `IsPending(utcNow)` | Not expired, not revoked, not accepted |
| `IsExpired(utcNow)` | `ExpiresUtc < utcNow` |
| `IsRevoked` | `RevokedUtc != null` |
| `IsAccepted` | `AcceptedByUserId != null` |

## Key Behaviors

- The token is `{invitationId}.{secret}`, generated on creation. Only the secret's hash is stored,
  so the token cannot be retrieved again
- Accepting an invitation adds the user to the account
- Email must match the authenticated user's email when accepting
- Creating an invitation for a user already in the account returns `UserAlreadyInAccountError`
- Revoking is only possible for pending invitations
- Expiry durations are configurable at creation time (e.g., 1h, 1d, 7d, 30d)

## Delivery

IAM sends nothing itself. A host that wants invitations delivered (by email, chat or anything else)
registers an `IInvitationSender`; with none registered, the token goes back to the inviter to hand
over.

```csharp
services.AddScoped<IInvitationSender, EmailInvitationSender>();

internal sealed class EmailInvitationSender(IMailer mailer) : IInvitationSender
{
    public async Task<InvitationSendResult> SendAsync(InvitationDelivery delivery)
    {
        var link = $"https://app.example.com/accept-invitation?token={Uri.EscapeDataString(delivery.Token)}";
        await mailer.SendAsync(delivery.Email, delivery.AccountName, link, delivery.ExpiresUtc);
        return new(InvitationSendResultCode.Sent, $"Invitation emailed to {delivery.Email}.");
    }
}
```

The sender is called right after the invitation is saved, for every caller (the portal and the API
alike), and builds its own link. `InvitationDelivery` carries the invitation's id, account id and
name, email, description, expiry, the inviter's username and email, and the token.

`CreateInvitationResult.Delivery` says what happened:

| `Delivery` | `Token` | `DeliveryMessage` |
|------------|---------|-------------------|
| `NotAttempted` | Returned | Null: no sender is registered |
| `Sent` | Null | The sender's message, or "Invitation sent to x@example.com." |
| `Failed` | Returned | The sender's reason, if it gave one |

A sender that returns `NotSent` or throws never loses the invitation: it already exists, and the
token goes back to the inviter instead. A throw is logged.

## Result Codes

### Create

| Code | Meaning |
|------|---------|
| `Success` | Invitation created; sent, or its token returned (see Delivery) |
| `UserAlreadyInAccountError` | User already in account |
| `ValidationError` | Invalid email or parameters |

### Accept

| Code | Meaning |
|------|---------|
| `Success` | User added to account |
| `InvitationExpiredError` | Token expired |
| `InvitationRevokedError` | Token revoked |
| `InvitationAlreadyAcceptedError` | Already used |
| `InvitationNotFoundError` | Invalid token |
| `EmailMismatchError` | Email doesn't match authenticated user |
