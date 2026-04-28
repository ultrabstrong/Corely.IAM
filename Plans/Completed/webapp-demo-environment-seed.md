# WebApp demo environment seed

## Problem

Seed the local `iam-webapp` SQL Server database with a realistic multi-user, multi-account demo environment using the `corely` devtool so the WebApp can be exercised with both sparse and paged states.

## Scope

1. Use the existing local `corely` configuration that already points at the WebApp demo database.
2. Keep the current `admin` user intact and use it only to execute the global seed steps.
3. Create a realistic demo dataset with multiple accounts, overlapping memberships, roles, groups, and permissions.
4. Ensure at least one account clearly shows paging and at least one account clearly stays below paging thresholds.
5. Do not execute anything yet; this file is only the reviewable seed plan.

## Assumptions

- The seed is **additive** against the existing `iam-webapp` database. No reset/drop is included in this pass.
- All newly created demo users will use password `admin`.
- Demo user emails will use the `@demo.local` domain.
- The existing `admin` user is only needed to authenticate the devtool for the initial user/account creation steps.
- If we later want the pre-existing `admin` user added into the seeded demo accounts, that will be a small follow-up because the current CLI surface does not expose a username-based user lookup.

## Proposed dataset

### Users

Create these 30 new users:

- `alice.johnson`
- `ben.carter`
- `cara.nguyen`
- `daniel.lee`
- `elena.morris`
- `farah.khan`
- `gavin.brooks`
- `hannah.price`
- `isaac.reed`
- `julia.woods`
- `kevin.bell`
- `leo.turner`
- `maya.shah`
- `nolan.gray`
- `olivia.ward`
- `paula.ramos`
- `quinn.hughes`
- `rafael.gomez`
- `sara.kim`
- `tyler.adams`
- `uma.patel`
- `victor.ross`
- `willa.ford`
- `xavier.young`
- `yasmine.ali`
- `zane.cooper`
- `seasonal.01`
- `seasonal.02`
- `seasonal.03`
- `seasonal.04`

### Accounts

| Account | Owner | Intended size | Purpose |
| --- | --- | --- | --- |
| `Acme Retail - North America` | `alice.johnson` | 30 members | Primary showcase account with paging |
| `Acme Retail - Europe` | `ben.carter` | 9 members | Medium account for account-switch demos |
| `Contoso Manufacturing` | `cara.nguyen` | 8 members | Medium account with different role mix |
| `Fabrikam Professional Services` | `daniel.lee` | 6 members | Small account with no paging |
| `Northwind Internal IT` | `elena.morris` | 5 members | Sparse account with no paging |

### Membership shape

- `Acme Retail - North America` will contain all 30 new demo users.
- `alice.johnson` will also be added to all four non-primary accounts so at least one realistic demo user can view a 5-account non-paged account switcher/list.
- Cross-account overlap will be used to make account switching feel real:
  - `Acme Retail - Europe` will contain `ben.carter`, `alice.johnson`, `farah.khan`, `gavin.brooks`, `hannah.price`, `isaac.reed`, `quinn.hughes`, `sara.kim`, and `tyler.adams`
  - `Contoso Manufacturing` will contain `cara.nguyen`, `alice.johnson`, `julia.woods`, `kevin.bell`, `leo.turner`, `maya.shah`, `uma.patel`, and `victor.ross`
  - `Fabrikam Professional Services` will contain `daniel.lee`, `alice.johnson`, `nolan.gray`, `olivia.ward`, `paula.ramos`, and `zane.cooper`
  - `Northwind Internal IT` will contain `elena.morris`, `alice.johnson`, `quinn.hughes`, `rafael.gomez`, and `sara.kim`
- The four `seasonal.*` users will stay in the large North America account only so that account has obvious paging while the others stay compact.

### Shared owner/admin scenarios

The original draft did **not** explicitly call this out, but it should. The codebase supports:

- **one bootstrap account owner** at account creation time via `OwnerUserId`
- **multiple effective owners after creation** via the system-defined `Owner Role`
- **multiple admins** via direct role assignment and/or group-based role assignment

The revised seed will include both patterns:

- `Acme Retail - North America`
  - bootstrap owner: `alice.johnson`
  - additional owner: `ben.carter` via direct `Owner Role` assignment
  - shared owner group: `Leadership` group will also carry `Owner Role` and include `alice.johnson` and `farah.khan`
  - additional admins: `gavin.brooks` and `hannah.price` via `Account Admin`
- `Acme Retail - Europe`
  - bootstrap owner: `ben.carter`
  - additional owner: `isaac.reed` via direct `Owner Role` assignment
  - additional admin: `farah.khan` via `Account Admin`
- `Contoso Manufacturing`
  - bootstrap owner: `cara.nguyen`
  - additional admins: `julia.woods` and `kevin.bell`

That gives the demo all of the following states:

- single bootstrap owner
- shared ownership via direct role assignment
- shared ownership via group membership
- multiple admins without owner-level authority
- users who are admins in one account and ordinary members in another

### North America IAM surface

This is the flagship account and will get the richest setup.

**Roles (10 total: 1 system-defined + 9 custom)**

- `Owner Role` (system-defined; created automatically with the account)
- `Account Admin`
- `Operations Manager`
- `Support Agent`
- `Billing Manager`
- `Fulfillment Lead`
- `Content Editor`
- `Read Only Analyst`
- `Security Auditor`
- `Contractor Limited`

**Groups (8)**

- `Leadership`
- `Operations`
- `Support`
- `Finance`
- `Fulfillment`
- `Analytics`
- `Contractors`
- `All Hands`

**Permissions (28 total: 27 seeded + 1 system-defined owner permission)**

- Seeded wildcard CRUDX permissions for each resource type:
  - `account.create`, `account.read`, `account.update`, `account.delete`, `account.execute`
  - `user.create`, `user.read`, `user.update`, `user.delete`, `user.execute`
  - `group.create`, `group.read`, `group.update`, `group.delete`, `group.execute`
  - `role.create`, `role.read`, `role.update`, `role.delete`, `role.execute`
  - `permission.create`, `permission.read`, `permission.update`, `permission.delete`, `permission.execute`
- Seeded global wildcard permissions:
  - `all.read`
  - `all.execute`

These use the built-in resource types:

- `account`
- `user`
- `group`
- `role`
- `permission`
- `*`

All seeded permissions will use `Guid.Empty` as the wildcard resource ID. The account will also contain the system-defined wildcard permission that backs the auto-created `Owner Role`.

### Secondary account IAM surface

Each non-flagship account will get a smaller, cleaner variant:

- **Roles:** 3-5 per account, plus the system-defined `Owner Role`
- **Groups:** 2-4 per account
- **Permissions:** 6-10 per account

The secondary accounts will reuse the same naming style but with smaller catalogs so they stay below the default page size and show the simpler WebApp state.

## Intended UI states

- **Paged**
  - `Acme Retail - North America` users list (`30 > default take 25`)
  - `Acme Retail - North America` permissions list (`28 > default take 25`)
- **Non-paged**
  - Accounts list for `alice.johnson` (`5`)
  - North America roles (`10`)
  - North America groups (`8`)
  - All secondary-account lists

## Command plan

### 1. Preflight

Use the existing local tool configuration and confirm the DB connection before creating anything.

```powershell
corely config show
corely config test-connection
```

Create a temp working folder for request files and captured IDs:

```powershell
$seedRoot = Join-Path $env:TEMP "corely-webapp-demo"
New-Item -ItemType Directory -Force -Path $seedRoot | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $seedRoot "requests") | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $seedRoot "results") | Out-Null
```

### 2. Create the 30 demo users

User registration is unauthenticated in this tool surface, so these can be created before any sign-in step.

For each username above, write a `RegisterUserRequest` JSON file and call:

```powershell
corely register user <RequestJsonFile>
```

Representative payload shape:

```json
{
  "Username": "alice.johnson",
  "Email": "alice.johnson@demo.local",
  "Password": "admin"
}
```

During execution, each result JSON will be captured so the created user IDs can be reused for account ownership and memberships.

### 3. Create the five accounts as their future owners

Account creation is an authenticated **self** operation. Each account must be created while signed in as the same user referenced by `OwnerUserId`.

```powershell
$signInPath = Join-Path $seedRoot "requests\signin-alice.json"
@'
{
  "Username": "alice.johnson",
  "Password": "admin",
  "DeviceId": "webapp-demo-seed"
}
'@ | Set-Content -Path $signInPath

corely auth signin $signInPath
```

For each planned account, sign in as its owner, write a `RegisterAccountRequest` JSON file using that same user ID, and call:

```powershell
corely register account <RequestJsonFile>
```

Representative payload shape:

```json
{
  "AccountName": "Acme Retail - North America",
  "OwnerUserId": "<alice.johnson user id>"
}
```

### 4. Add users to accounts

For each membership edge in the dataset above, write a `RegisterUserWithAccountRequest` JSON file and call:

```powershell
corely register user-with-account <RequestJsonFile>
```

Representative payload shape:

```json
{
  "UserId": "<user id>",
  "AccountId": "<account id>"
}
```

### 5. Build the North America account surface

Authenticate as the account owner so all account-scoped creation happens inside the account being seeded.

```powershell
corely auth signout-all
corely auth signin <alice-signin-with-account-json>
```

Representative sign-in payload:

```json
{
  "Username": "alice.johnson",
  "Password": "admin",
  "DeviceId": "webapp-demo-seed",
  "AccountId": "<Acme Retail - North America account id>"
}
```

Create custom roles:

```powershell
corely register role <RequestJsonFile>
```

Representative payload:

```json
{
  "RoleName": "Operations Manager",
  "AccountId": "<Acme Retail - North America account id>"
}
```

The system-defined `Owner Role` will not be created manually; it is already created when the account is registered.

Create groups:

```powershell
corely register group <RequestJsonFile>
```

Representative payload:

```json
{
  "GroupName": "Operations",
  "AccountId": "<Acme Retail - North America account id>"
}
```

Create permissions:

```powershell
corely register permission <RequestJsonFile>
```

Representative payload:

```json
{
  "AccountId": "<Acme Retail - North America account id>",
  "ResourceType": "user",
  "ResourceId": "00000000-0000-0000-0000-000000000000",
  "Create": false,
  "Read": true,
  "Update": false,
  "Delete": false,
  "Execute": false,
  "Description": "user.read"
}
```

Attach permissions to roles:

```powershell
corely register permissions-with-role <RequestJsonFile>
```

Representative payload:

```json
{
  "PermissionIds": [
    "<user.read permission id>",
    "<group.read permission id>",
    "<role.read permission id>"
  ],
  "RoleId": "<Read Only Analyst role id>",
  "AccountId": "<Acme Retail - North America account id>"
}
```

Attach roles to groups:

```powershell
corely register roles-with-group <RequestJsonFile>
```

Representative payload for group-based shared ownership:

```json
{
  "RoleIds": [
    "<Owner Role id>",
    "<Account Admin role id>"
  ],
  "GroupId": "<Leadership group id>",
  "AccountId": "<Acme Retail - North America account id>"
}
```

Attach roles directly to selected users:

```powershell
corely register roles-with-user <RequestJsonFile>
```

Representative payload for shared ownership:

```json
{
  "RoleIds": [
    "<Owner Role id>"
  ],
  "UserId": "<ben.carter user id>",
  "AccountId": "<Acme Retail - North America account id>"
}
```

Attach users to groups:

```powershell
corely register users-with-group <RequestJsonFile>
```

Representative payload:

```json
{
  "UserIds": [
    "<alice.johnson user id>",
    "<farah.khan user id>"
  ],
  "GroupId": "<Leadership group id>",
  "AccountId": "<Acme Retail - North America account id>"
}
```

### 6. Build the four smaller accounts

Repeat the same pattern per account using the account owner for authentication:

- sign in as the owner with `AccountId`
- create the smaller role/group/permission catalog
- wire permissions to roles
- wire roles to groups
- add selected direct role assignments

The same `corely` commands will be used:

- `corely auth signin`
- `corely register role`
- `corely register group`
- `corely register permission`
- `corely register permissions-with-role`
- `corely register roles-with-group`
- `corely register roles-with-user`
- `corely register users-with-group`

### 7. Retrieval checks after seeding

Use the list commands to verify the intended paged and non-paged states:

```powershell
corely retrieval list-accounts -s 0 -t 25
corely retrieval list-users -a <Acme Retail - North America account id> -s 0 -t 25
corely retrieval list-users -a <Acme Retail - North America account id> -s 25 -t 25
corely retrieval list-roles -a <Acme Retail - North America account id> -s 0 -t 25
corely retrieval list-groups -a <Acme Retail - North America account id> -s 0 -t 25
corely retrieval list-permissions -a <Acme Retail - North America account id> -s 0 -t 25
corely retrieval list-permissions -a <Acme Retail - North America account id> -s 25 -t 25
```

## Notes

- I plan to capture all created IDs into a temp state file during execution so the account/member/role/group/permission wiring stays deterministic.
- I am intentionally not including invitation, MFA, Google auth, or password-recovery demo data in this first seed pass. The goal here is a dense but predictable authorization demo environment.
- If this seed feels good after review, the execution pass can be codified as a reusable PowerShell script instead of being re-run manually.

## Status

- Command and model audit complete.
- Plan written for review.
- No seed commands executed yet.
