# Query Projection Optimizations

## Background

`InvitationProcessor.CreateInvitationAsync` was refactored to replace two sequential DB queries
(get account, get user) with a single `EvaluateAsync` projection that checks both in one SQL
round trip. This document lists other places in the processors where the same kind of change
would be easy to make.

The pattern to replace:

```csharp
// BEFORE: two round trips
var accountEntity = await _accountRepo.GetAsync(a => a.Id == accountId);
var roleExists = await _roleRepo.AnyAsync(r => r.AccountId == accountId && r.Name == name);
```

```csharp
// AFTER: one round trip
var check = await _accountRepo.EvaluateAsync(async (q, ct) =>
    await q.Where(a => a.Id == accountId)
        .Select(a => new
        {
            RoleNameExists = a.Roles!.Any(r => r.Name == name),
        })
        .FirstOrDefaultAsync(ct)
);
// null → account not found; check.RoleNameExists → duplicate
```

---

## Easy Wins

These are straightforward drop-in replacements — same logic, no structural changes needed.

### 1. `RoleProcessor.CreateRoleAsync` (~line 45)

**Current:** GetAsync(account) → AnyAsync(role name in account)  
**Change:** Single `EvaluateAsync` on `_accountRepo` projecting `{ RoleNameExists }`.  
`null` result = account not found; `RoleNameExists == true` = duplicate name.

### 2. `PermissionProcessor.CreatePermissionAsync` (~line 44)

**Current:** GetAsync(account) → AnyAsync(permission name in account)  
**Change:** Same pattern as above — single `EvaluateAsync` on `_accountRepo` projecting
`{ PermissionNameExists }`.

### 3. `GroupProcessor.AddUsersToGroupAsync` (~line 83)

**Current:** GetAsync(group) → ListAsync(users filtered by group membership and account)  
**Change:** `EvaluateAsync` on `_groupRepo` projecting group metadata (account ID, group ID)
plus the list of valid user IDs in one query. Then use those IDs for the update.

### 4. `GroupProcessor.AssignRolesToGroupAsync` (~line 245)

**Current:** GetAsync(group) → ListAsync(roles not yet in group and in same account)  
**Change:** Same projection pattern — one `EvaluateAsync` from `_groupRepo` to get group data
and available roles together.

### 5. `RoleProcessor.AssignPermissionsToRoleAsync` (~line 217)

**Current:** GetAsync(role) → ListAsync(permissions not yet on role and in same account)  
**Change:** `EvaluateAsync` from `_roleRepo` projecting role data + available permissions.

### 6. `UserProcessor.AssignRolesToUserAsync` (~line 183)

**Current:** GetAsync(user with accounts) → ListAsync(roles not yet on user and in user's accounts)  
**Change:** `EvaluateAsync` from `_userRepo` projecting user account IDs + available roles.

---

## Medium Complexity (skip unless we revisit)

These would work but require more thought — either they need full entity objects for mutation,
or they involve N+1 patterns inside loops that would need more structural changes.

- **`AccountProcessor.AddUserToAccountAsync`** — GetAsync(account w/ users) + GetAsync(user).
  We need the full user entity to add it to `account.Users`, so we can't eliminate the second
  query without restructuring the mutation step.

- **`AccountProcessor.RemoveUserFromAccountAsync`** — Similar; the second GetAsync(user)
  is a validation step but the mutation still needs the user reference.

- **`UserProcessor.RemoveRolesFromUserAsync` / `DeleteUserAsync`** — N+1: a loop calls
  `IsSoleOwnerOfAccountAsync` once per account/role, each making 3 `AnyAsync` calls.
  Batching this would require reworking `UserOwnershipProcessor`.

- **`GroupProcessor.RemoveRolesFromGroupAsync` / `DeleteGroupAsync`** — Same N+1 via
  `AnyUserHasOwnershipOutsideGroupAsync`.

---

## Out of Scope

`UserOwnershipProcessor` internally chains 3 `AnyAsync` calls with early-exit logic. Combining
them would require complex nested predicates and is not worth the risk.
