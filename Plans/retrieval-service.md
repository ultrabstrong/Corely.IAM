# Plan: Retrieval Service

## Context

The library currently has no comprehensive read/list capability exposed to external callers. Individual processors have some Get methods, but there's no unified retrieval surface with filtering, paging, or hydration support.

## Implementation Progress

### Phase 0: Scaffolding
- [ ] 0a. Filtering & ordering system (Corely.Common)
  - [ ] Filter types (StringFilter, ComparableFilter, GuidFilter, BoolFilter, EnumFilter)
  - [ ] FilterBuilder with Where overloads
  - [ ] ExpressionTranslator
  - [ ] OrderBuilder with By/ThenBy
  - [ ] Unit tests (filter translation + order builder)
- [ ] 0b. Shared models & utilities (Corely.IAM)
  - [ ] PagedResult
  - [ ] ChildRef
  - [ ] EffectivePermission / EffectiveRole / EffectiveGroup
  - [ ] PermissionLabelProvider + refactor Permission.CrudxString
- [ ] 0c. Service shell (Corely.IAM)
  - [ ] IRetrievalService interface
  - [ ] RetrievalService implementation shell
  - [ ] Authorization + Telemetry decorators (shell)
  - [ ] DI registration

### Phase 1: Permission
- [ ] Processor List/Get methods
- [ ] RetrievalService wiring
- [ ] Authorization decorator
- [ ] Telemetry decorator
- [ ] DevTools CLI command
- [ ] ConsoleTest example
- [ ] Unit tests

### Phase 2: Group
- [ ] Processor List/Get methods
- [ ] RetrievalService wiring
- [ ] Authorization decorator
- [ ] Telemetry decorator
- [ ] DevTools CLI command
- [ ] ConsoleTest example
- [ ] Unit tests

### Phase 3: Role
- [ ] Processor List/Get methods
- [ ] RetrievalService wiring
- [ ] Authorization decorator
- [ ] Telemetry decorator
- [ ] DevTools CLI command
- [ ] ConsoleTest example
- [ ] Unit tests

### Phase 4: User
- [ ] Processor List/Get methods
- [ ] RetrievalService wiring
- [ ] Authorization decorator
- [ ] Telemetry decorator
- [ ] DevTools CLI command
- [ ] ConsoleTest example
- [ ] Unit tests

### Phase 5: Account
- [ ] Processor List/Get methods
- [ ] RetrievalService wiring
- [ ] Authorization decorator
- [ ] Telemetry decorator
- [ ] DevTools CLI command
- [ ] ConsoleTest example
- [ ] Unit tests

---

## Design Decisions

### 1. ✅ Service & Processor Structure

- **`IRetrievalService`** — public service interface, the single entry point for external callers to read data
- Actual Read/List methods live on existing processors (AccountProcessor, UserProcessor, GroupProcessor, RoleProcessor, PermissionProcessor)
- RetrievalService orchestrates calls to processors, same as Registration/Deregistration pattern
- Authorization + Telemetry decorators follow existing pattern

### 2. ✅ Filtering

#### ✅ Expression-Based FilterBuilder with Typed Filter Operations

Callers select properties via lambda expressions (compiler-verified) and apply pre-rolled filter types (library-controlled). No parallel filter classes to maintain — adding a property to a model makes it immediately filterable.

```csharp
// Caller usage
var filter = Filter.For<Group>()
    .Where(g => g.Name, StringFilter.Contains("Engineering"))
    .Where(g => g.CreatedDate, DateFilter.Between(jan1, jan31))
    .Where(g => g.Users, users => users
        .Where(u => u.Username, StringFilter.StartsWith("j")));
```

**Why this pattern:**
- **No maintenance overhead** — no `GroupFilter`, `UserFilter`, etc. to keep in sync with models. Property added/removed = compiler error at filter sites, not silent breakage.
- **Safe by design** — callers never write raw `Expression<Func<T, bool>>`. The library constructs the expression internally from property selectors (member access only) + filter operations (known LINQ translations). Impossible to smuggle in method calls, DB lookups, or untranslatable expressions.
- **Discoverable** — IntelliSense shows available properties (from the model) and available operations (from the filter type). No guessing.
- **Enhance once, apply everywhere** — adding a new operation to `StringFilter` gives it to every string property across all entities.

#### ✅ Filter Types

| Filter Type | Covers | Operations |
|---|---|---|
| **`StringFilter`** | `string`, `string?` | Equals, NotEquals, Contains, NotContains, StartsWith, NotStartsWith, EndsWith, NotEndsWith, In, NotIn, IsNull, IsNotNull |
| **`ComparableFilter<T>`** | `int`, `long`, `float`, `double`, `decimal`, `DateTime`, `DateTimeOffset` (+ nullable) | Equals, NotEquals, GreaterThan, GreaterThanOrEqual, LessThan, LessThanOrEqual, Between, NotBetween, In, NotIn, IsNull, IsNotNull |
| **`GuidFilter`** | `Guid`, `Guid?` | Equals, NotEquals, In, NotIn, IsNull, IsNotNull |
| **`BoolFilter`** | `bool`, `bool?` | IsTrue, IsFalse, IsNull, IsNotNull |
| **`EnumFilter<TEnum>`** | any `enum` (+ nullable) | Equals, NotEquals, In, NotIn, IsNull, IsNotNull |

- Each operation is a **static factory method** (e.g., `StringFilter.Contains("eng")`) returning a filter object the library knows how to translate
- `Between` = inclusive on both ends (`>=` and `<=`); `NotBetween` = outside the range (`<` low OR `>` high)
- `IsNull`/`IsNotNull` on non-nullable properties = compile error (overload doesn't exist)
- Multiple `.Where()` calls **AND** together. OR is deferred — adds grouping/precedence complexity, AND covers the vast majority of filter use cases.

#### ✅ FilterBuilder Overloads

Type mapping is automatic via overloads — the compiler enforces which filter types apply to which property types:

```csharp
Where(Expression<Func<T, string>>    prop, StringFilter filter)
Where(Expression<Func<T, int>>       prop, ComparableFilter<int> filter)
Where(Expression<Func<T, DateTime>>  prop, ComparableFilter<DateTime> filter)
Where(Expression<Func<T, Guid>>      prop, GuidFilter filter)
Where(Expression<Func<T, bool>>      prop, BoolFilter filter)
Where<TEnum>(..., EnumFilter<TEnum> filter) where TEnum : struct, Enum
// Nullable overloads — same filter types, adds IsNull/IsNotNull availability
Where(Expression<Func<T, int?>>      prop, ComparableFilter<int> filter)
// ... etc for all nullable value types
// Collection navigation — one level deep
Where<TChild>(Expression<Func<T, IEnumerable<TChild>>> collection, Action<FilterBuilder<TChild>> childFilter)
```

#### ✅ Nested Filters (One Level Deep)

Collection navigation properties get a child `FilterBuilder<TChild>` — same pattern, same safety, wrapped in `.Any()` by the library:

```csharp
// "Groups containing a user whose username starts with 'j'"
Filter.For<Group>()
    .Where(g => g.Users, users => users
        .Where(u => u.Username, StringFilter.StartsWith("j")));

// Translates internally to:
// g => g.Users.Any(u => u.Username.StartsWith("j"))
```

✅ **Capped at one level** — the child `FilterBuilder` does not expose the collection `Where` overload. Matches the domain (M:M relationships are all one hop) and prevents deep filter trees that generate nightmarish SQL.

#### ✅ Architecture: Lives in Corely.Common

The entire filtering system is **domain-agnostic** — nothing about translating `StringFilter.Contains("eng")` to `.Where(x => x.Name.Contains("eng"))` has anything to do with IAM. Full package goes into Corely.Common from the start:

```
Corely.Common
└── Filtering/
    ├── Filters/              (StringFilter, ComparableFilter<T>, GuidFilter, BoolFilter, EnumFilter<TEnum>)
    ├── FilterBuilder<T>      (Where overloads, accumulates filter state)
    └── ExpressionTranslator  (turns FilterBuilder state into Expression<Func<T, bool>>)
```

Corely.IAM (and any future consumer) just calls `Filter.For<T>().Where(...)` and passes the result to the repo layer. No coupling.

#### ✅ Translation Layer — How It Works

The `ExpressionTranslator` converts `FilterBuilder<T>` state into an `Expression<Func<T, bool>>` that EF Core can translate to SQL. This is the only place real logic lives.

**Translation steps:**
1. For each `.Where()` call, the builder stores a **(property expression, filter object)** pair
2. When building the final expression, for each pair:
   - Extract the `MemberExpression` from the property selector (e.g., `g => g.Name` → `g.Name`)
   - Read the filter object's operation and value (e.g., `StringFilter.Contains("eng")` → op=Contains, value="eng")
   - Build the corresponding LINQ expression node (e.g., `Expression.Call(memberExpr, "Contains", stringConstant)`)
3. AND all individual predicate expressions together via `Expression.AndAlso`
4. For nested collection filters: build the child predicate the same way, then wrap in an `Expression.Call` to `Enumerable.Any<TChild>(collection, childPredicate)`
5. Return the composed `Expression<Func<T, bool>>`

**Translation map (filter operation → LINQ expression):**

| Operation | Expression |
|---|---|
| `StringFilter.Contains(v)` | `x.Prop.Contains(v)` |
| `StringFilter.StartsWith(v)` | `x.Prop.StartsWith(v)` |
| `StringFilter.Equals(v)` | `x.Prop == v` |
| `StringFilter.NotContains(v)` | `!x.Prop.Contains(v)` |
| `ComparableFilter.GreaterThan(v)` | `x.Prop > v` |
| `ComparableFilter.Between(lo, hi)` | `x.Prop >= lo && x.Prop <= hi` |
| `ComparableFilter.NotBetween(lo, hi)` | `x.Prop < lo \|\| x.Prop > hi` |
| `ComparableFilter.In(v[])` | `v.Contains(x.Prop)` |
| `GuidFilter.Equals(v)` | `x.Prop == v` |
| `BoolFilter.IsTrue()` | `x.Prop == true` |
| `*.IsNull()` | `x.Prop == null` |
| `*.IsNotNull()` | `x.Prop != null` |
| Nested collection filter | `x.Collection.Any(child => [child predicate])` |

All translations use standard LINQ expression tree nodes that EF Core knows how to convert to SQL across all providers.

#### ✅ Testing Strategy

**What does NOT need tests — filter type definitions:**
The filter types (`StringFilter`, `ComparableFilter<T>`, etc.) are data containers. `StringFilter.Contains("eng")` just stores an operation enum and a value. Nothing to test.

**What DOES need tests — ExpressionTranslator:**
The translation layer is where real logic and real bug risk lives. Every operation needs a test confirming it produces the correct expression. Tests live in Corely.Common's test suite.

Tests per filter type:
- Each operation produces the correct expression (e.g., `Contains` → `.Contains()` call)
- Negated operations work correctly (e.g., `NotBetween` → `< lo || > hi`, not `!(>= lo && <= hi)`)
- `IsNull`/`IsNotNull` on nullable properties
- Multiple `.Where()` calls AND together
- Nested collection filters wrap in `.Any()` with correct child predicate
- Edge cases: empty filter (no `.Where()` calls) → no predicate (return all)

**What does NOT need tests in Corely.IAM:**
IAM doesn't test filter translation — that's Corely.Common's job. IAM tests would only verify that the right `FilterBuilder` is passed to the repo layer (behavioral/mock tests), not that filters themselves produce correct expressions.

~~**Rejected alternatives:**~~
- ~~Typed filter objects per entity~~ — maintenance burden: every new model property requires a corresponding filter property. Three places to update instead of two.
- ~~Raw `Expression<Func<T, bool>>`~~ — the footgun: callers can write expressions that don't translate to SQL, trigger client-side evaluation, or cause separate DB lookups. Library has zero control.
- ~~Generic specification pattern~~ — still requires domain-specific spec classes per entity. Same maintenance burden as typed filters with more abstraction overhead.

### 3. ✅ Ordering (List only)

#### ✅ OrderBuilder — Consistent API with FilterBuilder

Lightweight `OrderBuilder<T>` in Corely.Common. Same property-selector pattern as FilterBuilder for API consistency and compiler-verified safety, but much thinner — ordering only has two operations (ascending/descending), so there's no equivalent of the filter ExpressionTranslator's per-operation complexity.

```csharp
// Caller usage — consistent with Filter.For<T>().Where(...)
var order = Order.For<Group>()
    .By(g => g.Name, SortDirection.Ascending)
    .ThenBy(g => g.CreatedDate, SortDirection.Descending);
```

**Under the hood:** a list of `(Expression, SortDirection)` pairs that translate to `.OrderBy()` / `.OrderByDescending()` / `.ThenBy()` / `.ThenByDescending()`. Translation is trivial.

**Design details:**
- `By()` — primary sort (required first call, resets any existing sort)
- `ThenBy()` — secondary/tertiary sorts (only valid after `By()`)
- `SortDirection` enum: `Ascending`, `Descending`
- Property selectors are the same safe member-access expressions used in FilterBuilder
- **Default when no ordering specified:** primary key, for deterministic paging

**Architecture:** lives in Corely.Common alongside the filtering system. `Filter.For<T>()` + `Order.For<T>()` = consistent, discoverable API.

**Testing:** minimal — translation is direct mapping to LINQ OrderBy/ThenBy. Test that multiple `By`/`ThenBy` calls produce the correct chain and that the default (no ordering) falls back to primary key.

### 4. ✅ Two Operations: List and Get

**List** — for browsing and selecting
- Returns collections of entities (properties only, no hydration)
- Supports filtering (see section 2)
- Supports ordering (see section 3)
- Supports database-level paging (see section 5)
- Focused on answering: "what's out there?"

**Get** — for inspecting a single entity
- Takes an ID only (you List first, pick one, then Get it)
- Supports hydration of direct associations (see section 6)
- Always includes the caller's effective permissions on this resource (see section 7)
- Focused on answering: "tell me everything about this one thing"

This separation keeps each operation focused. List doesn't get muddied with dependency trees for N entities, and Get doesn't need paging/filtering logic.

### 5. ✅ Database-Level Paging (List only)

✅ Convention: **Skip** and **Take** parameters, passed through to `IRepo` layer (maps to EF Core's `.Skip()` / `.Take()`).

- Both optional with sensible defaults (Skip: 0, Take: 25)
- ✅ Default page size: **25**

✅ **List returns a `PagedResult<T>`** containing:
- **Items** — the data page
- **TotalCount** — from a parallel `CountAsync` query (same predicate, no paging)
- **CurrentPage** — calculated: `(skip / take) + 1`
- **HasMore** — calculated: `skip + take < totalCount`

The library does the paging math so callers don't have to. Skip/Take are the inputs; CurrentPage/HasMore are derived outputs.

### 6. ✅ Hydration (Get only)

✅ **One level deep** only. No nested hydration.

✅ Hydrated children are **names/IDs only** — the association is visible, not the full child entity. To inspect a child's details, the caller must Get that entity separately (with its own READ permission check).

Examples:
- Get a Group → returns group properties + hydrated child names/IDs
- Hydrated children: Users (name/ID), Roles (name/ID)
- Does NOT include: Group → Roles → Permissions (grandchildren, not supported)

✅ **Single `bool Hydrate` flag** — either include all direct children (names/IDs) or none. No per-relation granularity. The UI can decide what to display; the library doesn't need more nuance. Keeping this as a single boolean also firmly closes the door on "why not support filtering for relations" — we don't go there.

### 7. ✅ Effective Permissions on Get

When Getting a resource, the response **always includes** the caller's effective permissions as a **permission-rooted tree**:

```
Permission (CRUDX flags)
└── Role (name)
    ├── Direct
    └── Group (name)
```

- Permission is the root — answers "what access exists?"
- Roles explain which role grants this permission
- Leaves are distinct assignment paths: "Direct" or group name(s)
- User is implicit (the caller) and trimmed from the tree

Full detail: permission CRUDX + role name + assignment type (direct / group name). See `Docs/Permission Model.md` for comprehensive documentation.

### 8. ✅ Permissions on Hydrated Children

**Read on parent = full visibility of child associations (names/IDs only).**

- READ on Group shows all Users and Roles belonging to that group
- Children are names/IDs only — not full entity details
- To inspect a child entity's details, caller must Get it separately (requires READ on that entity)

~~Rejected alternative~~ — filtering children by caller's permissions:
- Introduces ambiguity ("why can't I see user X in this group?")
- Performance cost: N additional permission checks per hydrated entity
- Partial views of IAM membership are arguably less secure than full views

### 9. ✅ AccountId / UserId Scoping

**AccountId and UserId are never passed as parameters to services.** They come from `IUserContextProvider`, which requires the caller to be authenticated into a specific account before performing any scoped operations. This is an existing design principle enforced across the library — retrieval follows the same pattern.

---

## Repo Usage Examples (Corely.DataAccess)

> **Repo API recap** — `IReadonlyRepo<T>` provides: `GetAsync` (single entity, predicate + include + orderBy),
> `ListAsync` (collection, predicate + include + orderBy), `AnyAsync`, `CountAsync`,
> `QueryAsync<TResult>` (full LINQ pipeline with projection, paging, ordering — server-side),
> `EvaluateAsync<TResult>` (arbitrary aggregate/single-result). All query customizations
> (`include`, `orderBy`) are available on both `GetAsync` and `ListAsync`.
> See [Corely.DataAccess Docs](https://github.com/ultrabstrong/Corely.DataAccess/tree/master/Docs).

### A. List with Paging

Two queries: data page + total count. Both run server-side via the repo layer.

```csharp
// Data page — QueryAsync gives us full LINQ: Where → OrderBy → Skip → Take → Select
var groups = await _groupRepo.QueryAsync(q =>
    q.Where(g => g.AccountId == accountId && g.Name.Contains(nameFilter))
     .OrderBy(g => g.Name)
     .Skip(skip)
     .Take(take)
     .Select(g => new GroupListItem
     {
         Id = g.Id,
         Name = g.Name,
         Description = g.Description
     }));

// Total count — same predicate, no paging
var totalCount = await _groupRepo.CountAsync(
    g => g.AccountId == accountId && g.Name.Contains(nameFilter));
```

**✅ No concerns.** This is textbook EF Core usage. Projection keeps the SQL lean (only selected columns), ordering makes paging deterministic, and it all translates cleanly across MySQL / MariaDB / SQL Server.

### B. Get by ID (No Hydration)

Simple `GetAsync` with predicate. Returns the full entity, mapped to the domain model.

```csharp
var groupEntity = await _groupRepo.GetAsync(g => g.Id == groupId);
// Map to Group model (existing mapper pattern)
```

**✅ No concerns.** Identical to existing Get patterns in the codebase.

### C. Get with Hydration

Use `EvaluateAsync` with `.Select()` to project at the database level. EF Core translates nested `.Select()` into SQL — the DB only returns the columns/shape we ask for, not full entities. This means child tables only contribute their `Id` and `Name` columns to the result set.

```csharp
var result = await _groupRepo.EvaluateAsync((q, ct) =>
    q.Where(g => g.Id == groupId)
     .Select(g => new GetGroupResult
     {
         Id = g.Id,
         Name = g.Name,
         Description = g.Description,
         Users = g.Users!.Select(u => new ChildRef(u.Id, u.Username)).ToList(),
         Roles = g.Roles!.Select(r => new ChildRef(r.Id, r.Name)).ToList()
     })
     .FirstOrDefaultAsync(ct));
// EffectivePermissions attached separately — see section D
```

**Hydration children per entity:**

| Entity | Hydrated Children (name/ID only) |
|--------|----------------------------------|
| Account | Users, Groups, Roles, Permissions |
| User | Accounts, Groups, Roles |
| Group | Users, Roles |
| Role | Users, Groups, Permissions |
| Permission | Roles |

**✅ No major concerns.** Projection keeps the SQL lean — child tables only contribute `Id` and `Name` columns. EF Core handles nested `.Select()` on navigation properties well across all 3 providers. This is the same pattern as section A (List with Paging) but with `EvaluateAsync` + `FirstOrDefaultAsync` for a single result instead of `QueryAsync` for a list.

**Fallback (Include-based):** If projection causes issues with a specific provider, `GetAsync` with `Include` chains is the simpler alternative — loads full child entities, project in-memory. Acceptable for a single parent entity.

### D. Effective Permission Tree

#### ✅ Single Projection Query

```csharp
var effectivePermissions = await _permissionRepo.QueryAsync(q =>
    q.Where(p =>
            p.AccountId == accountId
            && (p.ResourceType == resourceType
                || p.ResourceType == PermissionConstants.ALL_RESOURCE_TYPES)
            && (p.ResourceId == resourceId || p.ResourceId == Guid.Empty)
            && p.Roles!.Any(r =>
                r.Users!.Any(u => u.Id == userId)
                || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userId))))
     .Select(p => new EffectivePermission
     {
         PermissionId = p.Id,
         Create = p.Create,
         Read = p.Read,
         Update = p.Update,
         Delete = p.Delete,
         Execute = p.Execute,
         Description = p.Description,
         ResourceType = p.ResourceType,
         ResourceId = p.ResourceId,
         Roles = p.Roles!
             .Where(r =>
                 r.Users!.Any(u => u.Id == userId)
                 || r.Groups!.Any(g => g.Users!.Any(u => u.Id == userId)))
             .Select(r => new EffectiveRole
             {
                 RoleId = r.Id,
                 RoleName = r.Name,
                 IsDirect = r.Users!.Any(u => u.Id == userId),
                 Groups = r.Groups!
                     .Where(g => g.Users!.Any(u => u.Id == userId))
                     .Select(g => new EffectiveGroup
                     {
                         GroupId = g.Id,
                         GroupName = g.Name
                     })
                     .ToList()
             })
             .ToList()
     }));
```

**Result shape (matches our tree design):**
```
EffectivePermission (CRUDX)
├── EffectiveRole (name, isDirect)
│   ├── if isDirect: leaf = "Direct"
│   └── EffectiveGroup[] (name) — groups through which this role reaches the user
```

**✅ Decision: Single query with full server-side projection.** Performance is critical for a security layer — every Get pays the cost of this query, so minimizing round-trips matters. The nested SQL complexity is a one-time cost to get right and then it doesn't get touched again.

**Notes on EF Core projection behavior:**
- `.Select()` inside `QueryAsync` translates to SQL — the DB builds and returns the projected shape, not full entities ([Microsoft Docs – Efficient Querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying))
- Nested `.Select()` on navigation properties is supported in EF Core 5+ and generates correlated subqueries ([Ben Cull – Expression and Projection Magic](https://bencull.com/blog/expression-projection-magic-entity-framework-core))
- EF Core 7+ throws on untranslatable expressions by default rather than silently falling back to client-side evaluation ([Microsoft Docs – Complex Query Operators](https://learn.microsoft.com/en-us/ef/core/querying/complex-query-operators))

**⚠️ Action item:** Test this query against all 3 DB providers (MySQL, MariaDB, SQL Server) early to confirm translation. The nested `.Where()` + `.Select()` + `.Any()` pattern is the most aggressive LINQ-to-SQL we'd use.

**✅ CrudxString formatting:** Shared utility `PermissionLabelProvider` — takes 5 CRUDX bools, returns the formatted string (e.g., `CRudx`). Refactor existing `Permission.CrudxString` (currently private, `Permission.cs:19`) to delegate to it. EffectivePermission and any future CRUDX-bearing models use the same provider.

---

## 🔒 Out of Scope: Security Key Retrieval

### What Are Security Keys?

Accounts and Users each have collections of **SymmetricKeys** and **AsymmetricKeys** — one-to-many relationships with a unique constraint on `(OwnerId, KeyUsedFor)`. These are **not internal plumbing** — they're a first-class feature of the library.

Every Account and User gets auto-provisioned, resource-scoped cryptographic keys out of the box. These integrate with Corely.Security to provide injectable, versioned, upgradable encryption, signing, and hashing capabilities for whatever purposes the consuming application needs (e.g., encrypting user-uploaded documents, sending encrypted messages to a user or account).

### Why Not Include in Retrieval?

Security keys contain sensitive cryptographic material:
- **Symmetric keys** — encrypted key material (`ISymmetricEncryptedValue`)
- **Asymmetric keys** — encrypted private key (`ISymmetricEncryptedValue`) + plaintext public key

Even though key material is stored encrypted, exposing it through the general-purpose retrieval service conflates "tell me about this entity" with "give me access to cryptographic secrets." These deserve a more intentional access model.

### 🚧 Ideas for Future Design

- **Dedicated key permission type** — a new permission scope specifically for Account/User security keys, separate from the entity-level CRUDX permissions. READ on a User shouldn't automatically grant visibility into their key material.
- **Metadata-only retrieval** — expose key *existence* (KeyUsedFor, ProviderTypeCode, Version, CreatedUtc) without key material. Answers "what keys does this user have?" without exposing secrets.
- **Asymmetric public key access** — public keys are designed to be shared (that's the point). Could be retrievable separately or with lighter permission requirements than private key material.
- **Key operations via Corely.Security** — actual encrypt/decrypt/sign/verify operations may belong entirely in the Security layer, not the retrieval layer.

### ✅ Decision

**Security keys are excluded from retrieval scope.** They do not appear in List results, Get results, or hydration for Account/User. A separate design pass is needed to determine the right permission model and access patterns for cryptographic key material.

---

## ✅ Entities in Scope

Retrieval supports **5 entities**: Account, User, Group, Role, Permission.

BasicAuth and Security Keys are excluded — BasicAuth is internal authentication infrastructure, and Security Keys need their own permission model (see above).

---

## Resolved Items

- [x] ~~Original detailed notes~~ → Lost, but plan has been rebuilt from scratch and is more thorough
- [x] ~~Filter model design~~ → Expression-based FilterBuilder with typed filter operations (Corely.Common)
- [x] ~~Default page size~~ → 25
- [x] ~~List return type~~ → `PagedResult<T>` with TotalCount, CurrentPage, HasMore
- [x] ~~Which entities need retrieval~~ → Account, User, Group, Role, Permission
- [x] ~~Expose `CrudxString` formatting~~ → Shared utility `PermissionLabelProvider` — takes 5 CRUDX bools, returns formatted string. Usable by Permission, EffectivePermission, or any future CRUDX-bearing model. Refactor existing `Permission.CrudxString` to use it.

## Deferred Items

- [ ] Security key retrieval — separate design pass needed (see 🔒 section above)

---

## 🗺️ Implementation Roadmap

### Phase 0: Scaffolding

Build the shared infrastructure before touching any entity-specific code.

**0a. Filtering & ordering system (Corely.Common)**
- Filter types: `StringFilter`, `ComparableFilter<T>`, `GuidFilter`, `BoolFilter`, `EnumFilter<TEnum>`
- `FilterBuilder<T>` with `Where` overloads (property, collection navigation)
- `ExpressionTranslator` — converts FilterBuilder state to `Expression<Func<T, bool>>`
- `OrderBuilder<T>` with `By` / `ThenBy` + `SortDirection` enum
- Unit tests for filter translation layer and order builder

**0b. Shared models & utilities (Corely.IAM)**
- `PagedResult<T>` — Items, TotalCount, CurrentPage, HasMore
- `ChildRef` — reusable name/ID pair for hydrated children
- `EffectivePermission`, `EffectiveRole`, `EffectiveGroup` — permission tree models
- `PermissionLabelProvider` — shared CRUDX → string formatter; refactor `Permission.CrudxString` to use it

**0c. Service shell (Corely.IAM)**
- `IRetrievalService` interface with method stubs for all 5 entities
- `RetrievalService` implementation shell
- Authorization + Telemetry decorators (shell)
- DI registration in `ServiceRegistrationExtensions.cs`

### Phase 1–5: Entity Implementation (one at a time, increasing complexity)

Each phase follows the same pattern:
1. Add List/Get methods to the entity's processor
2. Wire up processor calls in `RetrievalService`
3. Add authorization rules to the Authorization decorator
4. Add telemetry to the Telemetry decorator
5. Add DevTools CLI command (see below)
6. Add ConsoleTest example demonstrating usage
7. Unit tests (see testing strategy below)

**Phase 1: Permission** — simplest
- Hydrated children: Roles (1 child type)
- No nested M:M complexity
- Full e2e validation of the pattern: List with filtering/paging, Get with hydration, effective permission tree
- Proves out the FilterBuilder integration with the repo layer

**Phase 2: Group**
- Hydrated children: Users, Roles (2 child types)

**Phase 3: Role**
- Hydrated children: Users, Groups, Permissions (3 child types)

**Phase 4: User**
- Hydrated children: Accounts, Groups, Roles (3 child types)
- Security keys explicitly excluded from hydration (see 🔒 section)

**Phase 5: Account**
- Hydrated children: Users, Groups, Roles, Permissions (4 child types, most complex)
- Security keys explicitly excluded from hydration (see 🔒 section)

### DevTools CLI Commands

The DevTools project (`Corely.IAM.DevTools`) uses System.CommandLine with a reflection-based `CommandBase` pattern — commands are auto-discovered, so adding retrieval commands is just adding new classes.

Each entity phase includes a corresponding **DevTools CLI command** nested under a `Retrieval` parent command:

```
devtools retrieval list-permissions --filter <json> --skip 0 --take 25
devtools retrieval get-permission <id> --hydrate
devtools retrieval list-groups --filter <json> --skip 0 --take 25
devtools retrieval get-group <id> --hydrate
... (same pattern for role, user, account)
```

Commands follow the existing DevTools patterns:
- Inherit from `CommandBase`
- Nested under `Retrieval` parent command class
- Use `[Argument]` and `[Option]` attributes for CLI parameters
- Added alongside each entity phase (not all at once)

### ConsoleTest Examples

Each entity phase includes a usage example in `Corely.IAM.ConsoleTest` demonstrating List (with filtering/paging) and Get (with hydration) for that entity. Added alongside each phase.

### Testing Strategy (Phases 0–5)

**Phase 0 tests own their domain thoroughly:**
- **ExpressionTranslator** — every filter operation, negations, AND composition, nested `.Any()`, edge cases (empty filter). This is the exhaustive filter logic test suite.
- **PagedResult<T>** — CurrentPage/HasMore calculation
- **PermissionLabelProvider** — CRUDX bool → string formatting

**Phase 1–5 tests focus on the service and processor layers — no double-dipping on filter logic:**
- **Processor tests** — List returns correct results with paging, Get returns correct entity, Get with hydration returns correct children (names/IDs), Get returns effective permission tree, correct behavior when entity not found
- **Service tests** — RetrievalService correctly delegates to processors
- **Authorization decorator tests** — correct permission checks (READ required), bypass behavior
- **Telemetry decorator tests** — correct logging calls
- Filtering in Phase 1–5 tests is used *incidentally* (e.g., pass a filter to verify the processor hands it to the repo) — not re-testing that `StringFilter.Contains` produces the right expression. That's Phase 0's job.
