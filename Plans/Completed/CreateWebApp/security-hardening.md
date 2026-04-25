# Security Hardening: 7 Fixes

## Context

Security audit identified actionable items: password fields logged at TRACE level, unused `Disabled` flag creating confusion, no lockout cooldown, missing cookie expiration, CSP middleware in wrong layer, long device ID cookie TTL, and missing documentation about the authorization layer split.

---

## Phase 1 — Documentation & CLAUDE.md

### Fix 1: Clarify authorization layer split in CLAUDE.md

**Problem:** Security audit misidentified "unguarded" service methods as critical gaps because CLAUDE.md doesn't document the two-layer authorization model.

**File:** `CLAUDE.md` — update the **Layered Architecture** section.

**Add after the existing decorator description:**

> Authorization is split into two layers:
> - **Service decorators** — validate context only (`HasUserContext()` / `HasAccountContext()`). They do NOT check CRUDX permissions.
> - **Processor decorators** — enforce specific CRUDX permission checks on resources via `AuthorizationProvider.IsAuthorizedAsync()`.
>
> Service methods that appear "unguarded" (e.g., `RegisterUsersWithGroupAsync`) are protected at the processor level where the actual work happens.

Also update the **Security Model** section to add:

> Multi-tenant user model: users exist independently of accounts (M:M relationship). There is no concept of "user A administrates user B" — account owners can register/deregister users with account entities but cannot read or modify other users directly.

---

## Phase 2 — Remove password logging (core library)

### Fix 2: Stop logging password-bearing requests in telemetry

**Problem:** `ExecuteWithLoggingAsync` at TRACE level serializes entire request objects including plaintext `Password` fields. Library callers shouldn't need to know to install a log sanitizer.

**Approach:** Switch password-bearing telemetry calls from the `(className, request, operation)` overload to the `(className, operation)` overload (no request parameter). Non-password methods stay as-is.

| File | Method | Has password? | Change |
|------|--------|--------------|--------|
| `BasicAuths/Processors/BasicAuthProcessorTelemetryDecorator.cs` | `CreateBasicAuthAsync` | Yes | Use no-request overload |
| same | `UpdateBasicAuthAsync` | Yes | Use no-request overload |
| same | `VerifyBasicAuthAsync` | Yes | Use no-request overload |
| `Services/AuthenticationServiceTelemetryDecorator.cs` | `SignInAsync` | Yes (`SignInRequest.Password`) | Use no-request overload |
| same | `SwitchAccountAsync` | No | Keep as-is |
| same | `SignOutAsync` | No | Keep as-is |

**No changes to `LoggerExtensions.cs`** — the no-request overload already exists.

---

## Phase 3 — Remove Disabled flag + Add LockedUtc

### Fix 3a: Remove unused `User.Disabled` property

**Problem:** The `Disabled` flag was added early but never used. Users exist independently of accounts, so only the user themselves could "disable" their account — which is really a soft-delete concept that doesn't exist elsewhere. Leaving it creates confusion.

**Code changes (remove property + all references):**

| File | Change |
|------|--------|
| `Users/Entities/UserEntity.cs:14` | Remove `public bool Disabled { get; set; }` |
| `Users/Models/User.cs:11` | Remove `public bool Disabled { get; set; }` |
| `Users/Entities/UserEntityConfiguration.cs:25` | Remove `.Property(e => e.Disabled).IsRequired()` |
| `Users/Mappers/UserMapper.cs:21,41` | Remove `Disabled = ...` from both ToModel and ToEntity |
| `Users/Processors/UserProcessor.cs:457` | Remove `Disabled = e.Disabled,` from projection |
| `UnitTests/Users/Mappers/UserMapperTests.cs` | Remove 5 assertions referencing `Disabled` |
| `Feature-Ideas.md` | Remove "Disabled Flags" section |
| `Plans/modification-service.md` | Remove "Out of scope: User.Disabled" note |

### Fix 3b: Add `LockedUtc` field to UserEntity

**Purpose:** Explicit lockout timestamp. Set automatically when failed attempts reach max. Also serves as infrastructure for future admin-initiated hard locks.

| File | Change |
|------|--------|
| `Users/Entities/UserEntity.cs` | Add `public DateTime? LockedUtc { get; set; }` |
| `Users/Models/User.cs` | Add `public DateTime? LockedUtc { get; set; }` |
| `Users/Entities/UserEntityConfiguration.cs` | Add `.Property(e => e.LockedUtc).IsRequired(false)` |
| `Users/Mappers/UserMapper.cs` | Add `LockedUtc = ...` to both ToModel and ToEntity |
| `Users/Processors/UserProcessor.cs` | Add `LockedUtc = e.LockedUtc,` to projection |

**Database migration (combined):**

Run `.\AddMigration.ps1 "RemoveDisabledAddLockedUtc"` — single migration drops `Disabled` column and adds `LockedUtc` column across all 3 providers.

---

## Phase 4 — Lockout cooldown

### Fix 4: Add time-based lockout cooldown using LockedUtc

**Problem:** Account lockout after `MaxLoginAttempts` failures is permanent until manual reset — DoS vector against legitimate users.

**Changes:**

| File | Change |
|------|--------|
| `Security/Models/SecurityOptions.cs` | Add `public int LockoutCooldownSeconds { get; set; } = 900;` (15 min default) |
| `Services/AuthenticationService.cs:61-65` | Rewrite lockout check to use `LockedUtc` + cooldown |
| `Services/AuthenticationService.cs:72-77` | On reaching max failures, set `LockedUtc = now` |

**Updated flow in `SignInAsync`:**
```
1. Check lockout:
   if (lockedUtc != null)
       if (lockedUtc + cooldownSeconds > now)
           → return UserLockedError (still in cooldown)
       else
           → lockedUtc = null, failedLoginsSinceLastSuccess = 0 (cooldown expired, auto-unlock)

2. Verify password:
   if (password wrong)
       → failedLoginsSinceLastSuccess++
       → if (failedLoginsSinceLastSuccess >= maxLoginAttempts)
             lockedUtc = now  (lock the user)
       → return PasswordMismatchError

3. Success:
   → failedLoginsSinceLastSuccess = 0, continue
```

Uses existing `_timeProvider` for testability. `LockedUtc` added in Phase 3b.

**Tests:** Add to `AuthenticationServiceTests`:
- `SignInAsync_LockedUser_WithinCooldown_ReturnsUserLockedError`
- `SignInAsync_LockedUser_AfterCooldownExpires_AllowsRetry`
- `SignInAsync_LockedUser_AfterCooldownExpires_FailedAttempt_IncrementsFromZero`

---

## Phase 5 — Cookie & middleware fixes

### Fix 5: Set auth cookie expiration to match token TTL

**Problem:** Auth token cookies have no `Expires` — they're session cookies that disappear when the browser closes, but the token is valid for 1 hour server-side.

**Changes:**

| File | Change |
|------|--------|
| `Web/Security/IAuthCookieManager.cs` | Add `int authTokenTtlSeconds` parameter to `SetAuthCookies` |
| `Web/Security/AuthCookieManager.cs` | Add `Expires = DateTimeOffset.UtcNow.AddSeconds(authTokenTtlSeconds)` to cookie options |
| All callers of `SetAuthCookies` | Pass TTL from `IOptions<SecurityOptions>` |

Callers to update (grep found these):
- `SignIn.cshtml.cs`, `Register.cshtml.cs`, `SelectAccount.cshtml.cs`, `SwitchAccount.cshtml.cs`
- Related test files

### Fix 6: Move CSP to WebApp

**Problem:** `SecurityHeadersMiddleware` in the reusable `Web` layer sets CSP rules that might conflict with a host app's existing CSP policy.

**Changes:**

| File | Change |
|------|--------|
| `Web/Middleware/SecurityHeadersMiddleware.cs:17-24` | Remove the `Content-Security-Policy` header line |
| `WebApp/Program.cs` | Add inline CSP middleware before `app.UseIAMWebAuthentication()` |

CSP moves to WebApp as:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] = "...same CSP string...";
    await next();
});
```

This makes CSP a **recommended default** the host app controls, not something the library forces.

### Fix 7: Tighten device ID cookie expiration

**Problem:** Device ID cookie persists for 1 year. Shorten to 90 days.

| File | Change |
|------|--------|
| `Web/Security/AuthCookieManager.cs:57` | Change `.AddYears(1)` → `.AddDays(90)` |

---

## Verification

Run `.\RebuildAndTest.ps1` after each phase. After Phase 3, also run `.\AddMigration.ps1 "RemoveUserDisabled"`.

Expected: **all tests pass, 0 failures** (test count may decrease slightly after removing Disabled assertions).
