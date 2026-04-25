# SQL Server Cascade Delete Cleanup

## The Problem

SQL Server does not allow multiple cascade delete paths to the same table. When Entity Framework generates migrations, it creates ON DELETE CASCADE for many relationships by default. This works fine for MySQL/MariaDB but fails on SQL Server.

**Error:** Introducing FOREIGN KEY constraint on table may cause cycles or multiple cascade paths.

## Why This Happens

SQL Server enforces that each row can only be cascade deleted through ONE path. If a row could potentially be deleted via multiple parent relationships, SQL Server blocks the constraint.

### Example: GroupRoles Join Table

The GroupRoles table has two cascade paths:
1. Account -> Group -> GroupRoles
2. Account -> Role -> GroupRoles

SQL Server sees this as ambiguous and blocks it.

---

## Solution: NoAction with Explicit Join Entities

### Strategy

Use DeleteBehavior.NoAction on all many-to-many join table FKs:
- Database schema has NO CASCADE constraints (SQL Server accepts this)
- Application code must Include() related entities and Clear() them before deleting

### Why NoAction?

| Behavior | MySQL/MariaDB | SQL Server | Notes |
|----------|---------------|------------|-------|
| Cascade (database) | Works | Fails | SQL Server rejects multiple cascade paths |
| ClientCascade (EF) | Works | Fails | Still generates CASCADE in migration |
| NoAction (manual) | Works | Works | Requires manual cleanup in code |

Reference: https://www.billtalkstoomuch.com/2024/08/31/cascading-deletes-ef-core-and-sql-server-oh-my/

---

## Implementation

### Phase 1: Create Explicit Join Entity Classes

Created `Corely.IAM\Entities\JoinEntities.cs` with simple classes:
- [x] UserAccount, UserGroup, UserRole, GroupRole, RolePermission

### Phase 2: Update Entity Configurations

Updated entity configurations to use:
- [x] `UsingEntity<JoinClass>()` with typed entities (no hardcoded strings)
- [x] `DeleteBehavior.NoAction` on both FKs
- [x] `j.ConfigureTable()` to auto-pluralize table names

Files modified:
- [x] UserEntityConfiguration.cs (3 relationships)
- [x] GroupEntityConfiguration.cs (1 relationship)
- [x] RoleEntityConfiguration.cs (1 relationship)

### Phase 3: Update Processor Delete Methods

Delete methods must Include() and Clear() related entities before deleting.

- [x] UserProcessor.DeleteUserAsync - Include Accounts, Groups, Roles; Clear() all
- [x] GroupProcessor.DeleteGroupAsync - Include Users, Roles; Clear() both
- [x] RoleProcessor.DeleteRoleAsync - Include Users, Groups; Clear() both
- [x] PermissionProcessor.DeletePermissionAsync - Include Roles; Clear()
- [x] AccountProcessor.DeleteAccountAsync - Include Users; Clear()

### Phase 4: Regenerate Migrations

- [x] Remove existing migrations
- [x] Regenerate all migrations
- [x] Verify SQL Server migration has NO CASCADE constraints on join tables
- [x] Test: `corely-db db create` succeeds on SQL Server

---

## Final Configuration Pattern

```csharp
builder
    .HasMany(e => e.Roles)
    .WithMany(e => e.Groups)
    .UsingEntity<GroupRole>(
        j => j.HasOne<RoleEntity>().WithMany().HasForeignKey(e => e.RolesId).OnDelete(DeleteBehavior.NoAction),
        j => j.HasOne<GroupEntity>().WithMany().HasForeignKey(e => e.GroupsId).OnDelete(DeleteBehavior.NoAction),
        j => j.ConfigureTable()  // Auto-pluralizes to "GroupRoles"
    );
```

## Delete Method Pattern

```csharp
public async Task<DeleteRoleResult> DeleteRoleAsync(Guid roleId)
{
    var roleEntity = await _roleRepo.GetAsync(
        r => r.Id == roleId,
        include: q => q.Include(r => r.Users).Include(r => r.Groups)
    );
    if (roleEntity == null) return NotFound();

    // Clear join tables (NoAction side - must do manually)
    roleEntity.Users?.Clear();
    roleEntity.Groups?.Clear();

    await _roleRepo.DeleteAsync(roleEntity);
    return Success();
}
```

---

## Status

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Create explicit join entity classes | ? Complete |
| Phase 2 | Update entity configurations | ? Complete |
| Phase 3 | Update processor delete methods | ? Complete |
| Phase 4 | Regenerate migrations and test | ? Complete |

---

## Phase 6: Testing

- [ ] 6.1 SQL Server: corely-db db create succeeds
- [ ] 6.2 SQL Server: Delete operations cascade correctly
- [ ] 6.3 MySQL/MariaDB: Still works correctly

---

## Summary

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Create explicit join entity classes | Not Started |
| Phase 2 | Create join entity configurations with ClientCascade | Not Started |
| Phase 3 | Update entity configurations to use join entities | Not Started |
| Phase 4 | Update processor delete methods with Include | Not Started |
| Phase 5 | Regenerate migrations | Not Started |
| Phase 6 | Testing on all providers | Not Started |

Result: SQL Server compatible, consistent behavior across all providers, clean entity configuration code.
