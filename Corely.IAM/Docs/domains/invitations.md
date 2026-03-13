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

- Token is generated on creation and returned once — it cannot be retrieved again
- Accepting an invitation adds the user to the account
- Email must match the authenticated user's email when accepting
- Creating an invitation for a user already in the account returns `UserAlreadyInAccountError`
- Revoking is only possible for pending invitations
- Expiry durations are configurable at creation time (e.g., 1h, 1d, 7d, 30d)

## Result Codes

### Create

| Code | Meaning |
|------|---------|
| `Success` | Invitation created, token returned |
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
