# Per-package release tags

## Starting cold

For a session picking this up with no history. The change spans every Corely repository that
publishes, done in the same way. The owner extended it from the first two to all five: same
ecosystem, same CI rules, whether a repository ships one package or five.

| Repository | Path |
|---|---|
| Corely.IAM | `C:\source\git\ultrabstrong\Corely.IAM` |
| Corely.Billing | `C:\source\git\ultrabstrong\Corely.Billing` |
| Corely.Common | `C:\source\git\ultrabstrong\Corely.Common` |
| Corely.Security | `C:\source\git\ultrabstrong\Corely.Security` |
| Corely.DataAccess | `C:\source\git\ultrabstrong\Corely.DataAccess` |

Read each repository's `CLAUDE.md` before touching it. Tags are pushed only with the owner's say-so,
since a published package version cannot be deleted.

**Decided with the owner; do not reopen:** tags per package, not lockstep versioning. Each package
keeps its own `<Version>` in its csproj.

## Progress

**Implemented and committed locally on 2026-09-26, nothing pushed.** What is left is the owner's
review and the pushes, in this order:

1. **Push the baseline tags first, in both repositories.** They point at the commits that shipped
   what is on nuget.org, whose `release.yml` still triggers on `v*`, so pushing them publishes
   nothing:
   - Corely.IAM, all at `11fa1e87` (`v2.8.0`): `Corely.IAM-v2.3.1`, `Corely.IAM.Web-v2.4.0`,
     `Corely.IAM.DataAccessMigrations.Cli-v2.0.2`.
   - Corely.Billing, all at `c59fd4e` (`v2.0.0`): `Corely.Billing-v2.0.0`,
     `Corely.Billing.Web-v2.0.0`, `Corely.Billing.IAM-v1.0.0`, `Corely.Billing.Web.IAM-v1.0.0`,
     `Corely.Billing.DataAccessMigrations.Cli-v2.0.0`.
   - Corely.Common `Corely.Common-v2.0.3` at `8af28cc` (`v2.0.3`), Corely.Security
     `Corely.Security-v3.1.1` at `6bc8ffa` (`v3.1.1`), Corely.DataAccess `Corely.DataAccess-v3.1.0`
     at `02798bd` (`v3.1.0`).

   `git push origin <tag> ...` names each one; never `--tags`.
2. **Then push each repository's commit** with the new `release.yml`.
3. **The first real release under the new scheme** is the end to end proof. Ask the owner before
   tagging it.

Done so far:

- `scripts/release-package.sh` in each repository parses the tag, fails unless it names a package
  the repository publishes and its version equals that csproj's `<Version>`, enforces the CLI major,
  and hands the csproj and pack arguments to `release.yml`. Verified locally against every sample tag
  below plus malformed ones (`Corely.IAM-2.3.1`, `Corely.Nope-v1.0.0`, `Corely.IAM-vabc`, `v2.8.0`,
  a prerelease tag, a stale version, another repository's package) and a fixture with mismatched
  majors: each passed or failed as intended.
- `release.yml` triggers on `Corely.IAM*-v*` / `Corely.Billing*-v*`, has no `workflow_dispatch`,
  packs and pushes only the named package, and pushes without `--skip-duplicate`. The strict
  version check step is gone: the tag version check replaces it.
- `scripts/check-package-versions.sh` compares each package against its own latest tag, skips a
  package with none, and only ever reports (`STRICT` is gone with its only caller).
- Both `CLAUDE.md` files have a "Releasing" section.
- The GitHub `release-tags` ruleset on both repositories (no deletion, update or force-move of a
  release tag) was widened on GitHub to cover the new tag names alongside `v*`.
- Corely.Billing ships five packages, not the two this plan first listed; all five are covered.
- Corely.Common, Corely.Security and Corely.DataAccess got the same change with a one-package list
  and no CLI rule, their `CLAUDE.md` "Releasing" text rewritten (DataAccess had none), their
  rulesets widened, and each full suite green. Nothing in `pinnacleinnovation` said `git tag v`.
- `ci.yml`'s comment ("only on a version tag") was left as is: a per-package tag is still a version
  tag.

Found while verifying: the check reports `Corely.IAM.Web`, `Corely.IAM.DataAccessMigrations.Cli`,
`Corely.Billing.Web.IAM` and `Corely.Billing.DataAccessMigrations.Cli` as changed without a bump.
The only change in each is the README dash cleanup, and the README ships as the package's nuget.org
page. Corely.Common, Corely.Security and Corely.DataAccess have real unreleased code changes since
their last release (the extension-block and seam refactors, comment removal). The old script
reported all of these the same way. Whether any is worth a release is the owner's call.

## The problem

A tag names a release of the repository, not a version of anything published. `release.yml` fired on
`v*`, packed every package, and each took its version from its own csproj:

| Tag | Corely.IAM | Corely.IAM.Web | Migration CLI |
|---|---|---|---|
| `v2.6.0` | 2.2.2 | 2.3.0 | 2.0.1 |
| `v2.7.0` | 2.3.0 | 2.3.0 | 2.0.1 |

Nothing maps `v2.7.0` to Corely.IAM 2.3.0 without that table. Corely.Billing's `v1.0.0-preview.2`
matched its package only by coincidence; its CLI was still `1.0.0-preview.1`.

The industry convention for independently versioned packages in one repository is a tag naming both
the package and its version: Azure SDK for .NET (`Azure.Storage.Blobs_12.19.1`), OpenTelemetry .NET
(`core-1.9.0`), changesets/lerna (`pkg@1.2.3`), Go nested modules (`subdir/v1.2.3`).

## The change

Tag format `<PackageId>-v<Version>`, where `<Version>` equals the csproj's `<Version>`:

```
Corely.IAM-v2.3.0
Corely.IAM.Web-v2.3.0
Corely.IAM.DataAccessMigrations.Cli-v2.0.1
Corely.Billing-v1.0.0-preview.2
Corely.Billing.DataAccessMigrations.Cli-v1.0.0-preview.1
```

A tag releases exactly the package it names. Releasing two packages is two tags on the same commit.

**`release.yml` keeps its file name.** The nuget.org trusted-publishing policy is bound to it;
renaming breaks publishing until the policy is edited.

`--match 'Corely.IAM-v*'` does not match `Corely.IAM.Web-v*` (the character after `Corely.IAM` is `.`,
not `-`), so a plain prefix glob is enough to find a package's own previous tag.

The old `v*` tags stay. They are history, and deleting published tags breaks anyone who pinned one.

## Done when

Each repository releases one package per tag, the tag version equals the package version or the run
fails, every published package has a baseline tag on GitHub, and the `CLAUDE.md` of each says how to
release.
