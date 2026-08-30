# Testing Strategy

## Problem

Test coverage is one tier deep. Everything runs as unit tests against `AddIAMServices` with the
mock database, so whole classes of failure are invisible: EF query translation, provider-specific
SQL, migrations, and every browser-facing flow.

The deeper issue is that `MockRepo` is a hand-written re-implementation of EF's semantics - key
matching, `CreatedUtc` preservation, and a `SetProperty` expression interpreter. A passing test
proves the mock agrees with itself, not that EF can translate the predicate to valid SQL.

This is an IAM library. Broken means insecure, and the least-covered area is the one where that
matters most: authorization.

## Goals

1. **Verifiable remotely.** Every tier must run unattended, headless, in one command, with no
   manual host setup. The working mode is Remote Control from a phone: brainstorm, build, and
   verify without touching the keyboard.
2. **Trustworthy for an IAM library.** Authorization and multi-tenancy correctness proven against
   real infrastructure, not against a test double.
3. **No duplicated coverage.** Each tier owns one seam and proves what no lower tier can.

## Tiers

Tiers are defined by the **seam they own**, not by the tooling they happen to use.

| Tier | Owns | Substrate | Speed |
|---|---|---|---|
| **Unit** | A single class's logic, dependencies substituted | No database | ms |
| **Integration** | The persistence seam - EF translation, schema, provider behavior | Real DB (SQLite / Testcontainers) | seconds |
| **Functional** | The HTTP seam - middleware, cookies, redirects, ASP.NET pipeline | `WebApplicationFactory` in-process + real DB | seconds |
| **E2E** | The browser seam - JS, real cookie enforcement, external redirects | Playwright vs. running WebApp | tens of seconds |

**Functional** is ASP.NET's term for in-process host tests. It is the tier that was missing from
earlier thinking, and it is where most authentication flow coverage belongs.

## Placement ladder

For every candidate test case, walk down and stop at the first tier that can actually prove it.

1. **Can it be proven with no database and no host?** -> Unit.
2. **Does it need real SQL translation, real schema, or real provider behavior?** -> Integration.
3. **Does it need the HTTP pipeline - middleware, cookies, redirects, antiforgery?** -> Functional.
4. **Does it need a real browser - JS execution, actual cookie enforcement, external OAuth
   redirects?** -> E2E.

**Rule: a case proven at tier N is never re-proven at tier N+1.** Higher tiers exercise the seam
they own, not the logic that already passed below them. If a functional test is asserting a
permission calculation, the case is in the wrong tier - the functional test should assert only
that the pipeline surfaced the decision correctly.

When a case seems to fit two tiers, it usually means it is really two cases. Split it and place
each half separately.

## Capability inventory

Recorded because the root cause of earlier bad advice was forgetting what was already available,
not lacking a tool. Nothing here requires the user to do manual setup.

- **Databases** - SQLite in-memory, LocalDB, a local SQL Server instance, and any provider via
  Docker. All reachable directly.
- **Docker** - not always running, but startable on request. Remote Control means "start Docker"
  is just another instruction; it is not a manual-setup blocker and must not be treated as one.
- **The running WebApp** - launchable locally, with a demo dataset already defined.
- **Chrome driving** - the browser automation tooling can sign in, read cookies, and inspect
  pages against the running app.
- **Direct database access** - rows can be read and written to set up or verify state.

Any future claim that a behavior "cannot be verified locally" should be checked against this list
first.

## Priority targets

Ordered by risk, not by what was most recently built. All of the following are currently proven
only against `MockRepo`.

1. **Authorization** - CRUDX evaluation, wildcard `Guid.Empty` resolution, the two-layer
   service/processor decorator split, system-context bypass, and `IsNonSystemUserContext` gating
   on self-operations. Highest value: broken means insecure, and it is cheap to test well.
2. **Multi-tenancy** - user/account M:M, the join entities and their `DeleteBehavior.NoAction`
   mappings, and the manual `.Include()` / `.Clear()` sequence processors must perform before
   deletes. Exactly the provider-specific behavior a mock cannot model.
3. **RBAC** - group, role, and permission assignment and resolution.
4. **Migrations** - applying cleanly across all three shipped providers.
5. **Key management** - symmetric and asymmetric provisioning, storage, and rotation.
6. **Authentication flows** - registration, sign-in, MFA, Google auth, password recovery,
   account switch, token renewal, sign out everywhere.

### The authorization scenario matrix

The highest-value single piece of work. Shape:

- Build a fixture that mirrors what `SeedWebAppDemo.ps1` already produces - create a user, create
  an account, add a non-admin user to it, assign roles and permissions.
- Drive a data-driven matrix of `(actor, resource, operation) -> expected` rows asserting both
  what the non-admin *can* reach and what they *cannot*.
- Place it at **integration**: the thing under test is whether a grant survives traversal across
  real M:M join entities to produce the right decision. Real EF is the point.
- Pure permission-evaluation edge cases (wildcard resolution, precedence) stay at **unit** - they
  need no database, so by the ladder they belong lower.

## Decisions

- **Docker: yes, scoped to Testcontainers** for the provider matrix. You ship three providers with
  three sets of migrations that no automated test has ever executed against their real database.
  SQLite cannot close that gap.
- **Docker: no compose stack for demo/dev purposes.** A webapp + database + Seq stack is running
  convenience, not verification. It buys no coverage and is out of scope here.
- **Playwright: deferred, not dropped.** See the trigger list below.
- **SQLite in-memory** is the default substrate for integration and functional tiers - relational,
  supports `ExecuteUpdate`, no external dependency, already proven in the `Corely.DataAccess`
  parity tests. Its gaps (looser typing, weaker DDL) need checking against the M:M join entity
  configurations.
- **Fake `TimeProvider` everywhere.** No test may depend on wall-clock sleeping. A seven-day
  session expiry must be assertable in a millisecond.
- **Tiers stay separately runnable.** `RebuildAndTest.ps1` must remain fast enough for the
  edit-build-test loop; the Testcontainers tier is opt-in.

### Playwright trigger conditions

Deferred because the WebApp is a reference host, not the shipped product, and because
`HttpClient` at the functional tier can read `Set-Cookie` headers directly and assert `HttpOnly`,
`Secure`, `SameSite`, and expiry without a browser.

Reach for Playwright **only** when a gap looks like one of these:

- A flow that depends on JavaScript execution or the Blazor circuit
- Verifying that a browser *enforces* cookie attributes, as opposed to verifying the app *sets*
  them
- An external redirect through a real origin, such as the Google OAuth handshake
- The WebApp becoming a shipped deliverable rather than a demo

If a gap does not match one of these, the functional tier is the correct answer. In particular,
"we have not seen this work in the web app" is a **functional** tier gap - `WebApplicationFactory`
boots the real middleware, Razor Pages, antiforgery, and cookie pipeline in-process.

## The MockRepo question

Orthogonal to the tier layout: it concerns what substrate the *unit* tier runs on.

`MockRepo` is a second implementation of behavior EF already defines, and every piece of it is a
chance for the double to disagree with production. Running unit tests against a real `DbContext`
on SQLite would delete that entire class of problem.

Caveat found during the renewal work: the **EF Core InMemory provider is not relational**. It
cannot execute `ExecuteUpdateAsync`, does not enforce constraints or foreign keys, and does no SQL
translation - so it would not have caught the case that prompted this. SQLite is the right choice,
not InMemory.

Open decisions:

- Does a real EF context replace `MockRepo` outright, or does the mock stay for speed where query
  behavior is irrelevant?
- If the mock stays, are parity tests worth it - as `Corely.DataAccess` already does - so drift is
  caught rather than assumed absent?
- The switch point is `Corely.IAM.UnitTests/ServiceFactory.cs`, which wires every test through
  `AddIAMServices` with the mock database. Changing that default reaches 1300+ tests at once, so
  it needs measuring before committing.

Sequenced **after** the integration and functional tiers exist, so the tradeoff is concrete rather
than theoretical.

## Known obstacles

- **The seed script is not self-contained.** `SeedWebAppDemo.ps1` expects `corely` on PATH,
  `corely config` already pointed at the target database, an auth token at
  `%USERPROFILE%\Corely\corely-iam-auth-token.json`, and scratch directories under `%TEMP%`.
  Windows paths and host-level state. For tests it needs to become a code fixture; for any future
  container use it needs an entrypoint program rather than pwsh plus CLI plus config files.
- **Three target frameworks are in play** - WebApp on net10.0, DevTools and the migrations CLI on
  net9.0, `Corely.DataAccess` on net8.0.
- **The console test is not a test tier.** It is a manual demo harness - nothing asserts, nothing
  fails, a human reads the output. It should not be counted as integration coverage.

## Sequencing

1. **Functional tier** - `WebApplicationFactory` + SQLite + fake `TimeProvider`. Biggest coverage
   jump per unit of work, and it permanently closes the "never verified in the web app" gap.
2. **Integration tier** - starting with the authorization scenario matrix on SQLite, then
   Testcontainers for the three-provider matrix and migrations.
3. **Resolve the `MockRepo` question** with the tradeoff now measurable.
4. **E2E** - only if a Playwright trigger condition is actually hit.

## Notes

- `Corely.DataAccess` already runs the same operations against both `MockRepo` and real EF on
  SQLite. That pattern is worth copying rather than reinventing.
- Project structure and naming should make the tier obvious from where a test lives.
- Decide a skip-vs-fail policy for missing infrastructure, and make `RebuildAndTest.ps1` reflect it.

## Work items

Tracked backlog. Each item names the tier that owns it, per the placement ladder. Check off as
built.

### Unit backfill (U)

Unit coverage is otherwise complete - every processor, authorization decorator, provider, mapper,
and validator has a matching test class. Two gaps found on audit:

- [x] **U1 - `InvitationService` tests.** Currently zero coverage; the only references in the test
      project are build artifacts. Four unexercised public methods: `CreateInvitationAsync`,
      `AcceptInvitationAsync`, `RevokeInvitationAsync`, `ListInvitationsAsync`. Security-relevant,
      since invitations are how outsiders gain account access.
- [x] **U2 - Missing telemetry decorator tests.** `GoogleAuthServiceTelemetryDecorator`,
      `InvitationServiceTelemetryDecorator`, `MfaServiceTelemetryDecorator`,
      `PasswordRecoveryServiceTelemetryDecorator`, `PasswordRecoveryProcessorTelemetryDecorator`.
      Low risk - they only log - but every other decorator in the codebase has one.

### Functional (F)

New project. Boots the real WebApp in-process; owns the HTTP seam.

- [x] **F1 - Infrastructure.** `WebApplicationFactory` host, SQLite-backed `IamDbContext`, fake
      `TimeProvider` injected so expiry is controllable, and a reusable seed fixture. Enabler for
      everything else in this tier.
- [x] **F2 - Token renewal on idle.** Advance the clock past `AuthTokenTtlSeconds`, request a
      protected page, assert 200 rather than a redirect, a rotated `jti`, and a **preserved**
      `session_started_at`. This is the automated form of the manual browser verification.
- [x] **F3 - Session cap.** Advance past `AuthSessionTtlSeconds`; assert redirect to sign-in.
      Confirms the expiry clamp holds at the boundary.
- [x] **F4 - Revocation takes effect.** Revoke the active token out of band; assert the next
      request redirects to `/signin` with the correct `ReturnUrl`.
- [x] **F5 - Sign-in happy path.** Assert cookies are set with the expected `HttpOnly`, `Secure`,
      `SameSite`, and expiry attributes. Reads `Set-Cookie` directly - no browser needed.
- [x] **F6 - Sign-in failure and unauthenticated access.** Bad credentials, and an anonymous
      request to a protected page redirecting with `ReturnUrl` preserved.
- [x] **F7 - MFA challenge flow.** Sign-in returning a challenge, then completion.
- [x] **F8 - Account selection and switch.** Multi-account users through the real pipeline.
- [x] **F9 - Sign out, and sign out everywhere.** Cookies cleared; sibling sessions invalidated.
- [x] **F10 - Password recovery through the web.** Request, then redeem.
- [x] **F11 - Antiforgery enforcement.** A POST without a valid token is rejected.

### Integration (I)

Owns the persistence seam. Real EF, real schema, real provider behavior.

- [x] **I1 - Infrastructure.** SQLite-backed real `IamDbContext` fixture plus a code-based
      replacement for the demo dataset currently defined in `SeedWebAppDemo.ps1`. Enabler.
- [x] **I2 - Authorization scenario matrix.** *Highest-value single item.* Build the fixture -
      create a user, create an account, add a non-admin, assign roles and permissions - then drive
      a data-driven table of `(actor, resource, operation) -> expected`, asserting both what the
      non-admin can reach and what they must not. Lives here rather than at unit because the thing
      under test is whether a grant survives traversal across real M:M join entities.
- [x] **I3 - M:M join entity delete behavior.** `DeleteBehavior.NoAction` mappings and the manual
      `.Include()` / `.Clear()` sequence processors must perform before deleting. Precisely what a
      mock cannot model.
- [x] **I4 - `ExecuteUpdateAsync` predicates translate.** Token revocation and password-recovery
      invalidation currently pass only because `MockRepo` interprets the expression tree in memory.
- [x] **I5 - Nullable comparison predicates.** Cases such as `t.AccountId == accountId` behave
      differently in SQL than in memory.
- [x] **I6 - List, filter, and paging queries translate.** Across the retrieval surface.
- [x] **I7 - Key management round-trip.** Symmetric and asymmetric provisioning, storage, and
      rotation against a real database.
- [x] **I8 - Provider matrix via Testcontainers.** Re-run the integration suite against MySQL,
      MariaDB, and SQL Server. Opt-in; Docker is startable on request.
- [x] **I9 - Migrations apply cleanly on all three providers.** Three sets of migrations that no
      automated test has ever executed against their real database.

### E2E (E)

None scheduled. Gated on the Playwright trigger conditions above; add items only when a gap
actually matches one.

### Documentation (D)

The plan is where reasoning lives; `CLAUDE.md` is where the *rules* live, because it is loaded
automatically into every session with no flag, no path, and nothing for the user to remember.
Anything a future session must know without being told belongs there.

This exists to prevent a specific, already-observed failure: a remote session being asked to "run
the tests" and inventing reasons it cannot, when it plainly could.

- [x] **D1 - Codify the placement ladder in `CLAUDE.md`.** A short "Testing" section under
      Development Patterns holding the four tiers, one line each on the seam they own; the
      if/then ladder for placing a new case; and the anti-duplication rule that a case proven at
      tier N is never re-proven at tier N+1. Keep it terse and link out to this plan for the
      reasoning - `CLAUDE.md` should carry the decision procedure, not the argument behind it.
      **Do this first**, before any test code, so every subsequent item is placed by the written
      rule rather than by whoever happens to be implementing.
- [x] **D2 - Document how to run each tier, progressively.** A "Running tests" block giving the
      exact command per tier, what infrastructure each needs, and what to do when it is absent -
      explicitly including that Docker is startable on request rather than a blocker. Written so a
      cold session can go from "run the tests" to a running command with no discovery and no
      questions asked of the user.
- [x] **D3 - Record the capability inventory in `CLAUDE.md`.** A condensed form of the inventory
      in this plan: available databases, Docker on request, the launchable WebApp, browser driving,
      direct database access. Paired with an explicit instruction that a claim of "this cannot be
      verified locally" must be checked against that list before being made.

**Definition of done, standing rule:** no tier item (F or I) is complete until its run command is
in `CLAUDE.md`. D2 is not a one-time task - it grows as each tier lands. A tier that exists but is
undocumented is a tier a future session will not find.

## Build order

Phased so each phase ends somewhere useful rather than half-built.

0. **D1** - write the placement ladder into `CLAUDE.md` before any test code exists, so every item
   below is placed by the written rule rather than by implementer preference. D3 can ride along.
1. **U1, U2** - small, self-contained, closes the audit findings before starting anything new.
2. **F1, then F2 / F3 / F4** - stands up the functional tier and immediately converts the manual
   browser verification of renewal, session cap, and revocation into repeatable tests. Permanently
   closes the "never verified in the web app" gap.
3. **I1, then I2** - the authorization scenario matrix. Highest risk area in the library, and the
   first coverage of authorization against real EF rather than a mock.
4. **I3, I4, I5** - the remaining persistence-seam cases a mock cannot prove.
5. **F5 - F11** - broaden functional coverage across the rest of the auth surface.
6. **I6, I7** - remaining query and key-management translation.
7. **I8, I9** - Testcontainers provider matrix and migrations. Slow and opt-in, so last, but the
   only thing that proves the three shipped providers actually work.
8. **Resolve the `MockRepo` question** with the tradeoff now measurable against real tiers.

D2 is not a phase. It is incremental: each tier documents its own run command as it lands.

## Test case inventory

The work items above are **epics, not tests**. Each expands to somewhere between a handful and a
hundred cases. This section enumerates them so the work is resumable cold.

**Caveat for whoever picks this up:** these cases were derived from project structure and the
architecture described in `CLAUDE.md`, not from reading every method body. Treat the list as a
starting point to validate against the code at implementation time, not as gospel. Add cases you
find; strike cases that do not apply; note which you did either way.

Rough total: **200-400 cases**, heavily weighted toward I2, where most of the count is table rows
rather than hand-written methods.

### U1 - InvitationService (~15-25) — DONE, scope corrected

**Scope correction found on implementation.** `InvitationService` is a pure delegating service:
three methods are expression-bodied pass-throughs to `IInvitationProcessor`, and only
`ListInvitationsAsync` contains logic (mapping `ListResult<T>` to `RetrieveListResult<T>`). The
business-rule cases originally listed below - expired, revoked, already-accepted, email mismatch -
belong to `InvitationProcessor`, which **already has coverage** in `InvitationProcessorTests`.

Writing them here would have duplicated processor coverage, violating the anti-duplication rule.
Delivered instead: 10 tests covering delegation, result pass-through on both success and failure,
the `ListInvitationsAsync` mapping including null data and non-success codes, and the constructor
null guard. This is exactly the "validate the case list against the code" caveat below, working
as intended.

Original case list, retained as the record of what was checked and reassigned:

- `CreateInvitationAsync`: valid request succeeds; invalid request returns validation failure;
  duplicate or already-pending invitation; target account does not exist; caller lacks context.
- `AcceptInvitationAsync`: valid acceptance adds the user to the account; expired invitation;
  already-accepted invitation; revoked invitation; invitation addressed to a different user;
  unknown id.
- `RevokeInvitationAsync`: pending invitation is revoked; already-accepted cannot be revoked;
  already-revoked returns the correct code; unknown id.
- `ListInvitationsAsync`: returns invitations for the account; empty result; paging honored.
- Every path asserts the specific result code, not merely success or failure.

### U2 - Telemetry decorators (~30-50) — DONE

38 tests across five new classes, following the existing
`AuthenticationServiceTelemetryDecoratorTests` shape. Per decorator: constructor null guards for
both inner service and logger, one delegate-and-log test per public method, a failure-result case,
and an exception-propagation case.

For each of `GoogleAuthServiceTelemetryDecorator`, `InvitationServiceTelemetryDecorator`,
`MfaServiceTelemetryDecorator`, `PasswordRecoveryServiceTelemetryDecorator`, and
`PasswordRecoveryProcessorTelemetryDecorator`, per public method:

- Delegates to the inner implementation and returns its result unchanged.
- Logs on success.
- Logs on failure result codes.
- Does not swallow exceptions.

Mirror the shape of the existing decorator tests rather than inventing a new one.

### F1 - Functional infrastructure (0 tests)

Harness only. Deliverables:

- `WebApplicationFactory` host wired to the WebApp.
- SQLite-backed `IamDbContext` replacing the configured provider, created per fixture.
- Fake `TimeProvider` registered so tests control expiry without sleeping.
- A seed fixture producing the demo dataset. Shared with I1 - build it once.
- A helper that signs in and returns an authenticated `HttpClient`, handling antiforgery.
- Helpers to decode the auth cookie JWT claims and to read auth token rows from the database.

### F2 - Token renewal on idle (~6-10)

- Idle past `AuthTokenTtlSeconds` but within session: protected request returns 200, not a redirect.
- The reissued token has a **new** `jti`.
- The reissued token **preserves** `session_started_at`.
- The previous token row is marked revoked at the instant the new one is issued.
- New `exp` is clamped to `sessionStart + AuthSessionTtlSeconds` when that is nearer than
  `now + AuthTokenTtlSeconds`.
- Renewal refreshes the cookie with the expected lifetime.
- A request comfortably inside `AuthTokenTtlSeconds` does **not** rotate the token.
- Renewal is refused for an already-revoked token.

### F3 - Session cap (~3-5)

- Just before `AuthSessionTtlSeconds`: still renews, returns 200.
- Just after: redirects to sign-in.
- Auth cookies are cleared on session expiry.
- Repeated renewals never extend the session past the original `session_started_at` bound.

### F4 - Revocation takes effect (~3-5)

- Token revoked out of band: next protected request redirects to `/signin` with `ReturnUrl`
  preserved.
- A revoked token cannot be renewed.
- Revoking one session does not sign out a sibling session.
- Auth cookies are cleared on the rejected request.

### F5 - Sign-in and cookie attributes (~5-8)

- Valid credentials redirect to the post-auth destination.
- Auth cookies are set with `HttpOnly`, `Secure`, and the expected `SameSite`.
- Cookie expiry reflects `AuthSessionTtlSeconds`, not `AuthTokenTtlSeconds`.
- The device id cookie is set, and is reused rather than regenerated on a second sign-in.
- The issued JWT carries the expected claims, including `session_started_at`.

### F6 - Failures and unauthenticated access (~5-8)

- Bad password sets no auth cookies and re-renders with an error.
- Unknown username behaves identically to a bad password - no user enumeration via response
  differences.
- Anonymous request to a protected page redirects with `ReturnUrl` correctly encoded.
- After signing in, the `ReturnUrl` destination is honored.
- A `ReturnUrl` pointing off-host is rejected rather than followed.
- A malformed or garbage auth cookie is rejected and cleared rather than throwing.

### F7 - MFA (~8-12)

- Sign-in for an MFA-enabled user yields the challenge, not a full session.
- The pre-MFA state grants no access to protected pages.
- Correct TOTP completes sign-in and issues the full token.
- Incorrect code is rejected.
- Expired or replayed code is rejected.
- Challenge state expires.
- Enrollment and disable flows through the pipeline.

### F8 - Account selection and switch (~5-8)

- Single-account user skips selection.
- Multi-account user is routed to selection.
- Selecting an account issues a token scoped to it.
- Switching accounts rotates the token and reflects the new account in claims.
- Switching to an account the user does not belong to is refused.

### F9 - Sign out (~4-6)

- Sign out clears all auth cookies.
- The token row is revoked.
- A subsequent protected request redirects.
- Sign out everywhere revokes sibling sessions; a second client is signed out on its next request.

### F10 - Password recovery (~6-10)

- Requesting recovery for a known user creates a pending recovery.
- Requesting for an unknown user responds identically - no enumeration.
- Valid token renders the reset form; redeeming it changes the password.
- The old password no longer works; the new one does.
- An expired, already-redeemed, or invalidated token is rejected.
- Completing recovery revokes existing sessions.
- Requesting a second recovery invalidates the first.

### F11 - Antiforgery (~2-4)

- POST without a token is rejected.
- POST with a stale or mismatched token is rejected.
- Rejection does not leak a stack trace.

### I1 - Integration infrastructure (0 tests)

Harness only. Deliverables:

- SQLite-backed real `IamDbContext` fixture with schema created from the model.
- A **code-based** replacement for the dataset in `SeedWebAppDemo.ps1` - a user, an account, a
  non-admin member, roles, groups, and permission grants. Shared with F1.
- Verification that the SQLite schema round-trips the M:M join entity configurations, since
  SQLite DDL support is weaker than the shipped providers.
- A seam allowing the same suite to run against a Testcontainers-provided provider for I8.

### I2 - Authorization scenario matrix (~50-150)

Data-driven. Define the axes, then generate rows.

- **Actors**: account owner; non-admin member with explicit grants; member granted via role;
  member granted via group-to-role; user with no account membership; system context; the
  resource's own user (self).
- **Operations**: Create, Read, Update, Delete, Execute.
- **Resources**: at minimum users, accounts, groups, roles, permissions.
- **Expected**: allowed or denied, asserted in **both** directions - every row proves either that a
  grant works or that a non-grant is refused. The denials matter more than the allowances.

Rules that must each have rows:

- Wildcard `ResourceId == Guid.Empty` grants access to all resources of that type.
- A specific-resource grant does not leak to sibling resources of the same type.
- A grant on one operation does not imply another - Read does not imply Update.
- Permissions resolve transitively through role, and through group-to-role.
- Removing a role, group membership, or permission revokes the derived access.
- Cross-account isolation: a grant in account A never grants anything in account B.
- The owner role behaves as expected without needing explicit grants.
- System context bypasses permission checks.
- System context is **blocked** from self operations - MFA, password, Google auth, deregister self.
- `IsNonSystemUserContext` gating: a real user passes, system context does not.
- Service decorators validate context only; they perform no CRUDX checks.
- Processor decorators perform the CRUDX check and refuse before invoking the inner implementation.
- There is no user-administrates-user path: an account owner cannot read or modify another user
  directly, only manage that user's relationship to account entities.

Pure evaluation edge cases with no persistence dependency stay at unit, per the ladder.

### I3 - M:M join entities and deletes (~10-20)

For each many-to-many relationship - user/account, user/role, user/group, group/role,
role/permission, and any others present:

- Deleting a parent without clearing the join collection fails as expected under
  `DeleteBehavior.NoAction`, rather than silently cascading.
- The processor's `.Include()` / `.Clear()` sequence allows the delete to succeed.
- Join rows are actually removed, leaving no orphans.
- The counterpart entity survives the delete.
- Re-adding a previously removed relationship works.

### I4 - ExecuteUpdateAsync predicates translate (~5-10)

- Revoke all tokens for a user: predicate translates and affects only matching rows.
- Revoke by token id; revoke by device.
- Password recovery invalidation, including the `excludeRecoveryId` branch.
- Already-revoked and already-expired rows are excluded.
- The returned affected-row count is accurate.
- A non-matching predicate affects zero rows and does not throw.

### I5 - Nullable comparison predicates (~5-15)

- `AccountId == accountId` where the column is nullable and the value is null.
- The same where the value is non-null and rows containing nulls exist.
- `!=` comparisons over nullable columns, which do **not** match nulls in SQL.
- Optional date columns - `RevokedUtc`, `CompletedUtc`, `InvalidatedUtc` - in null and non-null
  predicates.
- Any predicate combining a nullable comparison with a boolean operator.

### I6 - List, filter, and paging (~15-30)

Across the retrieval surface:

- Paging returns the correct page, size, and total count.
- A page beyond the end returns empty rather than throwing.
- Ordering is stable and deterministic across pages.
- Filters translate to SQL rather than falling back to client evaluation.
- Combined filter plus paging plus ordering.
- Queries with `.Include()` return the expected graph without duplication.
- Empty result sets.

### I7 - Key management round-trip (~8-15)

- Symmetric key provisioned, stored encrypted, and read back decryptable.
- Asymmetric key pair provisioned, stored, and read back; signing and verification round-trip.
- Rotation issues a new key while values encrypted under the prior key remain readable.
- Keys are never persisted in plaintext - assert on the stored column.
- Retrieval with a wrong or missing system key fails cleanly rather than returning garbage.

### I8 - Provider matrix via Testcontainers (multiplier, not new cases)

- Re-run the I1-I7 suite against MySQL, MariaDB, and SQL Server.
- Opt-in; Docker is startable on request.
- Expect divergence, especially around the M:M `NoAction` behavior in I3, nullable semantics in
  I5, and case sensitivity in string comparisons. Divergences found here are the point of the tier.

### I9 - Migrations (~3-6)

- All migrations apply cleanly from empty on each of the three providers.
- The resulting schema matches the EF model - no pending-changes diff.
- Migrations are idempotent when re-run against an already-migrated database.

## Status

- Plan created and revised: tiers defined by seam, placement ladder added, Docker scoped to
  Testcontainers, Playwright deferred with explicit trigger conditions, targets reoriented around
  the whole library rather than the token renewal work.
- Unit coverage audited. Complete except for `InvitationService` - no tests at all - and five
  telemetry decorators. Recorded as U1 and U2.
- Documentation items D1-D3 added: the placement ladder, per-tier run commands, and the capability
  inventory all get codified in this project's `CLAUDE.md`, since that is the only file loaded into
  every session automatically. D1 is sequenced ahead of all test code.
- Work items, build order, and a per-item test case inventory recorded. The plan is meant to be
  resumable cold: if something here is ambiguous to a fresh reader, that is a defect in the plan
  worth fixing rather than working around.
### Outcome

All eight phases are built. Final suite: **1593 passing, 21 skipped** (the opt-in provider matrix),
green through `.\RebuildAndTest.ps1`.

| Tier | Project | Tests |
|---|---|---|
| Unit | `Corely.IAM.UnitTests` | 1365 |
| Unit | `Corely.IAM.Web.UnitTests` | 102 |
| Integration | `Corely.IAM.IntegrationTests` | 66 + 21 opt-in |
| Functional | `Corely.IAM.Web.FunctionalTests` | 60 |
| E2E | none | 0, deliberately |

### Defects found and fixed

The tiers earned their keep immediately. Three findings, two of them production bugs that no
amount of additional unit testing would have surfaced.

**1. JWT lifetime validation bypassed the injected `TimeProvider`.**
`AuthenticationProvider` set `ValidateLifetime = true` with no `LifetimeValidator`, so
Microsoft.IdentityModel fell back to `DateTime.UtcNow`. This violated the project's own convention
and made token expiry unassertable with a controllable clock. Caught by F2's negative case - a
request *inside* the token TTL rotated the token anyway, which meant the other renewal tests had
been passing for the wrong reason. Fixed by adding `IsWithinLifetime`, which reads the injected
provider. Both validation sites now use it.

**2. `DeleteRoleAsync` never cleared its `Permissions` collection.**
`RolePermission` is `DeleteBehavior.NoAction` on both sides, so deleting any role with a permission
assigned orphaned the join rows and threw `FOREIGN KEY constraint failed`. The processor included
and cleared `Users` and `Groups` but not `Permissions`. This affected **all three shipped
providers** - it was reproduced on SQL Server, MySQL, and MariaDB after the fix landed, as
regression cover. Exactly the failure class `CLAUDE.md` warns about, and precisely what a mock repo
cannot model, since it exists only in the database's referential integrity rules.

**3. EF Core compensates for SQL three-valued logic on inequality.**
Not a bug - behaviour worth pinning down. `Where(t => t.AccountId != value)` emits
`AccountId <> @value OR AccountId IS NULL`, so null rows are *included*, matching C# semantics. Raw
SQL would exclude them. The test asserting the opposite failed, and now documents the real
behaviour: anyone hand-writing SQL for one of these predicates must add the null branch back.

### Corrections to the plan's own case list

The inventory's caveat - validate against the code, not the plan - was needed twice.

- **U1 shrank.** `InvitationService` turned out to be a pure delegating service. The business-rule
  cases listed for it belong to `InvitationProcessor`, which already had coverage; writing them at
  the service layer would have duplicated processor tests and violated the anti-duplication rule.
  Delivered 10 delegation and mapping tests instead of the 15-25 estimated.
- **F10 needed a stronger assertion than the plan implied.** The reset page returns 200 whether the
  reset succeeds or fails, so a status-code assertion passed while the reset silently did nothing -
  a missing `ConfirmPassword` field. Assertions now check page content. The plan's "no user
  enumeration" case was also dropped: this host deliberately surfaces library result codes, which
  is documented intent for a reference host rather than a defect.

### Decisions resolved

- **`MockRepo` stays.** Measured: 1365 unit tests run in ~4s against the mock; 66 integration tests
  take ~8s against SQLite - roughly 40x per test. The integration tier now covers everything the
  mock cannot model, so the right move is to place database-dependent behaviour there rather than
  make the mock more faithful. Recorded in `CLAUDE.md`.
- **Provider matrix is opt-in.** `CORELY_RUN_CONTAINER_TESTS=1` gates it; three containers take
  roughly nine minutes. Skipped by default so the edit-build-test loop stays fast, and skipped with
  a message rather than failing when Docker is down.
- **E2E remains empty.** No gap encountered matched a Playwright trigger condition. Everything the
  plan expected a browser for - cookie attributes included - was observable from `Set-Cookie`
  headers at the functional tier.

### What a future session should know

- Nothing in the default test run needs a database, Docker, or manual setup. All four tiers are one
  `dotnet test` away.
- The provider matrix needs Docker running. Docker being down is a reason to start Docker, not a
  reason to report something unverifiable.
- `CLAUDE.md` carries the tier table, the placement ladder, the anti-duplication rule, the run
  commands, the Playwright trigger conditions, and the capability inventory. It is the entry point;
  this plan is the reasoning behind it.

## Status

**Complete.** All work items U1-U2, F1-F11, I1-I9, and D1-D3 are built and green. Two production
defects found and fixed. E2E deliberately empty, gated on the recorded trigger conditions.
