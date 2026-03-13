## Account Invitations

### Context

The library supports adding users to accounts via `AddUserToAccountAsync`, but this requires knowing the target user's GUID — which is undiscoverable due to the multi-tenant privacy model. There's no practical path for an account owner to invite someone to join their account.

This plan adds a **DB-persisted invitation token** system. The library handles token generation, validation, expiry, and acceptance. It does **not** handle delivery (email, link sharing, etc.) — that's the host app's responsibility.

**Key requirements:**
- Account owners can create invitations (receive a token back)
- Invitations have configurable expiry
- Invitations are NOT tied to existing users (new users can be invited)
- Any authenticated user can accept an invitation to add themselves to the account
- Invitations are revocable, auditable, and single-use (Phase 1)

---

### Design Decisions

### 1. Token Format: Crypto-Random String (Not JWT)

**Decision:** Use a URL-safe, crypto-random string (Base64URL-encoded, 32 bytes = 256 bits).

**Why not JWT:**
- JWTs are stateless — can't revoke individual invitations without a DB lookup anyway
- JWTs are long and URL-unfriendly (300+ chars vs ~43 chars)
- We need DB state regardless (audit trail, single-use enforcement, listing active invitations)
- A random token with a DB lookup is simpler and covers all requirements

**Token generation:**
```csharp
var bytes = RandomNumberGenerator.GetBytes(32);
var token = Base64UrlEncoder.Encode(bytes);
```

### 2. Entity Design

**`InvitationEntity`** — new table, FK to Account (cascade delete with account):

| Column | Type | Notes |
|--------|------|-------|
| `Id` | `Guid` | PK, `Guid.CreateVersion7()` |
| `AccountId` | `Guid` | FK → Account, cascade delete |
| `Token` | `string` | Unique, indexed, crypto-random |
| `CreatedByUserId` | `Guid` | Who created the invitation |
| `Email` | `string` | Required — identifies the recipient; used for duplicate burn on acceptance |
| `Description` | `string?` | Optional — freeform label for display/tracking (e.g. name, team, notes) |
| `ExpiresUtc` | `DateTime` | When the invitation expires |
| `AcceptedByUserId` | `Guid?` | Null until accepted |
| `AcceptedUtc` | `DateTime?` | Null until accepted |
| `RevokedUtc` | `DateTime?` | Null unless revoked |
| `CreatedUtc` | `DateTime` | Standard audit field |
| `ModifiedUtc` | `DateTime?` | Standard audit field |

**Design notes:**
- `Email` is required — identifies the intended recipient. On acceptance, the processor burns all other pending invitations for the same `AccountId + Email` combo, preventing duplicate invitations (e.g. resends) from remaining live.
- `Description` is freeform and informational only — the host app can use it for a name, a team label, notes, or anything else. The system doesn't interpret or enforce it.
- `CreatedByUserId` is stored but NOT a FK to User — the invitation record survives if the inviting user is later removed from the account (for audit purposes). However, all **pending** invitations created by that user are **bulk-revoked** at removal time (see Design Decision #13).
- Cascade delete on `AccountId` — if the account is deleted, all its invitations are cleaned up automatically.
- Unique index on `Token` for fast lookup during acceptance.

### 3. Domain Structure

Follow existing domain folder layout:

```
Invitations/
├── Constants/
│   └── InvitationConstants.cs
├── Entities/
│   ├── InvitationEntity.cs
│   └── InvitationEntityConfiguration.cs
├── Models/
│   ├── Invitation.cs
│   ├── CreateInvitationRequest.cs
│   ├── CreateInvitationResult.cs
│   ├── AcceptInvitationRequest.cs
│   ├── AcceptInvitationResult.cs
│   ├── RevokeInvitationResult.cs
│   └── ListInvitationsResult.cs       (or use generic ListResult<Invitation>)
├── Processors/
│   ├── IInvitationProcessor.cs
│   ├── InvitationProcessor.cs
│   ├── InvitationProcessorAuthorizationDecorator.cs
│   └── InvitationProcessorTelemetryDecorator.cs
├── Mappers/
│   └── InvitationMapper.cs
└── Validators/
    └── CreateInvitationRequestValidator.cs
```

### 4. Processor Interface

```csharp
internal interface IInvitationProcessor
{
    Task<CreateInvitationResult> CreateInvitationAsync(CreateInvitationRequest request);
    Task<AcceptInvitationResult> AcceptInvitationAsync(AcceptInvitationRequest request);
    Task<RevokeInvitationResult> RevokeInvitationAsync(Guid invitationId);
    Task<ListResult<Invitation>> ListInvitationsAsync(Guid accountId, int skip, int take);
}
```

### 5. Authorization Model

| Method | Authorization Check | Rationale |
|--------|---------------------|-----------|
| `CreateInvitationAsync` | `AuthAction.Update` on `ACCOUNT_RESOURCE_TYPE` for `request.AccountId` | Same as `AddUserToAccountAsync` — inviting is an account update |
| `AcceptInvitationAsync` | Authenticated user only (`HasUserContext()`) | Any authenticated user can accept; no account context required |
| `RevokeInvitationAsync` | `AuthAction.Update` on `ACCOUNT_RESOURCE_TYPE` for the invitation's account | Only account managers can revoke |
| `ListInvitationsAsync` | `AuthAction.Read` on `ACCOUNT_RESOURCE_TYPE` for `accountId` | Viewing invitations is an account read |

**Special case — `AcceptInvitationAsync`:** This is the one method that does NOT require account context. The user may not be in any account yet (new user accepting first invitation). The authorization decorator should only check `HasUserContext()`, not `HasAccountContext()`.

### 6. Acceptance Flow

```
1. User presents token via AcceptInvitationRequest(token)
2. Processor looks up invitation by token
3. Validate:
   - Invitation exists → InvitationNotFoundError
   - Not expired (ExpiresUtc > now) → InvitationExpiredError
   - Not revoked (RevokedUtc == null) → InvitationRevokedError
   - Not already accepted (AcceptedByUserId == null) → InvitationAlreadyAcceptedError
4. Mark invitation as accepted (AcceptedByUserId, AcceptedUtc)
5. Bulk-burn all other pending invitations for same AccountId + Email
6. Check if user is already in the account → skip add, return Success (idempotent; tokens are burned)
7. Add user to account directly via repo (bypasses AccountProcessor auth)
8. Return success with AccountId
```

**Important:** The acceptance logic should add the user to the account directly via the repository, NOT through `AccountProcessor.AddUserToAccountAsync`. The latter requires `AuthAction.Update` on the account — which the accepting user doesn't have yet (they're not in the account). This is a controlled bypass: the invitation token itself is the authorization.

**Duplicate burn:** Accepting an invitation also marks all other pending invitations for the same `AccountId + Email` as accepted (step 5). This handles the "resend" scenario — if two tokens were issued for john@example.com, accepting either one burns both.

### 7. Request/Result Models

```csharp
public record CreateInvitationRequest(Guid AccountId, string Email, string? Description, int ExpiresInSeconds);

public record CreateInvitationResult(
    CreateInvitationResultCode ResultCode,
    string Message,
    string? Token,          // The generated token (only on success)
    Guid? InvitationId      // The invitation ID (only on success)
);

public enum CreateInvitationResultCode
{
    Success,
    AccountNotFoundError,
    ValidationError,
    UnauthorizedError
}

public record AcceptInvitationRequest(string Token);

public record AcceptInvitationResult(
    AcceptInvitationResultCode ResultCode,
    string Message,
    Guid? AccountId         // The account joined (only on success)
);

public enum AcceptInvitationResultCode
{
    Success,
    InvitationNotFoundError,
    InvitationExpiredError,
    InvitationRevokedError,
    InvitationAlreadyAcceptedError,
    UnauthorizedError
}

public record RevokeInvitationResult(
    RevokeInvitationResultCode ResultCode,
    string Message
);

public enum RevokeInvitationResultCode
{
    Success,
    InvitationNotFoundError,
    InvitationAlreadyAcceptedError,
    UnauthorizedError
}
```

### 8. Domain Model

```csharp
public class Invitation
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public Guid CreatedByUserId { get; init; }
    public string Email { get; init; }
    public string? Description { get; init; }
    public DateTime ExpiresUtc { get; init; }
    public Guid? AcceptedByUserId { get; init; }
    public DateTime? AcceptedUtc { get; init; }
    public DateTime? RevokedUtc { get; init; }
    public DateTime CreatedUtc { get; init; }
    public bool IsExpired => ExpiresUtc < DateTime.UtcNow;
    public bool IsRevoked => RevokedUtc != null;
    public bool IsAccepted => AcceptedByUserId != null;
    public bool IsPending => !IsExpired && !IsRevoked && !IsAccepted;
}
```

**Note:** The `Token` value is intentionally NOT exposed on the domain model. It's returned only in `CreateInvitationResult` at creation time. Listing invitations shows metadata (who, when, status) but never re-exposes the token.

### 9. Constants

```csharp
public static class InvitationConstants
{
    public const int TOKEN_LENGTH = 32;             // bytes, before Base64URL encoding
    public const int EMAIL_MAX_LENGTH = 254;        // RFC 5321
    public const int DESCRIPTION_MAX_LENGTH = 200;
    public const int MIN_EXPIRY_SECONDS = 300;      // 5 minutes
    public const int MAX_EXPIRY_SECONDS = 2_592_000; // 30 days
    public const int DEFAULT_EXPIRY_SECONDS = 604_800; // 7 days
}
```

### 10. Validation

**`CreateInvitationRequestValidator`:**
- `AccountId` — not empty
- `Email` — not empty, valid email format, max length
- `Description` — if provided, max length
- `ExpiresInSeconds` — between `MIN_EXPIRY_SECONDS` and `MAX_EXPIRY_SECONDS`

### 11. Service Wiring

Where the invitation processor is exposed depends on context:

- **Option A:** Add methods to existing `IRegistrationService` — invitations are part of registration flow
- **Option B:** Create a new `IInvitationService` — cleaner separation

**Decision: Option A** — `IRegistrationService` already handles `RegisterUsersWithAccountAsync` (the creation flow). Invitations are the counterpart for adding external users. Adding `CreateInvitationAsync`, `AcceptInvitationAsync`, `RevokeInvitationAsync`, and `ListInvitationsAsync` to `IRegistrationService` keeps the public API surface cohesive.

If the service grows too large, it can be split later.

### 12. PermissionConstants

Add a new resource type constant:

```csharp
public const string INVITATION_RESOURCE_TYPE = "invitation";
```

**However**, invitation authorization piggybacks on `ACCOUNT_RESOURCE_TYPE` (Update on account = can create/revoke invitations). The `INVITATION_RESOURCE_TYPE` constant is only needed if we want fine-grained invitation-specific permissions in the future. **Defer adding it** — use `ACCOUNT_RESOURCE_TYPE` for now.

### 13. Revoke Invitations on User Removal

**Problem:** If a user is removed from an account (intentionally or because they had unauthorized access), any pending invitations they created should not remain active. A removed user should not be able to affect the account indirectly through outstanding invitations.

**Solution:** When `AccountProcessor.RemoveUserFromAccountAsync` executes successfully, bulk-revoke all pending invitations created by the removed user for that account:

```csharp
// After removing user from account, revoke their pending invitations
var pendingInvitations = await _invitationRepo.QueryAsync(q =>
    q.Where(i =>
        i.AccountId == request.AccountId
        && i.CreatedByUserId == request.UserId
        && i.AcceptedByUserId == null
        && i.RevokedUtc == null));

var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
foreach (var invitation in pendingInvitations)
{
    invitation.RevokedUtc = utcNow;
}
```

**Why revoke instead of validate-on-accept:**
- Explicit and auditable — revocation is a recorded event
- Doesn't leave live tokens floating around
- Validate-on-accept would silently fail for the recipient, which is a confusing UX

**Dependency:** `AccountProcessor` will need `IRepo<InvitationEntity>` injected. This is acceptable — the account processor already manages the user-account relationship, and invitation cleanup is a direct consequence of that operation.

---

### Scope

**In scope:**
- [x] Entity, configuration, migration
- [x] Processor with authorization + telemetry decorators
- [x] Create, Accept, Revoke, List operations
- [x] Single-use invitations
- [x] Required email with duplicate burn
- [x] Configurable expiry
- [x] Validation
- [x] Unit tests
- [x] Service wiring via `IRegistrationService`
- [x] DevTools CLI commands
- [x] ConsoleTest example
- [x] Web UI — invitation management on Account detail page
- [x] Web UI — add user by email on Account detail page
- [x] Web UI — self-service accept invitation page

**Out of scope (future):**
- Multi-use invitations (reusable invite links with use count / unlimited)
- Invitation delivery (email, notifications)
- Role/group pre-assignment (invite + auto-assign to specific roles/groups on acceptance)

---

### Implementation Phases

### Phase 1: Entity & Migration

**New files:**
- `Corely.IAM/Invitations/Entities/InvitationEntity.cs`
- `Corely.IAM/Invitations/Entities/InvitationEntityConfiguration.cs`
- `Corely.IAM/Invitations/Constants/InvitationConstants.cs`

**Modified files:**
- `Corely.IAM/DataAccess/IamDbContext.cs` — add `DbSet<InvitationEntity>`

**Steps:**
1. Create entity class with all fields
2. Create entity configuration:
   - PK on `Id`, `ValueGeneratedNever()`
   - FK to `AccountEntity` with cascade delete
   - Unique index on `Token`
   - Index on `AccountId` (for listing)
   - Max lengths from constants
3. Add `DbSet<InvitationEntity>` to `IamDbContext`
4. Run `.\AddMigration.ps1 "AddInvitations"` to generate migrations for all 3 providers
5. Verify migrations compile

### Phase 2: Models, Mapper, Validator

**New files:**
- `Corely.IAM/Invitations/Models/Invitation.cs`
- `Corely.IAM/Invitations/Models/CreateInvitationRequest.cs`
- `Corely.IAM/Invitations/Models/CreateInvitationResult.cs`
- `Corely.IAM/Invitations/Models/CreateInvitationResultCode.cs`
- `Corely.IAM/Invitations/Models/AcceptInvitationRequest.cs`
- `Corely.IAM/Invitations/Models/AcceptInvitationResult.cs`
- `Corely.IAM/Invitations/Models/AcceptInvitationResultCode.cs`
- `Corely.IAM/Invitations/Models/RevokeInvitationResult.cs`
- `Corely.IAM/Invitations/Models/RevokeInvitationResultCode.cs`
- `Corely.IAM/Invitations/Mappers/InvitationMapper.cs`
- `Corely.IAM/Invitations/Validators/CreateInvitationRequestValidator.cs`

### Phase 3: Processor & Decorators

**New files:**
- `Corely.IAM/Invitations/Processors/IInvitationProcessor.cs`
- `Corely.IAM/Invitations/Processors/InvitationProcessor.cs`
- `Corely.IAM/Invitations/Processors/InvitationProcessorAuthorizationDecorator.cs`
- `Corely.IAM/Invitations/Processors/InvitationProcessorTelemetryDecorator.cs`

**Key implementation details:**

- `InvitationProcessor` depends on:
  - `IRepo<InvitationEntity>` — invitation CRUD
  - `IRepo<AccountEntity>` — verify account exists, add user to account
  - `IReadonlyRepo<UserEntity>` — verify user exists during acceptance (optional)
  - `IUserContextProvider` — get accepting user's ID
  - `IValidationProvider` — validate requests
  - `TimeProvider` — for expiry calculations and acceptance timestamps

- **Token generation** in `CreateInvitationAsync`:
  ```csharp
  var bytes = RandomNumberGenerator.GetBytes(InvitationConstants.TOKEN_LENGTH);
  var token = Base64UrlEncoder.Encode(bytes);
  ```

- **Acceptance** adds user to account directly via repo (bypasses `AccountProcessor` auth):
  ```csharp
  var accountEntity = await _accountRepo.GetAsync(
      a => a.Id == invitation.AccountId,
      include: q => q.Include(a => a.Users));
  accountEntity.Users!.Add(userEntity);
  await _accountRepo.UpdateAsync(accountEntity);
  ```

**Modified files:**
- `Corely.IAM/ServiceRegistrationExtensions.cs` — register processor + decorators
- `Corely.IAM/Accounts/Processors/AccountProcessor.cs` — inject `IRepo<InvitationEntity>`, add bulk-revoke logic to `RemoveUserFromAccountAsync`

### Phase 4: Service Wiring

**Modified files:**
- `Corely.IAM/Services/IRegistrationService.cs` — add invitation methods to interface
- `Corely.IAM/Services/RegistrationService.cs` — implement by delegating to `IInvitationProcessor`
- `Corely.IAM/Services/RegistrationServiceAuthorizationDecorator.cs` — context guards
- `Corely.IAM/Services/RegistrationServiceTelemetryDecorator.cs` — telemetry wrappers

**Service-level auth:**
- `CreateInvitationAsync` — `HasAccountContext()` (processor handles CRUDX)
- `AcceptInvitationAsync` — `HasUserContext()` only (no account context required)
- `RevokeInvitationAsync` — `HasAccountContext()` (processor handles CRUDX)
- `ListInvitationsAsync` — `HasAccountContext()` (processor handles CRUDX)

### Phase 5: Unit Tests

**New files:**
- `Corely.IAM.UnitTests/Invitations/Processors/InvitationProcessorTests.cs`
- `Corely.IAM.UnitTests/Invitations/Validators/CreateInvitationRequestValidatorTests.cs`

**Test coverage:**

**InvitationProcessor — CreateInvitationAsync:**
- Happy path: returns token and invitation ID
- Account not found → `AccountNotFoundError`
- Validation failure → `ValidationError`

**InvitationProcessor — AcceptInvitationAsync:**
- Happy path: user added to account, invitation marked accepted
- User already in account → token(s) burned, returns `Success` (idempotent)
- Also burns sibling invitations for same account+email
- Token not found → `InvitationNotFoundError`
- Expired → `InvitationExpiredError`
- Revoked → `InvitationRevokedError`
- Already accepted → `InvitationAlreadyAcceptedError`

**InvitationProcessor — RevokeInvitationAsync:**
- Happy path: invitation marked revoked
- Not found → `InvitationNotFoundError`
- Already accepted → `InvitationAlreadyAcceptedError`

**AccountProcessor — RemoveUserFromAccountAsync (updated):**
- Removing a user bulk-revokes their pending invitations for that account
- Already-accepted and already-revoked invitations are unaffected

**InvitationProcessor — ListInvitationsAsync:**
- Returns invitations for account
- Respects skip/take pagination

**CreateInvitationRequestValidator:**
- Valid request passes
- Empty AccountId fails
- ExpiresInSeconds below minimum fails
- ExpiresInSeconds above maximum fails
- Empty email fails
- Email invalid format fails
- Email over max length fails
- Description over max length fails

### Phase 6: Account Cascade Cleanup

**Modified files:**
- `Corely.IAM/Accounts/Processors/AccountProcessor.cs` — add `.Include(a => a.Invitations)` and `.Clear()` in `DeleteAccountAsync` if NOT using cascade delete

**Note:** Since invitations use `DeleteBehavior.Cascade` (1:M owned by account, not M:M), EF Core will handle cleanup automatically. No manual clearing needed — unlike M:M join entities. **This phase may be a no-op** but should be verified during implementation.

### Phase 7: DevTools CLI Commands

Add an `invitation` parent command with subcommands for creating and accepting invitations via the CLI.

**New files:**
- `Corely.IAM.DevTools/Commands/Invitation/Invitation.cs` — parent command
- `Corely.IAM.DevTools/Commands/Invitation/Invitation.Create.cs` — create subcommand
- `Corely.IAM.DevTools/Commands/Invitation/Invitation.Accept.cs` — accept subcommand
- `Corely.IAM.DevTools/Commands/Invitation/Invitation.Revoke.cs` — revoke subcommand
- `Corely.IAM.DevTools/Commands/Invitation/Invitation.List.cs` — list subcommand

**Follows existing pattern:** Nested classes inheriting `CommandBase`, auto-discovered by reflection.

**Commands:**

| Command | Arguments / Options | Output |
|---------|---------------------|--------|
| `invitation create` | `--account-id <guid>` `--email <email>` `--description <text>?` `--expires <seconds>?` | Token string + invitation ID |
| `invitation accept` | `--token <token>` | Account ID joined |
| `invitation revoke` | `--invitation-id <guid>` | Success/failure |
| `invitation list` | `--account-id <guid>` `--skip <n>?` `--take <n>?` | Table of invitations with status |

**Note:** DevTools commands require authentication context. Follow the pattern used by existing Registration/Retrieval commands (sign in first via `authentication sign-in`).

### Phase 8: ConsoleTest Example

Add invitation workflow to the existing ConsoleTest `Program.cs` sequential flow.

**Modified files:**
- `Corely.IAM.ConsoleTest/Program.cs`

**Flow to add** (after account creation and authentication):
```
1. Create invitation for the current account (email: "test@example.com")
2. Log the returned token
3. Accept the invitation (demonstrates idempotent accept — user is already in account)
4. List invitations for the account (shows accepted status)
5. Create a second invitation
6. Revoke the second invitation
7. List invitations again (shows one accepted, one revoked)
```

This demonstrates the full lifecycle: create → accept → list → revoke.

### Phase 9: Web UI — Account Detail (Admin Flow)

Add invitation management to the Account detail page. The existing "Add User by GUID" flow stays in the Users section. A new Invitations section is added above it for creating and managing invitations.

**Modified files:**
- `Corely.IAM.Web/Components/Pages/Accounts/AccountDetail.razor`

**Layout** (section order on Account detail page):
1. **Details** (existing — edit name, delete account)
2. **Invitations** (new — create and manage invitations)
3. **Users** (existing — add user by GUID or email, user list with pagination)
4. **Your Access** (existing — effective permissions panel)

#### Invitations Section

```html
<div class="section-divider mb-4">
    <div class="section-divider-label">
        Invitations
        <PermissionView Action="AuthAction.Update" Resource="@PermissionConstants.ACCOUNT_RESOURCE_TYPE" ResourceIds="new[] { Id }">
            <button class="btn btn-primary btn-sm" @onclick="() => _createInviteModal.ShowAsync()">
                Create Invitation
            </button>
        </PermissionView>
    </div>
    <!-- Table: Email | Description | Status | Created | Expires | Actions (Revoke) -->
    <table class="table table-sm table-hover mb-0">...</table>
    <Pagination ... />
</div>
```

**Create Invitation modal** (`FormModal`):
- Email field (required)
- Description field (optional)
- Expiry dropdown or input (default 7 days)
- On success: display the generated token in a copyable readonly field (this is the only time the token is shown)

**Status display:** Each invitation row shows:
- `Pending` (green badge) — not expired, not revoked, not accepted
- `Accepted` (blue badge) — accepted by user
- `Expired` (grey badge) — past expiry date
- `Revoked` (red badge) — manually revoked

**Revoke action:** Button per pending invitation, wrapped in `PermissionView` for Update on account.

#### Users Section Update

Enhance the existing Users section to support adding users by **email** in addition to GUID. Emails are globally unique (`UserEntityConfiguration` line 36: `HasIndex(e => e.Email).IsUnique()`), so an email lookup reliably identifies a single user.

**Current flow:** Single GUID input field + "Add User" button.

**Updated flow:** Input field that accepts either a GUID or an email + "Add User" button. The backend determines which it is:
- If the input is a valid GUID → look up user by ID (existing behavior)
- If the input contains `@` → look up user by email (new behavior)
- Otherwise → validation error

This requires a new processor/service method (or extending the existing one) to resolve a user by email. Options:
- **Option A:** Add `GetUserByEmailAsync(string email)` to `IUserProcessor` — returns the user entity so we can get the ID, then call existing `RegisterUserWithAccountAsync`
- **Option B:** Add an overload `RegisterUserWithAccountByEmailAsync(string email, Guid accountId)` that handles the lookup + registration in one call

**Decision: Option A** — keep it composable. The page calls `GetUserByEmailAsync` to resolve the ID, then calls the existing `RegisterUserWithAccountAsync`. This avoids duplicating registration logic and the new method is useful independently.

**Modified files:**
- `Corely.IAM.Web/Components/Pages/Accounts/AccountDetail.razor` — update input handling
- `Corely.IAM/Users/Processors/IUserProcessor.cs` — add `GetUserByEmailAsync`
- `Corely.IAM/Users/Processors/UserProcessor.cs` — implement email lookup
- `Corely.IAM/Users/Processors/UserProcessorAuthorizationDecorator.cs` — auth check (Read on user)
- `Corely.IAM/Users/Processors/UserProcessorTelemetryDecorator.cs` — telemetry wrapper
- `Corely.IAM/Services/IRetrievalService.cs` — expose `GetUserByEmailAsync`
- `Corely.IAM/Services/RetrievalService.cs` — delegate to processor
- `Corely.IAM/Services/RetrievalServiceAuthorizationDecorator.cs` — context guard
- `Corely.IAM/Services/RetrievalServiceTelemetryDecorator.cs` — telemetry wrapper

### Phase 10: Web UI — Accept Invitation (Self-Service Flow)

Separate page where an authenticated user can accept an invitation token to join an account. This is the recipient-side flow — distinct from the admin flow on the Account detail page.

**New files:**
- `Corely.IAM.Web/Components/Pages/Invitations/AcceptInvitation.razor`

**Route:** `/accept-invitation` (optionally accepts `?token=<token>` query param to pre-fill)

**Layout:**
```html
<PageTitle>Accept Invitation - Corely IAM</PageTitle>

<div class="d-flex justify-content-center align-items-center mt-5">
    <div class="card border-0 shadow" style="max-width: 500px; width: 100%;">
        <div class="card-body p-4 text-center">
            <i class="bi bi-envelope-open fs-1 text-primary"></i>
            <h3 class="mt-3">Accept Invitation</h3>
            <p class="text-muted">Paste your invitation token below to join an account.</p>

            <Alert ... />

            <div class="input-group mt-3">
                <input type="text" class="form-control" @bind="_token" placeholder="Invitation token" />
                <button class="btn btn-primary" @onclick="AcceptAsync" disabled="@_loading">
                    Accept
                </button>
            </div>
        </div>
    </div>
</div>
```

**Behavior:**
- User must be authenticated (inherits `AuthenticatedPageBase`)
- Does NOT require account context — user may not be in any account yet
- On success: redirect to the account dashboard (`/switch-account?accountId=...&returnUrl=/`)
- On failure: show error message (expired, revoked, already accepted, etc.)
- If `?token=` query param is present, auto-fill the input (allows the host app to build shareable invite links like `https://app.com/accept-invitation?token=abc123`)

**Navigation:** Add an "Accept Invitation" link somewhere accessible to users without account context — e.g., the account selection page or the sidebar/nav when no account is selected.

---

### Verification Checklist

- [ ] `.\RebuildAndTest.ps1` passes
- [ ] Migrations generated for all 3 providers
- [ ] Token is URL-safe and not re-exposed after creation
- [ ] Acceptance works without account context
- [ ] Expired/revoked/accepted invitations are properly rejected
- [ ] Account deletion cascades to invitations
- [ ] No circular dependency between InvitationProcessor and AccountProcessor
- [ ] Removing a user from an account revokes their pending invitations
- [ ] DevTools commands work end-to-end (create, accept, revoke, list)
- [ ] ConsoleTest demonstrates full invitation lifecycle
- [ ] Web UI — Account detail shows invitation list with correct status badges
- [ ] Web UI — Create Invitation modal displays token on success
- [ ] Web UI — Accept Invitation page works with and without query param pre-fill
- [ ] Web UI — Accept Invitation redirects to account on success

---

## Follow-up: Remove Direct Add-User by GUID

### Context

The Account detail page (`AccountDetail.razor`) has a "Users" section header with a GUID input field + "Add User" button that calls `RegisterUserWithAccountAsync` directly. This mechanism was added before the invitations system existed as a placeholder admin shortcut.

Now that invitations are fully implemented, the direct-add-by-GUID flow is **unnecessary and an attack vector**: it lets any account owner enumerate whether arbitrary GUIDs correspond to real users, bypassing the privacy model. The invitations system is the correct and only way to bring a user into an account.

**Note:** The "Add user by email" variant described in Phase 9 of this plan was **never implemented** (the decision was correctly reversed — email lookup has the same privacy problem as GUID lookup). No backend changes are needed for that.

The Phase 9 checklist items "Add user by email resolves and registers correctly" and "Add user input handles both GUID and email formats" were from that discarded direction and are dropped.

### Scope

**Single file change — Web UI only:**

`Corely.IAM.Web/Components/Pages/Accounts/AccountDetail.razor`
- Remove the GUID `<input>` and "Add User" `<button>` from the Users section header (lines 162–165)
- Remove the `_newUserInput` field declaration
- Remove the `AddUserAsync()` method

**No backend changes.** `IRegistrationService.RegisterUserWithAccountAsync` is intentionally kept — it has valid uses in programmatic/admin/test flows (DevTools, ConsoleTest) and is part of the public library surface. Only the web UI entry point is removed.

### What Changes

| Location | Change |
|----------|--------|
| `AccountDetail.razor` markup | ~~Remove GUID input + Add User button from Users section header~~ ✅ |
| `AccountDetail.razor` @code | ~~Remove `private string? _newUserInput` field~~ ✅ |
| `AccountDetail.razor` @code | ~~Remove `AddUserAsync()` method~~ ✅ |

### After This Change

Users can only be added to an account via the invitation flow:
1. Account owner creates an invitation (email + optional description + expiry) → receives a token
2. Invited user navigates to `/accept-invitation`, pastes the token, and is added to the account

---

## Follow-up: Invitation Token Modal — Select All + Copy to Clipboard

### Context

The "Invitation Created" modal (`AccountDetail.razor` lines 220–243) displays the token in a readonly text input with only a Close button in the footer. The user must manually select and copy the token. The screenshot shows the token is already visually selected but there's no programmatic copy button.

### Changes

**`Corely.IAM.Web/Components/Pages/Accounts/AccountDetail.razor`**

1. Auto-select the token text when the modal renders — add `@ref="_tokenInput"` to the input and call `_tokenInput.FocusAsync()` + JS `select()` via `JSRuntime` after the modal appears, OR use the simpler `onclick="this.select()"` HTML attribute on the input.
2. Add a **"Copy to Clipboard"** button in the modal footer, left of the existing Close button. On click, call `JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _createdToken)`. Show brief visual feedback (button text changes to "Copied!" for ~1.5s).

**New field:**
```csharp
private bool _tokenCopied;
```

**Updated modal footer:**
```razor
<div class="modal-footer">
    <button type="button" class="btn btn-primary btn-sm" @onclick="CopyTokenAsync">
        @(_tokenCopied ? "Copied!" : "Copy to Clipboard")
    </button>
    <button type="button" class="btn btn-secondary btn-sm" @onclick="() => { _createdToken = null; _tokenCopied = false; }">Close</button>
</div>
```

**Updated input** (auto-selects on click):
```razor
<input type="text" class="form-control form-control-sm font-monospace" readonly value="@_createdToken" onclick="this.select()" />
```

**New method:**
```csharp
private async Task CopyTokenAsync()
{
    await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", _createdToken);
    _tokenCopied = true;
    StateHasChanged();
    await Task.Delay(1500);
    _tokenCopied = false;
    StateHasChanged();
}
```

**Requires:** inject `IJSRuntime JSRuntime` at the top of the component.

### Files

| File | Change |
|------|--------|
| `AccountDetail.razor` | Add `_tokenCopied` field, `CopyTokenAsync` method, update modal footer and token input |

---

## Follow-up: Prevent Creating Invitation for User Already in Account

### Context

`InvitationProcessor.CreateInvitationAsync` does not check whether the provided email already belongs to a user who is a member of the account. This means an owner can create (and send) an invitation for someone who is already in the account — confusing for both parties.

The check should happen at invitation **creation** time, not acceptance time.

### Backend Changes

**`Corely.IAM/Invitations/Models/CreateInvitationResultCode.cs`**

Add new result code:
```csharp
UserAlreadyInAccountError,
```

**`Corely.IAM/Invitations/Models/CreateInvitationResult.cs`**

Add `Guid? ExistingUserId` to the result (populated only on `UserAlreadyInAccountError`):
```csharp
public record CreateInvitationResult(
    CreateInvitationResultCode ResultCode,
    string Message,
    string? Token,
    Guid? InvitationId,
    Guid? ExistingUserId
);
```

**`Corely.IAM/Invitations/Processors/InvitationProcessor.cs`**

After the account existence check, add:
```csharp
// Check if a user with this email is already a member of the account
var existingUser = await _userRepo.GetAsync(u =>
    u.Email == request.Email
    && u.Accounts!.Any(a => a.Id == request.AccountId));

if (existingUser != null)
{
    _logger.LogWarning(
        "User {UserId} with email {Email} is already a member of account {AccountId}",
        existingUser.Id, request.Email, request.AccountId);
    return new CreateInvitationResult(
        CreateInvitationResultCode.UserAlreadyInAccountError,
        $"A user with email {request.Email} is already a member of this account.",
        null,
        null,
        existingUser.Id);
}
```

`InvitationProcessor` already has access to `IReadonlyRepo<UserEntity>` (or it needs to be injected — confirm during implementation).

All existing `CreateInvitationResult` call sites need the new `ExistingUserId` argument added (pass `null`).

**`Corely.IAM.UnitTests/Invitations/Processors/InvitationProcessorTests.cs`**

Add test: `CreateInvitation_ReturnsUserAlreadyInAccountError_WhenEmailBelongsToExistingMember`

### Web Changes

**`Corely.IAM.Web/Components/Pages/Accounts/AccountDetail.razor`**

In `CreateInvitationAsync`, handle the new result code:
```csharp
if (result.ResultCode == CreateInvitationResultCode.UserAlreadyInAccountError)
{
    SetResultMessage(false, string.Empty,
        $"That email belongs to a user already in this account (ID: {result.ExistingUserId}).");
    return;
}
```

Show the error in the existing `Alert` component inside the create invitation modal — no new UI needed.

### Files

| File | Change |
|------|--------|
| `CreateInvitationResultCode.cs` | Add `UserAlreadyInAccountError` |
| `CreateInvitationResult.cs` | Add `Guid? ExistingUserId` parameter |
| `InvitationProcessor.cs` | Add email-in-account check after account existence check; inject `IReadonlyRepo<UserEntity>` if not already present |
| All `CreateInvitationResult(...)` call sites | Add `null` for new `ExistingUserId` parameter |
| `InvitationProcessorTests.cs` | Add test for new error code |
| `AccountDetail.razor` | Handle `UserAlreadyInAccountError` in `CreateInvitationAsync` |

---

## Bugfix: Accepting User Email Not Validated Against Invitation Email

### Problem

`AcceptInvitationAsync` does not verify that the accepting user's email matches the invitation's `Email` field. Any authenticated user who obtains or guesses the token can accept it and add themselves to the account — even if the invitation was created for a completely different person.

**Reproduction:**
1. Account owner creates invitation for `alice@example.com`
2. Authenticated user `bob@example.com` navigates to `/accept-invitation`
3. Bob pastes the token and is added to the account

This defeats the purpose of the email field and allows unintended users to join accounts.

### Root Cause

`InvitationProcessor.AcceptInvitationAsync` (line 195) reads the current user's ID from `UserContext` and calls `AddUserToAccountForInvitationAsync` without ever comparing the user's email against `invitationEntity.Email`.

The original design (Decision #5, Acceptance Flow step 1–8) treated the token as the sole authorization — "the invitation token itself is the authorization." But this was an oversight: the `Email` field was added specifically to identify the intended recipient and enable duplicate-burn logic. It should also gate acceptance.

### Fix (Implemented)

**`Corely.IAM/Invitations/Models/AcceptInvitationResult.cs`**

Added `EmailMismatchError` to the `AcceptInvitationResultCode` enum.

**`Corely.IAM/Invitations/Processors/InvitationProcessor.cs`**

After the existing validation checks (expired/revoked/accepted) and before calling `AddUserToAccountForInvitationAsync`, added a case-insensitive email match check:

```csharp
var userContext = _userContextProvider.GetUserContext()!;
var userId = userContext.User.Id;

if (!string.Equals(userContext.User.Email, invitationEntity.Email, StringComparison.OrdinalIgnoreCase))
{
    _logger.LogWarning(
        "User {UserId} attempted to accept invitation {InvitationId} intended for {InvitedEmail}",
        userId, invitationEntity.Id, invitationEntity.Email);
    return new AcceptInvitationResult(
        AcceptInvitationResultCode.EmailMismatchError,
        "This invitation is for a different user",
        null);
}
```

**Key details:**
- Case-insensitive comparison (`OrdinalIgnoreCase`) since email addresses are case-insensitive per RFC 5321
- Log at `LogWarning` — this is a blocked operation that could indicate misuse
- Error message says "This invitation is for a different user" — does NOT leak the intended email (security: don't confirm who the invitation was for)

**`Corely.IAM.UnitTests/Invitations/Processors/InvitationProcessorTests.cs`**

- Added `AcceptInvitation_ReturnsEmailMismatchError_WhenUserEmailDoesNotMatch` — verifies `EmailMismatchError` returned and user NOT added to account
- Added `AcceptInvitation_Succeeds_WhenUserEmailMatchesCaseInsensitive` — invitation for `Alice@Example.COM`, user has `alice@example.com`, verifies success
- Fixed `SetUserContext` to use `userEntity.Email` instead of hardcoded `"test@test.com"` — existing accept tests were updated to create users with matching emails
- Restructured `AcceptInvitation_ReturnsSuccess_WhenUserAlreadyInAccount` to create the invitation entity directly (via helper) before adding the user to the account, since `CreateInvitationAsync` now blocks invitations for emails already in the account

**`Corely.IAM.Web/Components/Pages/Invitations/AcceptInvitation.razor`**

- Added `EmailMismatchError` case to the result code switch — displays "This invitation is for a different user."
- Removed `disabled` guard on the Accept button that required non-empty token — button is always enabled (except during loading), empty input is handled by the processor returning `InvitationNotFoundError`

### Files

| File | Change |
|------|--------|
| `AcceptInvitationResult.cs` | ✅ Added `EmailMismatchError` to enum |
| `InvitationProcessor.cs` | ✅ Added email comparison check before `AddUserToAccountForInvitationAsync` |
| `InvitationProcessorTests.cs` | ✅ Added 2 new tests, fixed `SetUserContext` to use entity email, restructured already-in-account test |
| `AcceptInvitation.razor` | ✅ Handle `EmailMismatchError` in UI; always-enabled Accept button |
