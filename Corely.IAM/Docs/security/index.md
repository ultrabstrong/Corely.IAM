# Security

Corely.IAM security is built on `Corely.Security` primitives with no external service dependencies.

## Key Principles

- **System key provisioning** — the system encryption key is supplied by the host via `ISecurityConfigurationProvider`, never stored in code
- **Encryption at rest** — all stored keys (account and user key pairs) are encrypted using the system key
- **No secrets in code** — no hardcoded keys, connection strings, or sensitive values
- **Pluggable algorithms** — crypto algorithms configurable via `IAMOptions` builder

## Topics

- [Key Management](key-management.md) — system keys, account keys, user keys, encryption providers
- [User Context](user-context.md) — `UserContext`, `IUserContextProvider`, host-agnostic auth
