# Testing Strategy

## Problem

Test coverage is currently one tier deep. Everything runs as unit tests against
`AddIAMServices` with the mock database, which means whole classes of failure are invisible:
EF query translation, provider-specific SQL behavior, migrations, and any browser-facing flow
are never exercised by the suite.

This became concrete while implementing token renewal. The revocation paths use
`IRepo.ExecuteUpdateAsync`, which `MockRepo` satisfies by interpreting the `SetProperty`
expression tree in memory. That mock is a hand-written re-implementation of EF's semantics, so
a passing test proves the mock agrees with itself - not that EF can translate the predicate to
valid SQL. A throwaway-LocalDB fixture was prototyped to close that gap and then deliberately
removed, because bolting one integration test onto the unit test project is not a strategy.

## Scope

1. Define the tiers - unit, integration, end-to-end - and what each is responsible for proving.
2. Decide project structure and naming so tier is obvious from where a test lives.
3. Decide how integration tests get a database, and whether that dependency is required or skippable.
4. Decide end-to-end tooling and how much of the WebApp is worth driving.
5. Decide what runs locally, what runs in CI, and what a contributor without Docker or LocalDB sees.

## Approach

1. Inventory current coverage and identify what is only ever proven against mocks.
2. Choose a database strategy for integration tests:
   - LocalDB - zero setup on Windows, but Windows-only and not reproducible in CI
   - Testcontainers / Docker - matches the three shipped providers (MySQL, MariaDB, MsSql), reproducible, needs Docker
   - Consider running the same integration suite against all three providers, since provider
     differences are exactly what this tier should catch
3. Choose an e2e approach - Playwright against the WebApp - and decide the seed story
   (`Corely.IAM.WebApp/DemoSetup/SeedWebAppDemo.ps1` already builds a realistic dataset).
4. Decide skip-vs-fail policy for missing infrastructure, and make `RebuildAndTest.ps1` reflect it.
5. Backfill the highest-value gaps first rather than chasing coverage percentage.

## Replace the hand-written mock repo with a real EF context

`MockRepo` is a second implementation of behavior that EF already defines - key matching,
`CreatedUtc` preservation, and now a `SetProperty` expression interpreter. Every one of those is
a chance for the test double to disagree with production. Running unit tests against a real
`DbContext` backed by an in-memory database would delete that entire class of problem, and would
mean tests exercise the same query translation the app uses.

Important caveat found while building the renewal work: the **EF Core InMemory provider is not
relational**. It cannot execute set-based updates (`ExecuteUpdateAsync`), does not enforce
constraints or foreign keys, and does no SQL translation - so it would not have caught the case
that prompted this. Microsoft discourages it as a general-purpose test database for exactly
these reasons.

**SQLite in-memory** is the stronger candidate: relational, supports `ExecuteUpdate`, fast, no
external dependency, and already proven in the `Corely.DataAccess` parity tests. Its own gaps
(looser typing, weaker DDL support) are worth checking against the IAM entity configurations,
particularly the M:M join entities and their `DeleteBehavior.NoAction` mappings.

Decisions to make:

- Does a real EF context replace `MockRepo` outright, or does the mock stay for speed in tests
  that never touch query behavior?
- If `MockRepo` stays, is it worth parity tests against real EF, as `Corely.DataAccess` does, so
  drift is caught rather than assumed absent?
- The switch point is `Corely.IAM.UnitTests/ServiceFactory.cs`, which wires every test through
  `AddIAMServices` with the mock database. Changing that default reaches 1300+ tests at once, so
  it needs measuring before committing to it.

## Candidate first targets

- EF translation of repo predicates, especially nullable comparisons such as
  `t.AccountId == accountId`, which behave differently in SQL than in memory
- Parity between `MockRepo` and real EF behavior, so the mock cannot silently drift
- Migrations applying cleanly across all three providers
- Authentication flows end to end: sign in, MFA, account switch, token renewal after idle,
  sign out everywhere

## Notes

- `Corely.DataAccess` already has parity tests running the same operations against both `MockRepo`
  and real EF on SQLite. That pattern is worth copying rather than reinventing.
- Renewal is a good early e2e candidate because it is the one flow that depends on wall-clock
  time and cookie lifetime, which unit tests cannot meaningfully assert.
- Keep tiers separately runnable. `RebuildAndTest.ps1` should stay fast enough for the
  edit-build-test loop.

## Status

- Plan created.
- No implementation started.
