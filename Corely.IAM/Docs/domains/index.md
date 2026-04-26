# Domains

Each domain in Corely.IAM follows a consistent folder structure and naming pattern.

## Entity Relationships

```mermaid
graph TD
    A[Account] -->|M:M| U[User]
    A -->|1:M| G[Group]
    A -->|1:M| R[Role]
    A -->|1:M| P[Permission]
    A -->|1:M| I[Invitation]
    U -->|M:M| G
    U -->|M:M| R
    U -->|1:1| B[BasicAuth]
    U -->|1:M| PR[PasswordRecovery]
    G -->|M:M| R
    R -->|M:M| P

    style A fill:#4a9eff,color:#fff
    style U fill:#2ecc71,color:#fff
    style G fill:#f39c12,color:#fff
    style R fill:#e74c3c,color:#fff
    style P fill:#9b59b6,color:#fff
    style I fill:#1abc9c,color:#fff
    style B fill:#95a5a6,color:#fff
```

## Folder Structure

Each domain uses:

```
Domain/
├── Constants/        # Domain constants (SCREAMING_SNAKE_CASE)
├── Entities/         # EF Core entities
├── Models/           # Request/response/domain models
├── Processors/       # Business logic + authorization/telemetry decorators
├── Mappers/          # Entity ↔ Model mapping
└── Validators/       # FluentValidation rules
```

## Shared Patterns

- **Result pattern** — all operations return typed result objects with result codes, not exceptions
- **Account scoping** — groups, roles, permissions, and invitations are scoped to an account via `AccountId`
- **M:M relationships** — use explicit join entities with `DeleteBehavior.NoAction` (SQL Server constraint)
- **ChildRef** — lightweight `record ChildRef(Guid Id, string Name)` used in hydrated collections
- **Constants** — `SCREAMING_SNAKE_CASE`, defined in `Constants/` folder per domain

## Topics

- [Accounts](accounts.md)
- [Users](users.md)
- [Groups](groups.md)
- [Roles](roles.md)
- [Permissions](permissions.md)
- [Basic Auths](basic-auths.md)
- [Password Recoveries](password-recoveries.md)
- [Invitations](invitations.md)
