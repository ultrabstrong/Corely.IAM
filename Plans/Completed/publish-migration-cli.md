# Publish the Migration CLI as a dotnet Tool

## Problem

A consumer who takes `Corely.IAM` from NuGet has no supported way to create the IAM schema. This
was reported from DocsToData, which consumes the published packages and treats the library as a
black box. Every claim in that report was verified against this repository:

- The four `Corely.IAM.DataAccessMigrations.*` projects carry no packaging metadata and have never
  been published. The `Corely.IAM.DataAccessMigrations.*` rows in the README's structure table
  describe projects a consumer cannot install.
- No `Migrate()`, `MigrateAsync()`, or `EnsureCreated()` call exists anywhere in `Corely.IAM` or
  `Corely.IAM.Web`. There is no startup-migration path, by design.
- `IamDbContext` is `internal`. Even if the provider migration assemblies were published, a
  consumer could not name the type to migrate it. The existing migration projects only compile
  because `AssemblyInfo.cs` grants them `InternalsVisibleTo`.
- The README's documentation links are relative repository paths. From an extracted package they
  resolve to nothing, so a consumer is pointed at documentation they cannot open, describing
  tooling they cannot install.
- The README quickstart goes from configuration straight to `RegisterUserAsync`, never saying how
  the tables come to exist.

The gap went unnoticed because the only consumer that ever created the schema — `Corely.IAM.WebApp`
— is in this repository and reaches the CLI through a `ProjectReference`.

## Decision

Ship the migration CLI as a `dotnet tool`. Do not add a runtime migration API.

Schema changes belong in a deployment step, not in application startup. A tool keeps the migration
path off the library's public surface, which means `IamDbContext` stays `internal` and no new API
has to be supported forever. This is the same shape as `dotnet ef`.

Rejected alternatives:

- **A public `MigrateIamDatabaseAsync` extension.** Would work, but puts a schema-mutating entry
  point in the runtime surface of a library whose consumers deploy it in ways we do not control.
- **Publishing the provider migration assemblies for direct consumer use.** Insufficient on its
  own — `IamDbContext` is internal — and it would force consumers to reference an assembly whose
  only purpose is to carry migration classes.
- **`EnsureCreated()`.** Not available (internal context), and all-or-nothing per database. In
  DocsToData the IAM context shares a database with four application contexts; the first context
  to call it would leave the others with no tables and no error.

## Scope

### 1. Replace file-based configuration with flags and environment variables

Today `ConfigurationProvider` reads `corely-iam-db-migration-settings.json` from
`AppContext.BaseDirectory` in a static constructor, with no per-invocation override. For an
installed tool that is the tool's own install directory: one machine-global settings file shared by
every repository and every CI job on the box, holding a plaintext connection string. `config init`
would clobber whatever the last caller wrote.

Resolution order, highest wins:

1. `--provider` / `--connection-string` options
2. `CORELY_IAM_DB_PROVIDER` / `CORELY_IAM_DB_CONNECTION` environment variables

No settings file. Local interactive use sets the environment variables once per shell; CI sets them
per job; a test fixture passes the flags with whatever connection string its container was assigned.

This removes most of the `config` and `provider` command groups, which exist only to manage the
file:

| Command | Disposition |
|---------|-------------|
| `config init` | Removed |
| `config set-connection` | Removed |
| `config show` | Removed |
| `config path` | Removed |
| `config test-connection` | Moved to `db test-connection` |
| `provider set` | Removed |
| `provider show` | Removed |
| `provider list` | Kept — reads no state |

`db script` needs a provider but never opens a connection, so it must not require a connection
string.

This is a breaking change to the CLI. Nothing outside this repository consumes it yet, which is
precisely why it has to happen before the first publish rather than after.

### 2. Package the CLI as a dotnet tool

`PackAsTool`, `ToolCommandName`, `PackageId`, `Version`, plus the description, tags, README and
LICENSE the other packages already carry. The existing `_IsPublishing` single-file properties must
keep working — `RebuildAndTest.ps1` publishes a self-contained executable, and that is separate
from the tool package.

The release workflow's trusted-publishing policy is scoped to `Corely.IAM*`, so the new package id
is already covered. Add pack and push steps for it.

### 3. Give IAM its own migrations history table

Neither `EFMsSqlConfiguration` nor `EFMySqlConfiguration` sets `MigrationsHistoryTable`, so IAM
records its migrations in the default `__EFMigrationsHistory`. When IAM shares a database with a
consumer's own contexts — the DocsToData case, five contexts in one database — they all write to
that one table.

This is not a correctness bug: EF computes pending migrations as its own assembly's migrations
minus the applied ids, so ids belonging to other contexts are ignored. It is a legibility and
safety problem, and the CLI's existing `--show-all` flag on `db status` and `db list` is evidence
that someone already ran into the muddle. A distinct history table per context is what Microsoft
recommends for exactly this case.

Default to `__CorelyIamMigrationsHistory`, with a `--history-table` option to override. The
override is not decoration: it is the upgrade path for anyone whose database already has IAM
migrations recorded in `__EFMigrationsHistory`. Without it, EF would find an empty history table
and try to re-apply every migration against tables that already exist.

Document both routes — pass `--history-table __EFMigrationsHistory` to stay put, or copy the rows
across once and adopt the new default.

### 4. Documentation

- Absolute GitHub URLs for the README's documentation links. Relative links still render correctly
  on github.com, so nothing is lost by making them absolute.
- A schema-creation step in the README quickstart, pointing at the tool. This is the first thing
  anyone deploying the library needs and currently the last thing the README covers.
- Rewrite the CLI's `Docs/index.md` and `README.md` around flags and environment variables.
- Update the CLAUDE.md WebApp setup steps, which currently instruct `config init`.
- Note the history-table change in `MIGRATION-2.0.md`.

### 5. Tests

The CLI has no test project; DevTools has one. Option resolution and its precedence rules are pure
logic with no database and no host, so they belong in the unit tier per the placement ladder in
CLAUDE.md. Add `Corely.IAM.DataAccessMigrations.Cli.UnitTests` mirroring
`Corely.IAM.DevTools.UnitTests`, covering flag-over-environment precedence, the environment-only
path, the missing-value error messages, and `db script` not demanding a connection string.

### 6. Cleanup

`Corely.IAM.DataAccessMigrations.MariaDb/` is still on disk but absent from `Corely.IAM.slnx`. The
project was removed from source control when MariaDB support was dropped; what remains is untracked
build output. Delete it.

## Versions

| Package | From | To | Why |
|---------|------|----|-----|
| `Corely.IAM.DataAccessMigrations.Cli` | — | 2.0.0 | New package. Its major tracks `Corely.IAM`'s, since a major IAM release is what can change the schema its migrations produce; minor and patch move independently |
| `Corely.IAM` | 2.0.0 | 2.0.1 | Packs the root README, whose links and quickstart changed; no API change |
| `Corely.IAM.Web` | 2.0.0 | 2.0.0 | Unchanged - it packs its own README, which had no relative doc links |

## Notes

- The consumer's own suggestion was to publish the provider migration packages so they could call
  `Database.MigrateAsync()` themselves. That path is closed by the internal context, and the tool
  gives them what they actually needed without opening it.
- Keeping `IamDbContext` internal is deliberate and worth restating here, because this is the
  second time it has come up as an obstacle. The obstacle is the point: consumers configure the
  context through `IAMOptions.EFConfigurationFactory` and never hold it.

## Status

Implemented.

- CLI resolves provider and connection string from `--provider` / `--connection-string`, falling
  back to `CORELY_IAM_DB_PROVIDER` / `CORELY_IAM_DB_CONNECTION`. No settings file remains, and the
  `config` group plus `provider set` / `provider show` are gone. `config test-connection` became
  `db test-connection`.
- Packed as a .NET tool with the command name `corely-iam-db`. Published first as 1.0.0, then
  immediately republished as 2.0.0 to establish the major-tracks-IAM rule before anyone installed
  it. 1.0.0 remains listed on nuget.org.
- Both provider configurations now set `MigrationsHistoryTable`, defaulting to
  `__CorelyIamMigrationsHistory`, overridable with `--history-table`.
- `Corely.IAM` bumped to 2.0.1 for the packed README. `Corely.IAM.Web` was left at 2.0.0 - it packs
  its own README, which had no relative links.
- Added `Corely.IAM.DataAccessMigrations.Cli.UnitTests` (20 tests).
- Deleted the untracked `Corely.IAM.DataAccessMigrations.MariaDb` build leftovers.

Two things were found by running the tool rather than by reading it:

- Walking the type hierarchy to pick up base-class options also picked up
  `RequiresConnectionString`, an ordinary `protected virtual` property, and bound it as a
  positional argument. Binding now requires an explicit `[Option]` or `[Argument]`.
- Verified end to end by packing, installing to a temp tool path, and running `db create` against
  LocalDB: all six migrations applied and all 23 tables were created, with the history recorded in
  `__CorelyIamMigrationsHistory`.
