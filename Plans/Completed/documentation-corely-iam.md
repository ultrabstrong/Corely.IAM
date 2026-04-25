# Documentation Plan: Corely.IAM

## Status: Complete

## Overview

Documentation for the core IAM library following the [Corely Documentation Style Guide](documentation-style-guide.md). Lives in `Corely.IAM/Docs/`.

---

## Document Map

```
Docs/
├── index.md                        # Landing page + concept map + quick start
├── step-by-step-setup.md           # Numbered setup guide (1-8)
├── iam-options.md                   # IAMOptions builder + AddIAMServices
├── authentication.md                # Sign in, sign out, tokens, account switching
├── authorization.md                 # CRUDX model, resource types, wildcard, effective permissions
├── resource-types.md                # Registry, built-in types, custom types
├── services/
│   ├── index.md                     # Service layer overview + decorator pattern
│   ├── registration.md              # IRegistrationService methods
│   ├── deregistration.md            # IDeregistrationService methods
│   ├── retrieval.md                 # IRetrievalService methods + filtering/ordering/hydration
│   ├── modification.md              # IModificationService methods
│   └── authentication-service.md    # IAuthenticationService methods
├── domains/
│   ├── index.md                     # Domain overview + shared patterns
│   ├── accounts.md                  # Account model, multi-tenancy, scoping
│   ├── users.md                     # User model, M:M with accounts, context
│   ├── groups.md                    # Group model, user/role assignment
│   ├── roles.md                     # Role model, system roles, permission assignment
│   ├── permissions.md               # Permission model, CRUDX, wildcard, effective tree
│   ├── basic-auths.md              # BasicAuth model, password hashing, login metrics
│   └── invitations.md              # Invitation lifecycle, token, expiry, acceptance
├── security/
│   ├── index.md                     # Security overview
│   ├── key-management.md            # System keys, per-account/user keys, providers
│   └── user-context.md              # UserContext, IUserContextProvider, host-agnostic auth
├── architecture.md                  # Layering, decorators, result pattern, validation
└── result-codes.md                  # Complete reference of all result code enums
```

**Total: 24 documents** (7 new directories including nested index files)

---

## Document Specifications

### `index.md` — Landing Page

**Sections:**
- `# Corely.IAM Documentation`
- Overview paragraph (3-4 sentences): host-agnostic multi-tenant IAM, authentication, authorization, RBAC, no external dependencies
- Concept map (Mermaid): Host App → IAMOptions → AddIAMServices → Services (5) → Processors → Repos → DB, plus decorators and auth context
- Capabilities bullet list (bold lead + em-dash):
    - **Multi-tenant accounts** — users belong to multiple accounts with scoped RBAC
    - **CRUDX permissions** — fine-grained Create/Read/Update/Delete/Execute per resource type
    - **Token-based authentication** — JWT with custom claims, no HttpContext dependency
    - **Invitation system** — token-based onboarding with expiry and revocation
    - **Per-entity encryption keys** — account and user-scoped key pairs, stored encrypted
    - **Pluggable crypto** — configure algorithms via IAMOptions builder
    - **Resource type registry** — built-in + custom resource types for validation and UI
- `## Topics` — links to all top-level docs
- `## Quick Start` — minimal code: Create IAMOptions, AddIAMServices, resolve IRegistrationService, register user+account
- `## Database Providers` — table: MySQL, MariaDB, SQL Server

---

### `step-by-step-setup.md` — Setup Guide

**Numbered steps (following DataAccess pattern):**

1. **Install packages** — `dotnet add package` for Corely.IAM + EF provider
2. **Create ISecurityConfigurationProvider** — implement the interface, provision system key
3. **Choose database provider** — create IEFConfiguration (link to DataAccess configs docs)
4. **Configure IAMOptions** — `IAMOptions.Create()` with configuration, security provider, EF config; show optional resource type registration and crypto customization
5. **Register services** — `services.AddIAMServices(options)`
6. **Apply migrations** — reference migration CLI tool (`Corely.IAM.DataAccessMigrations.Cli`)
7. **Set user context** — show how host app sets `IUserContextSetter` after authenticating a user
8. **Use services** — basic example: register user, sign in, create account, create permission
9. **Where to next?** — links to all topic docs

---

### `iam-options.md` — Configuration

**Sections:**
- What IAMOptions does (single builder for all IAM configuration)
- `Create()` overloads (EF vs mock)
- Fluent methods: `RegisterResourceType`, `UseSymmetricEncryption`, `UseAsymmetricEncryption`, `UseAsymmetricSignature`, `UseHash`
- Default crypto codes table (Algorithm | Default Code | Constant)
- `AddIAMServices()` — what it registers (services, processors, repos, validators, security providers, resource type registry)
- Testing: mock overload for unit tests
- Code examples for production and test setup

---

### `authentication.md` — Authentication

**Sections:**
- JWT-based auth model (audience, custom claims: account_id, signed_in_account_id, device_id)
- Sign-in flow: `IAuthenticationService.SignInAsync()` → token + token ID
- Account switching: `SwitchAccountAsync()` → new token scoped to selected account
- Sign-out: `SignOutAsync()` (single token) vs `SignOutAllAsync()` (all tokens for user)
- Token validation: `IUserContextProvider.SetUserContextAsync(token)` → validation result codes
- Login metrics: failed attempts, lockout cooldown, last success timestamp
- SecurityOptions: MaxLoginAttempts, LockoutCooldownSeconds, AuthTokenTtlSeconds

---

### `authorization.md` — Authorization

**Sections:**
- Two-layer authorization model:
    - Service layer: context validation (HasUserContext, HasAccountContext)
    - Processor layer: CRUDX permission checks via `IsAuthorizedAsync(action, resourceType, resourceIds)`
- AuthAction enum: Create, Read, Update, Delete, Execute
- Resource types: built-in constants table + link to resource-types.md
- Wildcard support: `Guid.Empty` = all resources of a type, `"*"` = all resource types
- Self-ownership checks: `IsAuthorizedForOwnUser()` for BasicAuth
- Effective permissions: how permissions aggregate through roles and groups
- Code examples: checking authorization, creating permissions with wildcard

---

### `resource-types.md` — Resource Type Registry

**Sections:**
- What resource types are (compile-time concepts, not runtime)
- Built-in types table (Name | Constant | Description)
- `IResourceTypeRegistry` interface: `GetAll()`, `Get()`, `Exists()`
- Registering custom types via `IAMOptions.RegisterResourceType()`
- Case-insensitive handling, duplicate guards
- Validation: `PermissionValidator` rejects unknown resource types
- UI: resource type dropdown in permission creation
- Code example: registering custom types and querying the registry

---

### `services/index.md` — Service Layer Overview

**Sections:**
- Five services overview table (Service | Purpose | Authorization | Methods Count)
- Decorator pattern: all services wrapped with Authorization + Telemetry decorators
- Registration order (Scrutor: last registered = outermost)
- Service vs processor distinction: services are public orchestration, processors are internal business logic

---

### `services/registration.md` through `services/authentication-service.md`

**Per-service pattern:**
- One-line purpose
- Method signatures table (Method | Parameters | Returns)
- Code example per major workflow
- Result codes table (Code | Meaning)
- Notes on authorization requirements

---

### `domains/index.md` — Domain Overview

**Sections:**
- Consistent folder structure: Constants → Entities → Models → Processors → Mappers → Validators
- Entity relationship overview (Mermaid diagram showing Account → Users, Groups, Roles, Permissions M:M)
- Result pattern: all operations return typed results with codes, no exceptions
- Constants naming convention: `SCREAMING_SNAKE_CASE`

---

### `domains/accounts.md` through `domains/invitations.md`

**Per-domain pattern:**
- One-line description
- Model properties table (Property | Type | Description)
- Constants table (Constant | Value)
- Relationships (what it links to and how)
- Key behaviors (domain-specific rules, validation, lifecycle)
- Result codes table
- Code example for primary workflow

**Domain-specific content:**
- **accounts.md**: multi-tenancy model, account scoping, owner role bootstrap
- **users.md**: M:M with accounts, no direct ownership, login metrics, JWT claims
- **groups.md**: container for users + roles, scoped to account
- **roles.md**: system-defined vs user-defined, Owner role, permission assignment
- **permissions.md**: CRUDX model, resource type + resource ID, wildcard, effective permission tree (EffectivePermission → EffectiveRole → EffectiveGroup)
- **basic-auths.md**: password hashing, verify flow, lockout mechanics
- **invitations.md**: lifecycle (pending → accepted/expired/revoked), token generation, email validation, sibling burning

---

### `security/index.md` — Security Overview

**Sections:**
- System key provisioning (ISecurityConfigurationProvider)
- Encryption at rest (keys stored encrypted in DB)
- No secrets in code
- Link to Corely.Security docs for crypto primitives

---

### `security/key-management.md` — Key Management

**Sections:**
- System keys: provisioned externally, used to encrypt stored keys
- Account keys: SymmetricKey, AsymmetricKey (encryption + signature)
- User keys: same pattern
- Key provider interfaces: IIamSymmetricEncryptionProvider, IIamAsymmetricEncryptionProvider, IIamAsymmetricSignatureProvider
- Retrieval: `IRetrievalService.GetAccountSymmetricEncryptionProviderAsync()` etc.
- Code example: encrypt/decrypt with account key

---

### `security/user-context.md` — User Context

**Sections:**
- UserContext record: User, CurrentAccount, DeviceId, AvailableAccounts
- IUserContextProvider: read context
- IUserContextSetter: write context (host-only)
- Host-agnostic design: no HttpContext dependency
- Flow: host authenticates → sets context → IAM services use context for authorization
- Code example: setting context in middleware

---

### `architecture.md` — Architecture

**Sections:**
- Layered architecture diagram: Services → Processors → Repos → DbContext → DB
- Decorator pattern (Authorization + Telemetry via Scrutor)
- Result pattern: typed results, no exceptions for business logic
- FluentValidation integration
- SQL Server constraint: no cascade deletes on M:M
- Time abstraction: TimeProvider injection
- Multi-target: net9.0 + net10.0

---

### `result-codes.md` — Result Code Reference

**Complete table of all result code enums across every domain:**
- CreateAccountResultCode, DeleteAccountResultCode, AddUserToAccountResultCode, ...
- CreateUserResultCode, DeleteUserResultCode, AssignRolesToUserResultCode, ...
- (All enums listed with their values and when each code is returned)

---

## Implementation Order

1. **Phase 1**: `index.md` + `step-by-step-setup.md` + `iam-options.md` (entry points)
2. **Phase 2**: `authentication.md` + `authorization.md` + `resource-types.md` (core concepts)
3. **Phase 3**: `services/` (all 6 files — service layer reference)
4. **Phase 4**: `domains/` (all 8 files — domain reference)
5. **Phase 5**: `security/` (all 3 files)
6. **Phase 6**: `architecture.md` + `result-codes.md` (reference material)

---

## Notes

- **Don't duplicate CLAUDE.md** — docs are user-facing for host app developers; CLAUDE.md is for AI/developer context
- **Cross-reference Corely.DataAccess** for IEFConfiguration, IRepo, IUnitOfWorkProvider
- **Cross-reference Corely.Security** for encryption/hashing/signature primitives
- **Cross-reference Corely.Common** for FilterBuilder, OrderBuilder, PagedResult
- **Demo references** should point to `Corely.IAM.ConsoleTest/Program.cs` and `Corely.IAM.DevTools`
