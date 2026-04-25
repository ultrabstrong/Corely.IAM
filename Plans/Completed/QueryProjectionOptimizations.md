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

> **Note on mock repos:** In-memory mock repos don't auto-link entities across separate stores,
> so tests must manually populate the account's navigation collection (e.g. `account.Roles`)
> after creating child entities. See the `AddXxxToAccountAsync` helpers in each test class.

---

## Easy Wins

### ✅ 1. `RoleProcessor.CreateRoleAsync`

**Was:** GetAsync(account) → AnyAsync(role name in account) — 2 round trips  
**Now:** Single `EvaluateAsync` on `_accountRepo` projecting `{ RoleNameExists }`.  
`null` result = account not found; `RoleNameExists == true` = duplicate name.

### ✅ 2. `PermissionProcessor.CreatePermissionAsync`

**Was:** GetAsync(account) → AnyAsync(permission exists in account) — 2 round trips  
**Now:** Single `EvaluateAsync` on `_accountRepo` projecting `{ PermissionExists }`.

### ✅ 3. `GroupProcessor.CreateGroupAsync` *(bonus — same pattern)*

**Was:** GetAsync(account) → AnyAsync(group name in account) — 2 round trips  
**Now:** Single `EvaluateAsync` on `_accountRepo` projecting `{ GroupNameExists }`.

### ✅ 4. `UserProcessor.AssignRolesToUserAsync`

**Was:** GetAsync(user, Include(Accounts)) → ListAsync(roles) → client-side account filter  
**Now:** GetAsync(user, Include(Accounts)) → extract account IDs as `List<Guid>` → ListAsync
with `accountIds.Contains(r.AccountId)` pushed into the SQL `IN` clause. Eliminates
the client-side `Where` step; SQL fetches only roles from the user's own accounts.

### ⏭️ 5. `GroupProcessor.AddUsersToGroupAsync`

**Was:** GetAsync(group) → ListAsync(users filtered by group membership and account)  
**Skipped:** Mutation requires tracked `UserEntity` objects; reducing to one query would need
entity tracking in anonymous-type projections — unreliable with mock repos without significant
test infrastructure changes.

### ⏭️ 6. `GroupProcessor.AssignRolesToGroupAsync`

**Was:** GetAsync(group) → ListAsync(roles not yet in group and in same account)  
**Skipped:** Same reason as #5 — tracked entities required for M:M mutation.

### ⏭️ 7. `RoleProcessor.AssignPermissionsToRoleAsync`

**Was:** GetAsync(role) → ListAsync(permissions not yet on role and in same account)  
**Skipped:** Same reason as #5 — tracked entities required for M:M mutation.

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
