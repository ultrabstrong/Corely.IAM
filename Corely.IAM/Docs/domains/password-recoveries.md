# Password Recoveries

Short-lived, user-scoped recovery records used to reset a password without an authenticated user context.

## Model Properties

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `Guid` | Recovery record identifier and token prefix |
| `UserId` | `Guid` | User being recovered |
| `SecretHash` | `IHashedValue` | Hashed recovery secret; plaintext token is never stored |
| `ExpiresUtc` | `DateTime` | Token expiry timestamp |
| `CompletedUtc` | `DateTime?` | Set when the recovery is consumed |
| `InvalidatedUtc` | `DateTime?` | Set when a newer recovery replaces this one |
| `CreatedUtc` | `DateTime` | Record creation timestamp |

## Relationships

- **User** — 1:M (a user can have many recovery records over time)

## Key Behaviors

- Tokens use the format `{recoveryId}.{secret}`
- Only the hashed secret is stored
- New recovery requests invalidate older pending recoveries for the same user
- Tokens expire after 10 minutes
- Successful reset revokes all active auth tokens for the user
- Successful reset clears login lockout state and failed-attempt counters
- Recovery can create a first password for a Google-only user if the host trusts email-based recovery
