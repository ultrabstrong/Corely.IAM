# Permission Model

## Overview

Corely.IAM uses a **CRUDX permission model** (Create, Read, Update, Delete, Execute) with resource-level granularity. Permissions are the atomic unit of access control — they define *what actions* are allowed on *which resources*.

## Permission Structure

Each permission entity defines:
- **AccountId** — permissions are always scoped to an account
- **ResourceType** — the kind of resource (e.g., `"group"`, `"role"`, `"user"`, or `"*"` for all types)
- **ResourceId** — a specific resource (`Guid`) or wildcard (`Guid.Empty` = all resources of this type)
- **CRUDX flags** — five booleans: `Create`, `Read`, `Update`, `Delete`, `Execute`
- **IsSystemDefined** — protects default permissions from deletion

## Uniqueness Constraint

Permissions enforce uniqueness on the **full combination** of `(AccountId, ResourceType, ResourceId, Create, Read, Update, Delete, Execute)`.

This means multiple permission entities **can and should** exist for the same resource type + ID, as long as their CRUDX flags differ. This is by design:

1. **Prevents accidental duplication** — you can't create the exact same permission set twice
2. **Enables configurable permission tiers** — different CRUDX combinations for the same resource support common access patterns like readonly, editor, and owner

### Example: Three Permission Tiers for Groups

| Permission | Resource | C | R | U | D | X | Assigned To |
|------------|----------|---|---|---|---|---|-------------|
| "Read-only groups" | `group : *` | | ✓ | | | | Role: Reader Role |
| "Manage groups" | `group : *` | ✓ | ✓ | ✓ | | ✓ | Role: Admin Role |
| "Full group access" | `group : *` | ✓ | ✓ | ✓ | ✓ | ✓ | Role: Owner Role |

All three coexist for the same resource scope — they differ in their CRUDX flags.

## RBAC Chain

Permissions flow to users through a **role-based** chain. Permissions are **never assigned directly to users or groups** — only to roles.

```
Permission → Role → User       (direct assignment)
Permission → Role → Group → User  (group assignment)
```

A user's **effective permissions** are the union of all permissions reachable through both paths.

## Assignment Paths & Duplication

A user may receive the same permission through multiple assignment paths. This is normal and expected.

### Example: Overlapping Assignments

```
Permission "Manage groups" (CR✓U✓X)
├── Role "Admin Role"
│   ├── Direct (assigned to user)          ← path 1
│   └── Group "Engineering"                ← path 2
│       └── (user is member)
└── Role "Team Lead"
    └── Group "Project Alpha"              ← path 3
        └── (user is member)
```

The user receives the "Manage groups" permission via **three** paths. The effective access is the same regardless of how many paths exist, but the assignment paths matter for:
- **Auditing** — understanding *why* a user has access
- **Revocation planning** — knowing which role/group changes would remove access

## Effective Permissions Tree

When retrieving a resource, the system returns the caller's effective permissions structured as a **permission-rooted tree**:

```
Resource: group (specific or wildcard)
├── Permission "Full group access" (CRUDX: ✓✓✓✓✓)
│   └── Role "Owner Role"
│       └── Direct
├── Permission "Manage groups" (CRUDX: ✓✓✓✗✓)
│   └── Role "Admin Role"
│       ├── Direct
│       └── Group "Engineering"
└── Permission "Read-only groups" (CRUDX: ✗✓✗✗✗)
    └── Role "Reader Role"
        └── Group "Everyone"
```

The tree is **permission-rooted** (not user-rooted) because:
- The permission is what matters most — it answers "what access exists?"
- The roles and assignment paths explain "how did it get here?"
- The user is the implicit context (trimmed from the tree as the known caller)
- Leaves are **distinct** — each leaf is either "Direct" or a specific group name, with no duplication at the leaf level

If the tree were inverted (user as root, permissions as leaves), the same permission would appear multiple times across different role branches, producing redundant, non-distinct leaves.

## Wildcard Dimensions

Permissions support two independent wildcard dimensions:

| Wildcard | Meaning | Example |
|----------|---------|---------|
| `ResourceId = Guid.Empty` | Access to **all resources** of this type | Read any group |
| `ResourceType = "*"` | Access to **all resource types** | Read any resource of any type |

These can combine: `ResourceType = "*"` + `ResourceId = Guid.Empty` = full access to everything (used by the default Owner permission).

## Default System Permissions

Three system-defined permissions are created automatically for each account:

| Permission | Scope | CRUDX | Default Role |
|------------|-------|-------|-------------|
| Owner Role - Full access | `* : *` | ✓✓✓✓✓ | Owner Role |
| Admin Role - Manage | `* : *` | ✓✓✓✗✓ | Admin Role |
| Reader Role - Read only | `* : *` | ✗✓✗✗✗ | Reader Role |

System-defined permissions cannot be deleted (`IsSystemDefined = true`).

### Protected Assignment

The **Owner permission on the Owner Role** is the only protected role-permission assignment. It cannot be removed because doing so would leave the account without an owner, breaking the ownerless-account invariant that underpins authorization checks.

All other system permission assignments — including Admin Role and Reader Role — can be removed and replaced with custom permissions. Consumers have full control over which permissions are attached to system-defined roles, with the single exception above.
