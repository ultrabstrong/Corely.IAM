# Permission Model

## Overview

Corely.IAM uses a **CRUDX permission model** (Create, Read, Update, Delete, Execute) with resource-level granularity. Permissions are the atomic unit of access control. They define *what actions* are allowed on *which resources*.

## Permission Structure

Each permission entity defines:
- **AccountId**: permissions are always scoped to an account
- **ResourceType**: exactly one kind of resource (e.g., `"group"`, `"role"`, `"user"`, or a type the host registers). There is no wildcard type
- **ResourceId**: a specific resource (`Guid`) or `Guid.Empty` for all resources of this type
- **CRUDX flags**: five booleans: `Create`, `Read`, `Update`, `Delete`, `Execute`
- **IsSystemDefined**: marks the Owner role's defaults, which cannot be deleted or detached from it

## Uniqueness Constraint

Permissions enforce uniqueness on the **full combination** of `(AccountId, ResourceType, ResourceId, Create, Read, Update, Delete, Execute)`.

This means multiple permission entities **can and should** exist for the same resource type + ID, as long as their CRUDX flags differ. This is by design:

1. **Prevents accidental duplication**: you can't create the exact same permission set twice
2. **Enables configurable permission tiers**: different CRUDX combinations for the same resource support common access patterns like readonly, editor, and owner

### Example: Three Permission Tiers for Groups

| Permission | Resource | C | R | U | D | X | Assigned To |
|------------|----------|---|---|---|---|---|-------------|
| "Read-only groups" | `group : *` | | ✓ | | | | Role: Reader Role |
| "Manage groups" | `group : *` | ✓ | ✓ | ✓ | | ✓ | Role: Admin Role |
| "Full group access" | `group : *` | ✓ | ✓ | ✓ | ✓ | ✓ | Role: Owner Role |

All three coexist for the same resource scope and differ only in their CRUDX flags.

## RBAC Chain

Permissions flow to users through a **role-based** chain. Permissions are **never assigned directly to users or groups**, only to roles.

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
- **Auditing**: understanding *why* a user has access
- **Revocation planning**: knowing which role/group changes would remove access

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
- The permission is what matters most, because it answers "what access exists?"
- The roles and assignment paths explain "how did it get here?"
- The user is the implicit context (trimmed from the tree as the known caller)
- Leaves are **distinct**. Each leaf is either "Direct" or a specific group name, with no duplication at the leaf level

If the tree were inverted (user as root, permissions as leaves), the same permission would appear multiple times across different role branches, producing redundant, non-distinct leaves.

## All Resources of a Type

`ResourceId = Guid.Empty` grants access to **all resources** of the permission's one type: read any
group, for example. A permission never reaches another type.

## Owner Defaults

When an account is created, its Owner role receives one system-defined permission per resource type
that declares owner actions:

| Scope | CRUDX | Role |
|-------|-------|------|
| `account : all`, `user : all`, `group : all`, `role : all`, `permission : all` | ✓✓✓✓✓ | Owner Role |
| `<host type> : all` | The owner actions the host registered for that type | Owner Role |

A host type registered without owner actions gives the owner nothing on it.

System-defined permissions cannot be deleted, and cannot be detached from the Owner role, so an
account keeps an owner who can manage it. Every other permission on any role, the Owner role
included, can be added and removed freely.

## Granting

Nobody can hand out access beyond their own. Creating a permission, attaching permissions to a role,
assigning roles to a user or a group, and adding users to a group succeed only if the caller holds
every action handed out, on the same type, covering the same resource. Owners are bounded by their
owner defaults; system context is not bounded. See
[Authorization](../Corely.IAM/Docs/authorization.md#granting).
